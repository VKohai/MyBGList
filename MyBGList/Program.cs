using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using MyBGList.Constants;
using MyBGList.GraphQL;
using MyBGList.gRPC;
using MyBGList.Swagger;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.Annotations;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Path = System.IO.Path;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

builder.Logging.ClearProviders()
    .AddSimpleConsole()
    .AddDebug()
    .AddJsonConsole(options =>
    {
        options.TimestampFormat = "HH:mm";
        options.UseUtcTimestamp = true;
    });

builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration);
    lc.WriteTo.MySQL(ctx.Configuration.GetConnectionString("DefaultConnection"),
        "LogEvents",
        LogEventLevel.Information,
        true);
    lc.Enrich.WithMachineName();
    lc.Enrich.WithThreadId();
    lc.Enrich.WithThreadName();
    lc.WriteTo.File("Logs/log.txt",
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] " +
                        "[{MachineName} #{ThreadId}] {ThreadName}" +
                        "{Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day);
    lc.WriteTo.File("Logs/errors.txt",
        restrictedToMinimumLevel: LogEventLevel.Error,
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] " +
                        "[{MachineName} #{ThreadId}] {ThreadName}" +
                        "{Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day);
}, writeToProviders: true);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(cfg =>
    {
        cfg.WithOrigins(builder.Configuration["AllowedOrigins"]!);
        cfg.AllowAnyHeader();
        cfg.AllowAnyMethod();
    });
    options.AddPolicy(name: "AnyOrigin", cfg =>
    {
        cfg.AllowAnyOrigin();
        cfg.AllowAnyHeader();
        cfg.AllowAnyMethod();
    });
});

builder.Services.AddIdentity<ApiUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        options.DefaultChallengeScheme =
            options.DefaultForbidScheme =
                options.DefaultScheme =
                    options.DefaultSignInScheme =
                        options.DefaultSignOutScheme =
                            JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        RequireExpirationTime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF32.GetBytes(builder.Configuration["JWT:SigningKey"]!))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ModerWithMobilePhone", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, nameof(RoleNames.Moderator))
            .RequireClaim(ClaimTypes.MobilePhone);
    });
    options.AddPolicy("MinAge18", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(c => c.Type == ClaimTypes.DateOfBirth)
            && DateTime.ParseExact("yyyyMMdd",
                ctx.User.Claims.First(c => c.Type == ClaimTypes.DateOfBirth).Value,
                System.Globalization.CultureInfo.InvariantCulture) >= DateTime.Now.AddYears(-18)));
});

// builder.Services.Configure<ApiBehaviorOptions>(options =>
// {
//     options.SuppressModelStateInvalidFilter = true;
// });

builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("NoCache", new CacheProfile { NoStore = true });
    options.CacheProfiles.Add("Any-60",
        new CacheProfile
        {
            Location = ResponseCacheLocation.Any,
            Duration = 60
        });
    options.CacheProfiles.Add("Client-120",
        new CacheProfile
        {
            Location = ResponseCacheLocation.Client,
            Duration = 120
        });

    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(
        (x) => $"The value '{x}' is invalid.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
        (x) => $"The field {x} must be a number.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
        (x, y) => $"The value '{x}' is not valid for {y}.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(
        () => $"A value is required.");
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
    options.EnableAnnotations();

    options.ParameterFilter<SortOrderFilter>();
    options.ParameterFilter<SortColumnFilter>();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    // options.AddSecurityRequirement(new OpenApiSecurityRequirement
    // {
    //     {
    //         new OpenApiSecurityScheme
    //         {
    //             Reference = new OpenApiReference
    //             {
    //                 Type = ReferenceType.SecurityScheme,
    //                 Id = "Bearer"
    //             }
    //         },
    //         Array.Empty<string>()
    //     }
    // });
    options.OperationFilter<AuthRequirementFilter>();
    options.DocumentFilter<DocumentFilter>();
    options.RequestBodyFilter<PasswordRequestFilter>();
    options.RequestBodyFilter<UsernameRequestFilter>();
    options.SchemaFilter<CustomKeyValueFilter>();
});

#region Caching Services

builder.Services.AddResponseCaching(options =>
{
    var mb = (1024 * 2);
    options.MaximumBodySize = 128 * mb;
    options.SizeLimit = 200 * mb;
    options.UseCaseSensitivePaths = true;
});

builder.Services.AddMemoryCache();

// builder.Services.AddDistributedMySqlCache(options =>
// {
//     options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//     options.SchemaName = "db";
//     options.TableName = "AppCache";
// });

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

#endregion

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(
        connectionString: connectionString,
        serverVersion: ServerVersion.AutoDetect(connectionString));
}, ServiceLifetime.Scoped);

builder.Services.AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(config =>
    {
        config.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
        {
            ["activated"] = false
        };
    });
}

if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage"))
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/error");

app.UseHttpsRedirection();
app.UseCors("AnyOrigin");
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();
app.MapGrpcService<GrpcService>();
app.Use((content, next) =>
{
    // content.Response.Headers["cache-control"] = "no-cache, no-store";
    content.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
    {
        NoCache = true, NoStore = true
    };
    return next.Invoke();
});

