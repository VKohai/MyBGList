using System.Linq.Dynamic.Core;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyBGList.Attributes;
using MyBGList.Constants;
using MyBGList.DTO;
using Swashbuckle.AspNetCore.Annotations;

namespace MyBGList.Controllers;

[Route("[controller]")]
[ApiController]
public class BoardGamesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<BoardGamesController> _logger;

    public BoardGamesController(
        ApplicationDbContext dbContext,
        IMemoryCache memoryCache,
        ILogger<BoardGamesController> logger)
    {
        _dbContext = dbContext;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    // GET
    [SwaggerOperation(
        Summary = "Get a list of board games.",
        Description = "Retrieves a list of board games " +
                      "with custom paging, sorting, and filtering rules.")]
    [HttpGet(Name = "GetBoardGames")]
    [ResponseCache(CacheProfileName = "Client-120")]
    public async Task<RestDTO<BoardGame[]>> Get(
        [FromQuery]
        [SwaggerParameter("A DTO object that can be used " +
                          "to customize the data-retrieval parameters.")]
        RequestDTO<BoardGameDTO> input)
    {
        _logger.LogInformation(CustomLogEvents.BoardGamesController_Get, "Get method started");
        var query = _dbContext.BoardGames.AsQueryable();
        if (!string.IsNullOrEmpty(input.FilterQuery))
            query = query.Where(b => b.Name.StartsWith(input.FilterQuery));

        (BoardGame[]? result, int recordCounts) dataTuple = (null, 0);
        var cacheKey = $"{input.GetType()}-{JsonSerializer.Serialize(input)}";
        if (!_memoryCache.TryGetValue<BoardGame[]>(cacheKey, out dataTuple.result))
        {
            query = query
                .OrderBy($"{input.SortColumn} {input.SortOrder}")
                .Skip(input.PageIndex * input.PageSize)
                .Take(input.PageSize);
            dataTuple.recordCounts = await query.CountAsync();
            dataTuple.result = await query.ToArrayAsync();
            _memoryCache.Set(cacheKey, dataTuple,
                new TimeSpan(0, 0, 0, 30));
        }

        return new RestDTO<BoardGame[]>()
        {
            Data = dataTuple.result!,
            PageIndex = input.PageIndex,
            PageSize = input.PageSize,
            RecordCount = dataTuple.recordCounts,
            Links = new List<LinkDTO>
            {
                new LinkDTO(
                    Url.Action(null,
                        "BoardGames",
                        new { input.PageSize, input.PageIndex },
                        Request.Scheme)!,
                    "self",
                    "GET"),
            }
        };
    }

    // Get by Id
    [SwaggerOperation(
        Summary = "Get a single board game.",
        Description = "Retrieves a single board game by a given Id.")]
    [HttpGet("{id}")]
    [ResponseCache(CacheProfileName = "Any-60")]
    public async Task<RestDTO<BoardGame?>> GetBoardGame([CustomKeyValue("x-test-3", "value 3")] int id)
    {
        _logger.LogInformation(CustomLogEvents.BoardGamesController_Get,
            "GetBoardGame method started.");
        BoardGame? result = null;
        var cacheKey = $"GetBoardGame-{id}";
        if (!_memoryCache.TryGetValue<BoardGame>(cacheKey, out result))
        {
            result = await _dbContext.BoardGames.FirstOrDefaultAsync(bg => bg.Id == id);
            _memoryCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
        }

        return new RestDTO<BoardGame?>()
        {
            Data = result,
            PageIndex = 0,
            PageSize = 1,
            RecordCount = result != null ? 1 : 0,
            Links = new List<LinkDTO>
            {
                new LinkDTO(
                    Url.Action(
                        null,
                        "BoardGames",
                        new { id },
                        Request.Scheme)!,
                    "self",
                    "GET"),
            }
        };
    }


    // Post
    [SwaggerOperation(
        Summary = "Updates a board game.",
        Description = "Updates the board game's data.")]
    [Authorize(Roles = nameof(RoleNames.Moderator))]
    [HttpPost(Name = "PostBoardGames")]
    [ResponseCache(NoStore = true)]
    public async Task<RestDTO<BoardGame?>> Post(BoardGameDTO model)
    {
        var boardgame = await _dbContext.BoardGames
            .Where(b => b.Id == model.Id)
            .FirstOrDefaultAsync();
        if (boardgame != null)
        {
            if (!string.IsNullOrEmpty(model.Name))
                boardgame.Name = model.Name;
            if (model.Year.HasValue && model.Year.Value > 0)
                boardgame.Year = model.Year.Value;
            boardgame.MinAge = model.MinAge is > 0 ? model.MinAge.Value : boardgame.MinAge;
            boardgame.PlayTime = model.PlayTime is > 0 ? model.PlayTime.Value : boardgame.PlayTime;
            boardgame.MinPlayers = model.MinPlayers is > 0 ? model.MinPlayers.Value : boardgame.MinPlayers;
            boardgame.MaxPlayers = model.MaxPlayers is > 0 ? model.MaxPlayers.Value : boardgame.MaxPlayers;
            boardgame.LastModifiedDate = DateTime.UtcNow;
            _dbContext.BoardGames.Update(boardgame);
            await _dbContext.SaveChangesAsync();
        }

        return new RestDTO<BoardGame?>()
        {
            Data = boardgame,
            Links = new List<LinkDTO>
            {
                new LinkDTO(
                    Url.Action(
                        null,
                        "BoardGames",
                        model,
                        Request.Scheme)!,
                    "self",
                    "POST"),
            }
        };
    }

    // Delete
    [SwaggerOperation(
        Summary = "Deletes a board game.",
        Description = "Deletes board games from the database by list of id.")]
    [Authorize(Roles = nameof(RoleNames.Moderator))]
    [HttpDelete(Name = "DeleteBoardGame")]
    [ResponseCache(NoStore = true)]
    public async Task<RestDTO<BoardGame[]?>> Delete(string idList)
    {
        var boardgames = new List<BoardGame>();
        var ids = idList.Split(",").Select(int.Parse);
        foreach (var id in ids)
        {
            var boardgame = await _dbContext.BoardGames.FindAsync(id);
            if (boardgame != null)
            {
                _dbContext.BoardGames.Remove(boardgame);
                await _dbContext.SaveChangesAsync();
                boardgames.Add(boardgame);
            }
        }


        return new RestDTO<BoardGame[]?>()
        {
            Data = boardgames.Any() ? boardgames.ToArray() : null,
            Links = new List<LinkDTO>
            {
                new LinkDTO(Url.Action(null,
                        "BoardGames",
                        ids,
                        Request.Scheme)!,
                    "self", "DELETE"),
            }
        };
    }
}