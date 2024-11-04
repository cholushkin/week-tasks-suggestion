using System.CommandLine;
using System.Text.Json;

namespace WeekTasks
{
    class Program
    {
        private const string DefaultSettingsPath = "Data/Settings.json";

        static int Main(string[] args)
        {
            SetDefaultColorScheme();

            var startDateOption = new Option<string>(
                "--start-date",
                description: "The start date in DD-MMM-YYYY format (example: 04-Nov-2024). Defaults to next Monday if not specified.");

            var settingsOption = new Option<string>(
                "--settings",
                description:
                "Path to the task configuration JSON file. Defaults to 'configuration\\settings.json' if not specified.");

            // Create root command and add options
            var rootCommand = new RootCommand("Week tasks suggestion utility")
            {
                startDateOption,
                settingsOption
            };

            rootCommand.SetHandler((string startDate, string settingsPath) =>
            {
                // Set default values for options if they are not provided
                startDate = GetNextMonday();
                settingsPath = DefaultSettingsPath;

                // Load settings from JSON file
                var settings = Settings.LoadFromJson(settingsPath);
                
                // Extend settings with some dynamic settings
                settings["start-date"] = startDate;
                settings["output-file"] = $"{settings["output-dir"]}/{startDate}.md";

                // Display settings if verbose mode is enabled
                if (settings["log-level"] == "verbose")
                {
                    Console.WriteLine(
                        $"Settings : {JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true })}\n");
                }
                
                // // Load task types configuration
                // var taskTypes = LoadTaskTypeConfiguration(configuration["task-types-configuration"].ToString());
                //
                // if (configuration.ContainsKey("verbose") && configuration["verbose"].ToString().ToLower() == "true")
                // {
                //     Console.WriteLine($"Loaded task types from: {configuration["task-types-configuration"]}");
                // }
            }, startDateOption, settingsOption);

            return rootCommand.Invoke(args);
        }
        
        private static void SetDefaultColorScheme()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();
        }
        
        private static string GetNextMonday()
        {
            var today = DateTime.Now;
            int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            var nextMonday = today.AddDays(daysUntilNextMonday); 
            return nextMonday.ToString("dd-MMM-yyyy");
        }




        static Dictionary<string, TaskType> LoadTaskTypeConfiguration(string filePath)
        {
            try
            {
                var jsonData = File.ReadAllText(filePath);
                var taskData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(jsonData);

                var taskTypes = new Dictionary<string, TaskType>();
                foreach (var (tag, details) in taskData)
                {
                    taskTypes[tag] = TaskType.FromDictionary(details);
                }

                return taskTypes;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Error: The file {filePath} does not exist.");
                Environment.Exit(1);
                return null;
            }
        }
    }
}