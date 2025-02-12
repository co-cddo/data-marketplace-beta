using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Cddo.Data.Marketplace.UI.Configuration;

namespace Cddo.Data.Marketplace.UI.Pages.Support
{
    public class ApiModel : PageModel
    {
        public List<TagLinks>? Tags { get; set; } = new List<TagLinks>();

        public void OnGet()
        {
            try
            {
                string pathToJson = Directory.GetCurrentDirectory() + "/Pages/Support/swagger.json";
                string jsonContent = System.IO.File.ReadAllText(pathToJson);

                var apiDoc = JsonConvert.DeserializeObject<OpenApiSpec>(jsonContent);
                if (apiDoc != null)
                {
                    foreach (var item in apiDoc.Paths.Where(x => x.Key.Contains("DataMarketplaceApi")))
                    {
                        var pathItem = item.Value;
                        if (pathItem != null && pathItem.Get != null && pathItem.Get.Tags != null)
                        {
                            Tags.Add(new TagLinks()
                            {
                                TagName = pathItem.Get.Tags.First(),
                                TagLink = item.Key
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
