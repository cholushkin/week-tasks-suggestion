using System.Text.Json;

namespace WeekTasks
{
    public class TaskType
    {
        public string Description { get; set; }
        public List<int> WeeklyAmountDays { get; set; }
        public List<int> DailyAmount { get; set; }
        public double PickUpPriority { get; set; }
        public int SortingIndex { get; set; }
        public string Color { get; set; }

        // Load all task types from a JSON file
        public static Dictionary<string, TaskType> LoadFromJson(string filePath)
        {
            try
            {
                // Deserialize directly to Dictionary<string, TaskType>
                var jsonData = File.ReadAllText(filePath);
                var taskTypes = JsonSerializer.Deserialize<Dictionary<string, TaskType>>(jsonData);
                
                return taskTypes ?? throw new InvalidOperationException();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Error: The file {filePath} does not exist.");
                Environment.Exit(1);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading task configuration: {ex.Message}");
                Environment.Exit(1);
                return null;
            }
        }

        // Serialize a dictionary of task types to JSON and save to a file
        public static void SaveToJson(string filePath, Dictionary<string, TaskType> taskTypes)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(taskTypes, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving task configuration: {ex.Message}");
            }
        }
    }
}