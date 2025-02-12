using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CDDO.DataMarketplace.Controllers.External
{
    public class JsonStringEnumWithEnumMemberConverter<T> : JsonConverter<T> where T : Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var enumString = reader.GetString();
            var enumType = typeof(T);

            foreach (var field in enumType.GetFields())
            {
                // Look for EnumMember attribute
                var enumMemberAttribute = field.GetCustomAttributes(typeof(EnumMemberAttribute), false)
                    .Cast<EnumMemberAttribute>()
                    .FirstOrDefault();

                // Check if the EnumMember value matches the incoming JSON string
                if (enumMemberAttribute != null && enumMemberAttribute.Value?.Equals(enumString, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return (T)field.GetValue(null);
                }

                // Also check if the enum name itself matches the incoming string
                if (field.Name.Equals(enumString, StringComparison.OrdinalIgnoreCase))
                {
                    return (T)field.GetValue(null);
                }
            }

            throw new JsonException($"Unable to convert \"{enumString}\" to Enum {enumType.Name}.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var enumType = typeof(T);
            var enumField = enumType.GetField(value.ToString());

            // Look for EnumMember attribute
            var enumMemberAttribute = enumField?.GetCustomAttributes(typeof(EnumMemberAttribute), false)
                .Cast<EnumMemberAttribute>()
                .FirstOrDefault();

            // Write EnumMember value or enum name if EnumMember is not present
            writer.WriteStringValue(enumMemberAttribute?.Value ?? value.ToString());
        }
    }
}

