using static Cddo.Data.Marketplace.Audit.EventTypes;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Agm.Catalog.DotNet.Core.Utilities;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;

namespace Cddo.Data.Marketplace.Audit
{
    public static class LoggerExtensions
    {
        private static IConfiguration Configuration;
        public static void Initialize(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static void LogAdminEvent(this AppInsightsLogger logger, AdminAuditEvent adminEvent, string pageName, string client, string action, string subject, string setting, Dictionary<string, string> additionalProperties = null)
        {
            LogEventMain(logger, adminEvent, pageName, client, action, subject, setting, additionalProperties);
        }

        private static void AddAdditionalProperties(Dictionary<string, string> properties, Dictionary<string, string> additionalProperties)
        {
            if (additionalProperties.ContainsKey("organisation") || additionalProperties.ContainsKey("OrganisationInformation"))
            {
                var orgDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(additionalProperties.ContainsKey("organisation") ? additionalProperties["organisation"] : additionalProperties["OrganisationInformation"]);
                properties.Add("OrganisationId", orgDetails["organisationId"].ToString());
                properties.Add("OrganisationName", orgDetails["organisationName"].ToString());

                if (orgDetails.ContainsKey("domains"))
                {
                    var domainDetails = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(orgDetails["domains"].ToString());
                    properties.Add("DomainId", domainDetails.First()["domainId"].ToString());
                    properties.Add("DomainName", domainDetails.First()["domainName"].ToString());
                }
            }

            if (additionalProperties.ContainsKey("domain"))
            {
                var domainDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(additionalProperties["domain"]);
                properties.Add("DomainId", domainDetails["domainId"].ToString());
                properties.Add("DomainName", domainDetails["domainName"].ToString());
            }

            if (additionalProperties.ContainsKey("roles"))
            {
                var roles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(additionalProperties["roles"]);
                var roleDescriptions = new List<string>();
                foreach (var role in roles)
                {
                    roleDescriptions.Add($"{role["roleName"]} ({role["roleId"]})");
                }
                properties.Add("UserRoles", string.Join(", ", roleDescriptions));
            }
            if (additionalProperties.ContainsKey("ErrorDetails"))
            {
                properties.Add("ErrorDetails", additionalProperties["ErrorDetails"].ToString());
            }

            // If this is a validation error then log everything that has been provided
            if (properties.TryGetValue("EventCategory", out var eventCategoryString) && Enum.TryParse<ErrorEvent>(eventCategoryString, out var errorEventType) && errorEventType == ErrorEvent.ValidationError)
            {
                var validationErrorPropertyNames = new List<string>
                {
                    "CatalogAssetField", "ActionSource", "DataAssetPropertyName", "ErrorMessage", "ErrorSeverity", "ErrorType"
                };

                foreach (var validationErrorPropertyName in validationErrorPropertyNames)
                {
                    if (additionalProperties.TryGetValue(validationErrorPropertyName, out var validationPropertyValue))
                    {
                        properties.TryAdd(validationErrorPropertyName, validationPropertyValue);
                    }
                }
            }

        }

        public static void LogUserEvent(this AppInsightsLogger logger, UserEvent userEvent, string pageName, string client, Dictionary<string, string> additionalProperties = null)
        {
            LogEventMain(logger, userEvent, pageName, client, "", "", "", additionalProperties);

        }

        public static void LogDataSharingEvent(this AppInsightsLogger logger, DataSharingEvent adminEvent, string pageName, string client, string action, string subject, string setting, Dictionary<string, string> additionalProperties = null)
        {
            LogEventMain(logger, adminEvent, pageName, client, action, subject, setting, additionalProperties);
        }

        public static void LogDataAssetValidationError(this AppInsightsLogger logger,
            CatalogAssetField catalogAssetField,
            DataAssetActionSourceEnum actionSource,
            string dataAssetPropertyName,
            string errorMessage,
            DataAssetValidationPropertyErrorSeverity errorSeverity,
            DataAssetPropertyValidationErrorType errorType,
            IUserDetails initiatingUserDetails)
        {
            IEnumMemberConverter enumMemberConverter = new EnumMemberConverter(); // Can't inject because this is an extension method

            var catalogAssetFieldString = enumMemberConverter.GetEnumMemberValue(catalogAssetField);
            var actionSourceString = enumMemberConverter.GetEnumMemberValue(actionSource);
            var errorSeverityString = enumMemberConverter.GetEnumMemberValue(errorSeverity);
            var errorTypeString = enumMemberConverter.GetEnumMemberValue(errorType);

            var additionalProperties = new Dictionary<string, string>
            {
                { "CatalogAssetField", catalogAssetFieldString },
                { "ActionSource", actionSourceString },
                { "DataAssetPropertyName", dataAssetPropertyName },
                { "ErrorMessage", errorMessage },
                { "ErrorSeverity", errorSeverityString },
                { "ErrorType", errorTypeString }
            };

            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);
            foreach (var userEventProperty in userEventProperties)
            {
                additionalProperties.Add(userEventProperty.Key, userEventProperty.Value);
            }

            LogEventMain(logger, EventTypes.ErrorEvent.ValidationError, string.Empty, "CDDO", "Error", "DataAssetValidationError", string.Empty, additionalProperties);
        }

