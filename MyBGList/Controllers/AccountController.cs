using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MyBGList.DTO;

namespace MyBGList.Controllers;

[Route("[controller]/[action]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApiUser> _userManager;
    private readonly SignInManager<ApiUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DomainsController> _logger;

    public AccountController(
        ApplicationDbContext dbContext,
        UserManager<ApiUser> userManager,
        SignInManager<ApiUser> signInManager,
        ILogger<DomainsController> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _configuration = configuration;
    }
    
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="input">A DTO containing the user data.</param>
    /// <returns>A 201 - Created Status Code in case of success.</returns>
    /// <response code="201">User has been registered. </response> 
    /// <response code="400">Invalid data. </response>
    /// <response code="500">An error occurred. </response> 
    [HttpPost]
    [ResponseCache(CacheProfileName = "NoCache")]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Register([FromQuery] RegisterDTO input)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var details = new ValidationProblemDetails(ModelState)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Status = StatusCodes.Status400BadRequest
                };
                return new BadRequestObjectResult(details);
            }

            var newUser = new ApiUser
            {
                UserName = input.UserName,
                Email = input.Email
            };
            var result = await _userManager.CreateAsync(newUser, input.Password!);
            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "User {userName} ({email}) has been created.",
                    newUser.UserName, newUser.Email);
                return StatusCode(201,
                    $"User '{newUser.UserName}' has been created.");
            }

            throw new Exception($"Error: {string.Join(" ",
                result.Errors.Select(e => e.Description))}");
        }
        catch (Exception e)
        {
            var exceptionDetails = new ProblemDetails
            {
                Detail = e.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                exceptionDetails);
        }
    }

    /// <summary>
    /// Performs a user login.
    /// </summary>
    /// <param name="input">A DTO containing the user's credentials.</param>
    /// <returns>The Bearer Token (in JWT format).</returns>
    /// <response code="200">User has been logged in. </response>
    /// <response code="400">Login failed (bad request). </response>
    /// <response code="401">Login failed (unauthorized). </response>
    [HttpPost]
    [ResponseCache(CacheProfileName = "NoCache")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromQuery] LoginDTO input)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var details = new ValidationProblemDetails(ModelState)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Status = StatusCodes.Status400BadRequest
                };
                return new BadRequestObjectResult(details);
            }

            var user = await _userManager.FindByNameAsync(input.UserName!);
            if (user == null || !await _userManager.CheckPasswordAsync(user, input.Password!))
                throw new Exception("Invalid login attempt.");

            var key = Encoding.UTF32.GetBytes(_configuration["JWT:SigningKey"]!);
            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!)
            };
            claims.AddRange(
                (await _userManager.GetRolesAsync(user))
                .Select(r => new Claim(ClaimTypes.Role, r)));

            var jwtObject = new JwtSecurityToken(
                _configuration["JWT:Issuer"],
                _configuration["JWT:Audience"],
                claims,
                expires: DateTime.Now.AddSeconds(300),
                signingCredentials: signingCredentials);

            var jwtString = new JwtSecurityTokenHandler().WriteToken(jwtObject);
            return StatusCode(StatusCodes.Status200OK, jwtString);
        }
        catch (Exception e)
        {
            var exceptionDetails = new ProblemDetails
            {
                Detail = e.Message,
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };
            return StatusCode(
                StatusCodes.Status401Unauthorized,
                exceptionDetails);
        }
    }
}