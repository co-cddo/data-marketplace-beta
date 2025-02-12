using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cddo.Data.Marketplace.Api
{
    public class SwaggerFileUploadOperation : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileUploadMime = "multipart/form-data";
            if (operation.RequestBody != null && operation.RequestBody.Content.ContainsKey(fileUploadMime))
            {
                operation.RequestBody.Content[fileUploadMime].Schema.Properties =
                    new Dictionary<string, OpenApiSchema>
                    {
                    {
                        "file", new OpenApiSchema
                        {
                            Description = "Upload File",
                            Type = "file",
                            Format = "binary"
                        }
                    }
                    };
            }
        }
    }
}
