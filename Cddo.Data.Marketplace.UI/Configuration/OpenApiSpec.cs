using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace Cddo.Data.Marketplace.UI.Configuration
{
    public class OpenApiSpec
    {
        public string Openapi { get; set; }
        public Info Info { get; set; }
        public Dictionary<string, PathItem> Paths { get; set; }
        public Components Components { get; set; }
        public List<Dictionary<string, List<string>>> Security { get; set; }
    }

    public class Info
    {
        public string Title { get; set; }
        public string Version { get; set; }
    }

    public class PathItem
    {
        public Operation Get { get; set; }
        public Operation Post { get; set; }
        public Operation Delete { get; set; }
        public Operation Patch { get; set; }
    }

    public class Operation
    {
        public List<string> Tags { get; set; }
        public string Description { get; set; }
        public List<Parameter> Parameters { get; set; }
        public Dictionary<string, Response> Responses { get; set; }
        public RequestBody RequestBody { get; set; } // Added for request bodies in POST/PATCH
    }

    public class Parameter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string In { get; set; }
        public bool? Required { get; set; } // Nullable for optional parameters
        public Schema Schema { get; set; }
    }

    public class Schema
    {
        public string Type { get; set; }
        [JsonProperty("$ref")]
        public string Ref { get; set; } // Used for $ref
        public string Format { get; set; } // Added for formats like UUID
        public bool? Nullable { get; set; } // Nullable schema
        public List<string> Enum { get; set; } // Used for enums
        [JsonProperty("items")]
        public Schema Items { get; set; } // For arrays
        public Dictionary<string, Schema> Properties { get; set; } // For object properties
        public List<string> Required { get; set; } // Required properties in objects
    }

    public class Response
    {
        public string Description { get; set; }
        public Dictionary<string, Content> Content { get; set; }
    }

    public class Content
    {
        public Schema Schema { get; set; }
    }

    public class RequestBody
    {
        public Dictionary<string, Content> Content { get; set; } // Request body content types
    }

    public class Components
    {
        public Dictionary<string, Schema> Schemas { get; set; }
        public Dictionary<string, SecurityScheme> SecuritySchemes { get; set; }
    }

    public class SecurityScheme
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public string In { get; set; }
    }


    public class TagLinks
    {
        public string TagName { get; set; }
        public string TagLink { get; set; }
        public string TagLabel { get; set; }
    }

    public class EndpointOperation
    {
        public KeyValuePair<OperationType, OpenApiOperation>  Operation { get; set; }
        public string OperationName { get; set; }
        public string OperationPath { get; set; }
        public string Title { get; set; }
    }
}
