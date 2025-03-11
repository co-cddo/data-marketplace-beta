using Cddo.Data.Marketplace.UI.Configuration;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Cddo.Data.Marketplace.UI.Controllers;

[Route("Support")]
public class SupportController : Controller
{
    public SupportController()
    {
    }

    [Route("get-started")]
    public async  Task<IActionResult> GetStarted()
    {
        List<TagLinks> tags = await SetEndpointLinks();
        ViewData["Tags"] = tags;
        return View("~/Pages/Support/Index.cshtml");
    }

    [Route("roles")]
    public async Task<IActionResult> Roles()
    {
        List<TagLinks> tags = await SetEndpointLinks();
        ViewData["Tags"] = tags;
        return View("~/Pages/Support/Roles.cshtml");
    }

    [Route("requesting-restricted-data")]
    public async Task<IActionResult> RequestingRestrictedData()
    {
        List<TagLinks> tags = await SetEndpointLinks();

        ViewData["Tags"] = tags;

        return View("~/Pages/Support/DataShareArrangement.cshtml");
    }

    [Route("data-share-request-questions")]
    public async Task<IActionResult> DataShareQuestions()
    {
        List<TagLinks> tags = await SetEndpointLinks();

        ViewData["Tags"] = tags;
        return View("~/Pages/Support/DataShareQuestions.cshtml");
    }
    [Route("support-api")]
    public async Task<IActionResult> ApiSupport()
    {
        List<TagLinks> tags = await SetEndpointLinks();

        ViewData["Tags"] = tags;
        return View("~/Pages/Support/Api.cshtml", tags);
    }

    private async Task<List<TagLinks>> SetEndpointLinks()
    {
        OpenApiDocument document = await ApiCallGetSchemaDocumentAsync();

        var tags = new List<TagLinks>();
        if (document != null)
        {
            var allowedOperations = GetAllowedOperations();
            foreach (var item in document.Paths)
            {
                //var pathItem = item.Value;
                if (item.Value != null && item.Value.Operations != null)
                {
                    //if (item.Value.Operations.Count() > 1)
                    //{
                        foreach (var operation in GetOperationsWithKey(item.Value.Operations, allowedOperations))
                        {
                            tags.Add(new TagLinks()
                            {
                                //TODO - Tagname comes from OperationId because menu links are hard-coded.
                                TagName = operation.OperationId,
                                TagLink = item.Key,
                                TagLabel = allowedOperations[operation.OperationId]
                            });
                        }
                    //}
                    //else
                    //{
                    //    tags.Add(new TagLinks()
                    //    {
                    //        //TODO - Tagname comes from OperationId because menu links are hard-coded.
                    //        TagName = allowedOperations[item.Value.Operations.First().Value.OperationId],
                    //        TagLink = item.Key
                    //    });
                    //}

                }
            }
        }
        return tags;
    }

    public static List<OpenApiOperation> GetOperationsWithKey(IDictionary<OperationType, OpenApiOperation> operations, Dictionary<string, string> keyDict)
    {
        // Filter operations that have parameters matching any of the values in the keyDict
        return operations.Values.Where(op => keyDict.Keys.Contains(op.OperationId)).ToList();
    }

    public static Dictionary<string, string> GetAllowedOperations()
    {
        // Create and return a list of strings
        return new Dictionary<string, string>
        {
            {"queryDataAssets", "Search your datasets" },
            {"CreateDataSet" , "Submit dataset" },
            {"RetrieveDataset", "Get dataset"  },
            {"UpdateDataset", "Update dataset"  },
            { "RemoveDataset", "Delete dataset" }
        };
    }
    private string SetLabelByOperationId(string operationId)
    {
        throw new NotImplementedException();
    }

    [Route("api-spec")]
    public async Task<IActionResult> GetEndpointData(string apiPath, string operationId)
    {
        List<TagLinks> tags = await SetEndpointLinks();
        ViewData["Tags"] = tags;

        OpenApiDocument document = await ApiCallGetSchemaDocumentAsync();

        // Read JSON content from the file
        //string jsonContent = System.IO.File.ReadAllText(pathToJson);

        //var apiDoc = JsonConvert.DeserializeObject<OpenApiSpec>(jsonContent);

        var endPointDetails = document.Paths[apiPath];
        if (endPointDetails != null)
        {
            return SetDetails(endPointDetails, apiPath, operationId);
        }

        return View("~/Pages/Support/ApiSpec.cshtml", endPointDetails);
    }

    private async Task<OpenApiDocument> ApiCallGetSchemaDocumentAsync()
    {
        return await ReadOpenApiDocumentFromUrlAsync(); ;
    }

    private IActionResult SetDetails(OpenApiPathItem pathItem, string apiPath, string OperationId)
    {
        EndpointOperation endpointOperation = new();
        endpointOperation.Operation = pathItem.Operations.Where(x=>x.Value.OperationId == OperationId).First();
        endpointOperation.OperationPath = apiPath.ToLower();
        //Sort out the titles
        var allowedOperations = GetAllowedOperations();
        endpointOperation.Title = allowedOperations[OperationId];

        return View("~/Pages/Support/ApiSpec.cshtml", endpointOperation);
    }

    private static async Task<OpenApiDocument> ReadOpenApiDocumentFromUrlAsync()
    {
        var url = "https://raw.githubusercontent.com/co-cddo/data-catalogue-metadata/5470a1874bded172cac410c9d253a65ea176cefb/api_specification/metadata_management_api.yaml";
        using HttpClient httpClient = new HttpClient();

        // Fetch OpenAPI JSON/YAML from the URL
        var stream = await httpClient.GetStreamAsync(url);

        // Parse the OpenAPI document from the stream
        var openApiReader = new OpenApiStreamReader();
        var openApiDocument = openApiReader.Read(stream, out var diagnostic);

        // Check for parsing errors
        if (diagnostic.Errors.Count > 0)
        {
            throw new InvalidOperationException($"Parsing errors: {string.Join(", ", diagnostic.Errors)}");
        }

        return openApiDocument;
    }
}
