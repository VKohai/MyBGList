using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;

const string projectName = "MyBGList";

var builder = WebApplication.CreateBuilder(args);

// Enables URI versioning
builder.Services.AddApiVersioning(options =>
{
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
});
builder.Services.AddVersionedApiExplorer(options =>
{
    // Sets the API versioning format
    options.GroupNameFormat = "'v'VVV";
    // Replaces the {apiVersion} placeholder with version number
    options.SubstituteApiVersionInUrl = true;
});

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
    options.AddPolicy(name: "AnyOrigin_GetOnly", cfg =>
    {
        cfg.AllowAnyOrigin();
        cfg.AllowAnyHeader();
        cfg.WithMethods("GET");
    });
});
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = projectName,
        Version = "v1.0"
    });
    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = projectName,
        Version = "v2.0"
    });
    options.SwaggerDoc("v3", new OpenApiInfo
    {
        Title = projectName,
        Version = "v3.0"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging()) {
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            $"/swagger/v1/swagger.json",
            $"{projectName} v1");
        options.SwaggerEndpoint(
            $"/swagger/v2/swagger.json",
            $"{projectName} v2");
        options.SwaggerEndpoint(
            "/swagger/v3/swagger.json",
            $"{projectName} v3");
    });
}

if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage"))
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/error");

app.UseHttpsRedirection();
app.UseCors("AnyOrigin");
app.UseAuthorization();

app.MapGet("/v{version:ApiVersion}/error",
    [ApiVersion("1.0")] [ApiVersion("2.0")] [EnableCors("AnyOrigin")] [ResponseCache(NoStore = true)]
    () => Results.Problem());
app.MapGet("/v{version:ApiVersion}/sex",
    [ApiVersion("v1.0")] [ApiVersion("v2.0")] [EnableCors("AnyOrigin")] [ResponseCache(NoStore = true)]
    () => { throw new ArgumentException(); });

app.MapGet("/v{version:ApiVersion}/cod/test",
    [ApiVersion("v1.0")] [ApiVersion("v2.0")] [EnableCors("AnyOrigin_GetOnly")] [ResponseCache(NoStore = true)]
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

app.MapControllers();

app.Run();