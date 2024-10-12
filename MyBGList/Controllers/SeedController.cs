using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Constants;
using MyBGList.Models.Csv;

namespace MyBGList.Controllers;

[Authorize(Roles = nameof(RoleNames.Administrator))]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("[controller]/[action]")]
[ApiController]
public class SeedController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SeedController> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApiUser> _userManager;

    public SeedController(ApplicationDbContext dbContext,
        IWebHostEnvironment env,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApiUser> userManager,
        ILogger<SeedController> logger)
    {
        _dbContext = dbContext;
        _env = env;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpPut]
    [ResponseCache(CacheProfileName = "NoCache")]
    public async Task<JsonResult> BoardGameData(int? id = null)
    {
        var config = new CsvConfiguration(CultureInfo.GetCultureInfo("pt-BR"))
        {
            HasHeaderRecord = true,
            Delimiter = ";"
        };
        using var reader = new StreamReader(System.IO.Path.Combine(_env.ContentRootPath, "Data/bgg_dataset.csv"));
        using var csv = new CsvReader(reader, config);
        var existingBoardGames = await _dbContext.BoardGames
            .ToDictionaryAsync(bg => bg.Id);
        var existingDomains = await _dbContext.Domains
            .ToDictionaryAsync(d => d.Name);
        var existingMechanics = await _dbContext.Mechanics
            .ToDictionaryAsync(m => m.Name);

        // Representing the current date and time,
        // which will be used to assign a value to the CreatedDate and
        // LastModifiedDate properties of the EF Core entities
        // we’re going to create.
        var now = DateTime.Now;

        var records = csv.GetRecords<BggRecord>();
        var skippedRows = 0;
        foreach (var record in records)
        {
            if (!record.ID.HasValue ||
                string.IsNullOrEmpty(record.Name) ||
                existingBoardGames.ContainsKey(record.ID.Value) ||
                (id.HasValue && id.Value != record.ID.Value))
            {
                ++skippedRows;
                continue;
            }

            var boardgame = new BoardGame()
            {
                Id = record.ID.Value,
                Name = record.Name,
                BGGRank = record.BGGRank ?? 0,
                ComplexityAverage = record.ComplexityAverage ?? 0,
                MaxPlayers = record.MaxPlayers ?? 0,
                MinAge = record.MinAge ?? 0,
                MinPlayers = record.MinPlayers ?? 0,
                OwnedUsers = record.OwnedUsers ?? 0,
                PlayTime = record.PlayTime ?? 0,
                RatingAverage = record.RatingAverage ?? 0,
                UsersRated = record.UsersRated ?? 0,
                Year = record.YearPublished ?? 0,
                CreatedDate = now,
                LastModifiedDate = now,
            };
            _dbContext.BoardGames.Add(boardgame);

            GetDomains(record, boardgame);
            GetMechanics(record, boardgame);
        }

        await SaveChanges();

        return new JsonResult(new
        {
            BoardGames = _dbContext.BoardGames.Count(),
            Domains = _dbContext.Domains.Count(),
            Mechanics = _dbContext.Mechanics.Count(),
            SkippedRows = skippedRows
        });

        void GetDomains(BggRecord record, BoardGame boardgame)
        {
            if (!string.IsNullOrEmpty(record.Domains))
                foreach (var domainName in record.Domains
                             .Split(',', StringSplitOptions.TrimEntries)
                             .Distinct(StringComparer.InvariantCultureIgnoreCase))
                {
                    var domain = existingDomains.GetValueOrDefault(domainName);
                    if (domain == null)
                    {
                        domain = new Domain()
                        {
                            Name = domainName,
                            CreatedDate = now,
                            LastModifiedDate = now
                        };
                        _dbContext.Domains.Add(domain);
                        existingDomains.Add(domainName, domain);
                    }

                    _dbContext.BoardGames_Domains.Add(new BoardGames_Domains()
                    {
                        BoardGame = boardgame,
                        Domain = domain,
                        CreatedDate = now
                    });
                }
        }

        void GetMechanics(BggRecord record, BoardGame boardgame)
        {
            if (!string.IsNullOrEmpty(record.Mechanics))
                foreach (var mechanicName in record.Mechanics
                             .Split(',', StringSplitOptions.TrimEntries)
                             .Distinct(StringComparer.InvariantCultureIgnoreCase))
                {
                    var mechanic = existingMechanics.GetValueOrDefault(mechanicName);
                    if (mechanic == null)
                    {
                        mechanic = new Mechanic()
                        {
                            Name = mechanicName,
                            CreatedDate = now,
                            LastModifiedDate = now
                        };
                        _dbContext.Mechanics.Add(mechanic);
                        existingMechanics.Add(mechanicName, mechanic);
                    }

                    _dbContext.BoardGames_Mechanics.Add(new BoardGames_Mechanics()
                    {
                        BoardGame = boardgame,
                        Mechanic = mechanic,
                        CreatedDate = now
                    });
                }
        }

        async Task SaveChanges()
        {
            using var transaction = _dbContext.Database.BeginTransaction();
            _dbContext.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS=0;");
            _dbContext.Database.ExecuteSqlRaw("ALTER TABLE mybglist.boardgames MODIFY COLUMN Id INT NOT NULL;");
            await _dbContext.SaveChangesAsync();
            _dbContext.Database.ExecuteSqlRaw(
                "ALTER TABLE mybglist.boardgames MODIFY COLUMN Id INT NOT NULL AUTO_INCREMENT;");
            _dbContext.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS=1;");
            transaction.Commit();
        }
    }

    [AllowAnonymous]
    [HttpPost]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> AuthData()
    {
        int rolesCreated = 0;
        int usersAddedToRoles = 0;

        if (!await _roleManager.RoleExistsAsync(nameof(RoleNames.Moderator)))
        {
            await _roleManager.CreateAsync(
                new IdentityRole(nameof(RoleNames.Moderator)));
            ++rolesCreated;
        }

        if (!await _roleManager.RoleExistsAsync(nameof(RoleNames.Administrator)))
        {
            await _roleManager.CreateAsync(new IdentityRole(nameof(RoleNames.Administrator)));
            ++rolesCreated;
        }

        if (!await _roleManager.RoleExistsAsync(nameof(RoleNames.SuperAdmin)))
        {
            await _roleManager.CreateAsync(new IdentityRole(nameof(RoleNames.SuperAdmin)));
            ++rolesCreated;
        }

        var testModerator = await _userManager.FindByNameAsync("vlad-moder");
        if (testModerator != null &&
            !await _userManager.IsInRoleAsync(testModerator, nameof(RoleNames.Moderator)))
        {
            await _userManager.AddToRoleAsync(testModerator, nameof(RoleNames.Moderator));
            ++usersAddedToRoles;
        }

        var testAdministrator = await _userManager.FindByNameAsync("vlad-admin");
        if (testAdministrator != null &&
            !await _userManager.IsInRoleAsync(testAdministrator, nameof(RoleNames.Administrator)))
        {
            await _userManager.AddToRoleAsync(testAdministrator, nameof(RoleNames.Moderator));
            await _userManager.AddToRoleAsync(testAdministrator, nameof(RoleNames.Administrator));
            ++usersAddedToRoles;
        }

        var testSuperAdmin = await _userManager.FindByNameAsync("vlad-super-admin");
        if (testSuperAdmin != null &&
            !await _userManager.IsInRoleAsync(testSuperAdmin, nameof(RoleNames.SuperAdmin)))
        {
            await _userManager.AddToRoleAsync(testSuperAdmin, nameof(RoleNames.Moderator));
            await _userManager.AddToRoleAsync(testSuperAdmin, nameof(RoleNames.Administrator));
            await _userManager.AddToRoleAsync(testSuperAdmin, nameof(RoleNames.SuperAdmin));
            ++usersAddedToRoles;
        }

        return new JsonResult(new
        {
            RolesCreated = rolesCreated,
            UsersAddedToRoles = usersAddedToRoles
        });
    }
}