        public static void LogEventMain<TEnum>(this AppInsightsLogger logger, TEnum eventType, string pageName, string client, string action, string subject, string setting, Dictionary<string, string> additionalProperties = null) where TEnum : Enum
        {
            bool isGdprCompliant = Configuration.GetValue<bool>("GDPR");

            try
            {
                var properties = new Dictionary<string, string>
                {
                    {"Client", client},
                    {"EventCategory", eventType.ToString()},
                    {"EventId", ((int)(object)eventType!).ToString()}, // Log the numeric value of the enum
                    {"Action", action}, // Describes the action the admin took
                    {"Subject", subject}, // The target or subject of the action
                    {"Value", setting} // Value that was set
                };

                if (!string.IsNullOrEmpty(pageName))
                {
                    properties.Add("PageName", pageName);
                }

                if (additionalProperties != null)
                {
                    if (additionalProperties.ContainsKey("user") || additionalProperties.ContainsKey("UserIdSet"))
                    {
                        var userDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(additionalProperties.ContainsKey("user") ? additionalProperties["user"] : additionalProperties["UserIdSet"]);
                        properties.Add("UserId", userDetails["userId"].ToString()); // Always log UserId

                        if (!isGdprCompliant)
                        {
                            if (additionalProperties.ContainsKey("user"))
                            {
                                properties.Add("UserEmail", userDetails["userEmail"].ToString());
                                properties.Add("UserName", userDetails["userName"].ToString());
                            }
                            else
                            {
                                var userContactDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(additionalProperties["UserContactDetails"]);
                                properties.Add("UserEmail", userContactDetails["emailAddress"].ToString());
                                properties.Add("UserName", userContactDetails["userName"].ToString());
                            }

                        }
                    }
                    if (additionalProperties.TryGetValue("DataShareRequestId", out var dataShareRequestId))
                    {
                        properties.Add("DataShareRequestId", dataShareRequestId);
                    }
                    if (additionalProperties.TryGetValue("MetadataId", out var metadataId))
                    {
                        properties.Add("MetadataId", metadataId);
                    }
                    if (additionalProperties.TryGetValue("Title", out var title))
                    {
                        properties.Add("Title", title);
                    }
                    if (additionalProperties.ContainsKey("ReferrerUrl"))
                    {
                        //var referrerDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(additionalProperties["ReferrerUrl"]);
                        properties.Add("ReferrerUrl", additionalProperties["ReferrerUrl"].ToString());
                    }

                    // Include other user context information as needed
                    AddAdditionalProperties(properties, additionalProperties);
                }

                // Log the event with all properties
                logger.LogEvent(eventType, properties);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

}
