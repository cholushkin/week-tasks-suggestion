using System.Text.Json;

namespace WeekTasks
{
    public static class Settings
    {
        public static Dictionary<string, string> LoadFromJson(string filePath)
        {
            try
            {
                var jsonData = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Error: The file '{filePath}' does not exist.");
                Environment.Exit(1);
                return null;
            }
            catch (JsonException)
            {
                Console.WriteLine($"Error: Failed to parse JSON from file '{filePath}'. Ensure the file contains a valid dictionary structure.");
                Environment.Exit(1);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Environment.Exit(1);
                return null;
            }
        }
    }
}