#region Minimap APIs

app.MapGet("/error",
    [EnableCors("AnyOrigin")](HttpContext context, [FromServices] ILogger logger) =>
    {
        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
        // TODO: logging, sending notifications, and more
        var details = new ProblemDetails
        {
            Extensions =
            {
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
            },
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Status = StatusCodes.Status500InternalServerError
        };
        if (exceptionHandler?.Error is NotImplementedException)
        {
            details.Status = StatusCodes.Status501NotImplemented;
            details.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.2";
        } else if (exceptionHandler?.Error is TimeoutException)
        {
            details.Status = StatusCodes.Status504GatewayTimeout;
            details.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.5";
        }

        app.Logger.LogError(CustomLogEvents.Error_Get, exceptionHandler?.Error,
            "An unhandled exception occurred with status {status}.\nMessage: {message}",
            details.Status, exceptionHandler?.Error?.Message);

        return Results.Problem(details);
    });

app.MapGet("/sex",
    [EnableCors("AnyOrigin")]() => { throw new UnreachableException(); });

app.MapGet("/cod/test",
    [EnableCors("AnyOrigin")] [ResponseCache(NoStore = true)]
    () =>
    {
        return Results.Text("<script>" +
                            "window.alert('Your client supports JavaScript!" +
                            "\\r\\n\\r\\n" +
                            $"Server time (UTC): {DateTime.UtcNow.ToString("o")}" +
                            "\\r\\n" +
                            "Client time (UTC): ' + new Date().toISOString());" +
                            "</script>" +
                            "<noscript>Your client does not support JavaScript</noscript>",
            "text/html");
    });

app.MapGet("/error/test/501",
    [EnableCors("AnyOrigin")] [ResponseCacheAttribute(NoStore = true)]
    () => { throw new NotImplementedException(); });
app.MapGet("/error/test/504",
    [EnableCors("AnyOrigin")] [ResponseCacheAttribute(NoStore = true)]
    () => { throw new TimeoutException(); });

// Cache APIs
app.MapGet("/cache/test/1", [EnableCors] [SwaggerOperation(Tags = ["Cache"])](HttpContext context) =>
{
    context.Response.Headers["cache-control"] = "no-cache, no-store";
    return Results.Ok();
});

app.MapGet("/cache/test/2", [EnableCors] [SwaggerOperation(Tags = ["Cache"])](HttpContext context)
    => Results.Ok());

app.MapGet("/auth/test/1",
    [Authorize]
    [EnableCors("AnyOrigin")]
    [ResponseCache(NoStore = true)]
    [SwaggerOperation(
        Tags = new[] { "Auth" },
        Summary = "Auth test #1 (authenticated users).",
        Description = "Returns 200 - OK if called by an authenticated user regardless of its role(s).")]
    [SwaggerResponse(StatusCodes.Status200OK, "Authorized")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authorized")]
    () => Results.Ok("You are authorized!"));

app.MapGet("/auth/test/2",
    [Authorize(Roles = nameof(RoleNames.Moderator))]
    [EnableCors("AnyOrigin")]
    [ResponseCache(NoStore = true)]
    [SwaggerOperation(
        Tags = new[] { "Auth" },
        Summary = "Auth test #2 (Moderator role).",
        Description = "Returns 200 - OK status code if called by " +
                      "an authenticated user assigned to the Moderator role.")]
    () => Results.Ok("You are authorized!"));

app.MapGet("/auth/test/3",
    [Authorize(Roles = nameof(RoleNames.Administrator))]
    [EnableCors("AnyOrigin")]
    [ResponseCache(NoStore = true)]
    [SwaggerOperation(
        Tags = ["Auth"],
        Summary = "Auth test #3 (Administrator role).",
        Description = "Returns 200 - OK if called by " +
                      "an authenticated user assigned to the Administrator role.")]
    () => Results.Ok("You are authorized!"));

app.MapGet("/auth/test/4",
    [Authorize(Roles = nameof(RoleNames.SuperAdmin))]
    [EnableCors("AnyOrigin")]
    [ResponseCache(NoStore = true)]
    [SwaggerOperation(
        Tags = ["Auth"],
        Summary = "Auth test #4 (SuperAdministrator role).",
        Description = "Returns 200 - OK if called by " +
                      "an authenticated user assigned to the SuperAdministrator role.")]
    () => Results.Ok("You are authorized!"));

#endregion

app.MapControllers();
// app.UseExceptionHandler(action =>
// {
//     action.Run(async context =>
//     {
//         var exceptionHandler =
//             context.Features.Get<IExceptionHandlerPathFeature>();
//         var details = new ProblemDetails
//         {
//             Detail = exceptionHandler?.Error.Message,
//             Extensions =
//             {
//                 ["traceId"] = Activity.Current?.Id ??
//                               context.TraceIdentifier
//             },
//             Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
//             Status = StatusCodes.Status500InternalServerError
//         };
//         await context.Response.WriteAsync(JsonSerializer.Serialize(details));
//     });
// });
app.Run();