using System.Diagnostics;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Attributes;
using MyBGList.Constants;
using MyBGList.DTO;

namespace MyBGList.Controllers;

[Route("[controller]")]
[ApiController]
public class DomainsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DomainsController> _logger;

    public DomainsController(ApplicationDbContext dbContext, ILogger<DomainsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // GET
    /// <summary>
    /// Gets a list of domains.
    /// </summary>
    /// <remarks>Retrieves a list of domains with custom paging, sorting, and filtering rules</remarks>
    /// <param name="input">A DTO object that can be used to customize some retrieval parameters</param>
    /// <returns>A RestDTO object containing a list of domains</returns>
    [HttpGet(Name = "GetDomains")]
    [ResponseCache(CacheProfileName = "Any-60")]
    [ManualValidationFilter]
    public async Task<ActionResult<RestDTO<Domain[]>>> Get([FromQuery] RequestDTO<DomainDTO> input)
    {
        if (!ModelState.IsValid)
        {
            var details = new ValidationProblemDetails(ModelState)
            {
                Extensions =
                {
                    ["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                }
            };

            if (ModelState.Keys.Any(k => k == "PageSize"))
            {
                details.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.2";
                details.Status = StatusCodes.Status501NotImplemented;
                return new ObjectResult(details)
                {
                    StatusCode = StatusCodes.Status501NotImplemented
                };
            }

            details.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            details.Status = StatusCodes.Status400BadRequest;
            return new BadRequestObjectResult(details);
        }

        var query = _dbContext.Domains.AsQueryable();
        if (!string.IsNullOrEmpty(input.FilterQuery))
        {
            query = query.Where(d => d.Name.Contains(input.FilterQuery));
        }

        var recordCount = await query.CountAsync();
        query = query
            .OrderBy($"{input.SortColumn} {input.SortOrder}")
            .Skip(input.PageIndex * input.PageSize)
            .Take(input.PageSize);

        return new RestDTO<Domain[]>
        {
            Data = await query.ToArrayAsync(),
            PageIndex = input.PageIndex,
            PageSize = input.PageSize,
            RecordCount = recordCount,
            Links = new List<LinkDTO>
            {
                new LinkDTO(
                    Url.Action(null,
                        "Domains",
                        new { input.PageSize, input.PageIndex },
                        Request.Scheme)!,
                    "self",
                    "GET"),
            }
        };
    }

    // Post
    [Authorize(Roles = nameof(RoleNames.Moderator))]
    [HttpPost(Name = "PostDomains")]
    [ResponseCache(CacheProfileName = "NoCache")]
    [ManualValidationFilter]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<RestDTO<Domain?>>> Post(DomainDTO model)
    {
        if (!ModelState.IsValid)
        {
            var details = new ValidationProblemDetails(ModelState)
            {
                Extensions =
                {
                    ["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                }
            };

            if (model.Id != 3 && model.Name != "Wargames")
            {
                details.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3";
                details.Status = StatusCodes.Status403Forbidden;
                return new ObjectResult(details)
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            details.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            details.Status = StatusCodes.Status400BadRequest;
            return new BadRequestObjectResult(details);
        }

        var domain = await _dbContext.Domains.FindAsync(model.Id);
        if (domain != null)
        {
            domain.Name = string.IsNullOrEmpty(model.Name) ? domain.Name : model.Name;
            domain.LastModifiedDate = DateTime.UtcNow;
            _dbContext.Domains.Update(domain);
            await _dbContext.SaveChangesAsync();
        }

        return new RestDTO<Domain?>
        {
            Data = domain,
            Links = new List<LinkDTO>
            {
                new LinkDTO(
                    Url.Action(
                        null,
                        "Domains",
                        model,
                        Request.Scheme)!,
                    "self",
                    "POST"),
            }
        };
    }

    // Delete
    [Authorize(Roles = nameof(RoleNames.Moderator))]
    [HttpDelete(Name = "DeleteDomains")]
    [ResponseCache(CacheProfileName = "NoCache")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<RestDTO<Domain?>> Delete(int id)
    {
        var domain = await _dbContext.Domains.FindAsync(id);
        if (domain != null)
        {
            _dbContext.Domains.Remove(domain);
            await _dbContext.SaveChangesAsync();
        }

        return new RestDTO<Domain?>
        {
            Data = domain,
            Links = new List<LinkDTO>
            {
                new LinkDTO(
                    Url.Action(null, "Domains", id, Request.Scheme)!,
                    "self", "DELETE")
            }
        };
    }
}