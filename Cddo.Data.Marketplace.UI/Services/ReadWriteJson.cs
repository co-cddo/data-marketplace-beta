using System.Text.Json;

namespace Cddo.Data.Marketplace.UI.Services
{
    public class ReadWriteJson<T>
    {
        private readonly string _filePath;

        public ReadWriteJson(string fileName)
        {
            _filePath = $"Pages/Reports/ReportTemplates/{fileName}";
        }

        // Method to write a JSON string to a file
        public async Task WriteJsonAsync(T data)
        {
            try
            {
                // Serialize the object to a JSON string
                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                var directory = Path.GetDirectoryName(_filePath);
                if(!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(_filePath, jsonString);
                Console.WriteLine("JSON string written to " + _filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing JSON: " + ex.Message);
            }
        }

        // Method to read a JSON string from a file
        public async Task<T> ReadJsonAsync(string filePath)
        {
            try
            {
                // Read the JSON string from the file
                string jsonString = await File.ReadAllTextAsync(filePath);
                Console.WriteLine("Read JSON string from " + filePath);
                // Deserialize the JSON string back to an object
                return JsonSerializer.Deserialize<T>(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading JSON: " + ex.Message);
                return default;
            }
        }
    }
}
