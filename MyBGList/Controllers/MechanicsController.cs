using System.Linq.Dynamic.Core;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace MyBGList.Controllers;

[Route("[controller]")]
[ApiController]
public class MechanicsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<MechanicsController> _logger;

    public MechanicsController(ApplicationDbContext dbContext,
        IDistributedCache distributedCache,
        ILogger<MechanicsController> logger)
    {
        _dbContext = dbContext;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    // Get
    [HttpGet(Name = "GetMechanics")]
    // [ResponseCache(CacheProfileName = "NoCache")]
    [ResponseCache(Location = ResponseCacheLocation.Client, Duration = 120)]
    [SwaggerOperation(
        Summary = "Get a list of mechanics",
        Description = "Retrieves a list of mechanics with custom paging, sorting, and filtering rules")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "A RestDTO object containing a list of mechanics",
        typeof(RequestDTO<MechanicDTO>))]
    public async Task<RestDTO<Mechanic[]>> Get(
        [FromQuery] [SwaggerParameter("A DTO object that can be used to customize some retrieval parameters")]
        RequestDTO<MechanicDTO> input)
    {
        var query = _dbContext.Mechanics.AsQueryable();
        if (!string.IsNullOrEmpty(input.FilterQuery))
        {
            query = query.Where(m => m.Name.Contains(input.FilterQuery));
        }

        var recordCount = await query.CountAsync();
        Mechanic[]? result = null;
        var cacheKey = $"{input.GetType()}-{JsonSerializer.Serialize(input)}";

        if (!_distributedCache.TryGetValue<Mechanic[]>(cacheKey, out result))
        {
            query = query
                .OrderBy($"{input.SortColumn} {input.SortOrder}")
                .Skip(input.PageIndex * input.PageSize)
                .Take(input.PageSize);
            result = await query.ToArrayAsync();
            _distributedCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
        }


        return new RestDTO<Mechanic[]>()
        {
            Data = result!,
            PageIndex = input.PageIndex,
            PageSize = input.PageSize,
            RecordCount = recordCount,
            Links = new List<LinkDTO>
            {
                new LinkDTO(Url.Action(null,
                        "Mechanics",
                        new { input.PageSize, input.PageIndex },
                        Request.Scheme)!,
                    "self", "GET")
            }
        };
    }

    // Post
    [Authorize(Roles = nameof(RoleNames.Moderator))]
    [HttpPost(Name = "PostMechanics")]
    [ResponseCache(CacheProfileName = "NoCache")]
    public async Task<RestDTO<Mechanic?>> Post(MechanicDTO model)
    {
        var mechanic = await _dbContext.Mechanics.FindAsync(model.Id);
        if (mechanic != null)
        {
            if (!string.IsNullOrEmpty(model.Name))
            {
                mechanic.Name = model.Name;
            }

            mechanic.LastModifiedDate = DateTime.UtcNow;
            _dbContext.Mechanics.Update(mechanic);
            await _dbContext.SaveChangesAsync();
        }

        return new RestDTO<Mechanic?>()
        {
            Data = mechanic,
            Links = new List<LinkDTO>()
            {
                new LinkDTO(Url.Action(null,
                        "Mechanics",
                        model,
                        Request.Scheme)!,
                    "self", "POST")
            }
        };
    }

    // Delete
    [Authorize(Roles = nameof(RoleNames.Moderator))]
    [HttpDelete(Name = "DeleteMechanics")]
    [ResponseCache(CacheProfileName = "NoCache")]
    public async Task<RestDTO<Mechanic?>> Delete(int id)
    {
        var mechanic = await _dbContext.Mechanics.FindAsync(id);
        if (mechanic != null)
        {
            _dbContext.Mechanics.Remove(mechanic);
            await _dbContext.SaveChangesAsync();
        }

        return new RestDTO<Mechanic?>()
        {
            Data = mechanic,
            Links = new List<LinkDTO>()
            {
                new LinkDTO(Url.Action(null,
                        "Mechanics",
                        id,
                        Request.Scheme)!,
                    "self", "DELETE")
            }
        };
    }
}