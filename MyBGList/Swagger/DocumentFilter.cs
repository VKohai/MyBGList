using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MyBGList.Swagger;

public class DocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Info.Title = "MyBGList Web API :3";
        swaggerDoc.Info.Contact = new OpenApiContact
        {
            Name = "VKohai",
            Url = new Uri("https://t.me/VKohai")
        };
        swaggerDoc.Info.License = new OpenApiLicense
        {
            Name = "The MIT License",
            Url = new Uri("https://mit-license.org")
        };
    }
}