using Cddo.Data.Marketplace.Api.Dto.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json;

namespace Cddo.Data.Marketplace.Audit
{
    public static class AuditUtility
    {
        public static Dictionary<string, string> ConvertUserProfileToJSONDictionary<T>(T profile)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!EqualityComparer<T>.Default.Equals(profile, default(T)))
            {
                foreach (PropertyInfo propertyInfo in profile.GetType().GetProperties())
                {
                    object propValue = propertyInfo.GetValue(profile, null);
                    if (propValue != null)
                    {
                        string jsonValue = JsonConvert.SerializeObject(propValue, settings);
                        result[propertyInfo.Name] = jsonValue;
                    }
                }
            }

            return result;
        }

        public static Dictionary<string, string> GetPlaceholderUserProfileDictionary()
        {
            var placeholderProfile = new UserProfile
            {
                User = new UserInfo { UserId = -1, UserEmail = "unkown@user.com", UserName = "No User" },
                Domain = new UserDomain { DomainId = -1, DomainName = "No Domain", IsEnabled = false },
                Organisation = new UserOrganisation { OrganisationId = -1, OrganisationName = "No Organisation", IsEnabled = false },
                Roles = new List<Role> { new Role { RoleId = -1, RoleName = "No Role", Description = "No Role Assigned" } },
                EmailNotification = false,
                WelcomeNotification = false,
                LastLogin = default(DateTime) // or DateTime.MinValue for clarity
            };

            return ConvertUserProfileToJSONDictionary(placeholderProfile);
        }

        public static async Task<Dictionary<string, string>> ParseResponseToDictionary(HttpResponseMessage response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            // Check if the content type is JSON
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var dictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse, options);

                    return dictionary.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ValueKind == JsonValueKind.Object || kvp.Value.ValueKind == JsonValueKind.Array
                            ? kvp.Value.GetRawText()  // For nested objects or arrays, return the raw JSON text
                            : kvp.Value.ToString()   // Convert non-nested elements directly to string
                    );
                }
                catch (System.Text.Json.JsonException ex)
                {
                    Console.WriteLine($"JSON parsing failed: {ex.Message}");
                    return new Dictionary<string, string>(); // Return an empty dictionary or handle accordingly
                }
            }
            else
            {
                // Handle plain text response
                var textResponse = await response.Content.ReadAsStringAsync();
                return new Dictionary<string, string>
            {
                {"Response", textResponse}
            };
            }
        }
        public static Dictionary<string, string> DecodeJwtToDictionary(string jwt)
        {
            var details = new Dictionary<string, string>();
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);

                foreach (var claim in token.Claims)
                {
                    if (!details.ContainsKey(claim.Type))
                    {
                        details.Add(claim.Type, claim.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                details.Add("Error", "Failed to decode JWT: " + ex.Message);
            }
            return details;
        }
    }
}
