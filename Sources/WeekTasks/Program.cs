using System.CommandLine;
using System.Text.Json;
using WeekTasks.Utils.Random;

namespace WeekTasks
{
    static class WeekTasksProgram
    {
        private const string DefaultSettingsPath = "Data/Settings.json";
        
        private static Dictionary<string, string> Settings;
        private static Dictionary<string, TaskType> TaskTypes;
        private static List<Tasks.Task> TaskList { get; set; } = [];
        private static MessageOfTheDay MessageOfTheDay;

        static async Task<int> Main(string[] args)
        {
            SetDefaultColorScheme();

            var startDateOption = new Option<string>(
                "--start-date",
                description: "The start date in DD-MMM-YYYY format (example: 04-Nov-2024). Defaults to next Monday if not specified.");

            var settingsOption = new Option<string>(
                "--settings",
                description:
                $"Path to the task configuration JSON file. Defaults to '{DefaultSettingsPath}' if not specified.");

            var seedOverrideOption = new Option<int?>(
                "--seed",
                description: "Override seed to have same results"
            );

            // Create root command and add options
            var rootCommand = new RootCommand("Week tasks suggestion utility")
            {
                startDateOption,
                settingsOption,
                seedOverrideOption
            };

            rootCommand.SetHandler((string startDate, string settingsPath, int? seed) =>
            {
                settingsPath ??= DefaultSettingsPath;
                startDate ??= GetNextMonday();
                
                if (seed.HasValue)
                    RandomHelper.Rnd.SetState(new LinearConRng.State(seed.Value));
                
                LoadData(settingsPath, startDate);
                
                Settings["start-date"] = startDate;
                Settings["output-file"] = $"{Settings["output-dir"]}/{startDate}.md";
            }, startDateOption, settingsOption, seedOverrideOption);

            // Start execution point 
            var result = rootCommand.Invoke(args);
            
            // Display settings if verbose mode is enabled
            if (Settings["log-level"] == "verbose")
            {
                Console.WriteLine(
                    $"Using these settings : {JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true })}\n"
                );
                Console.WriteLine($"Loaded {TaskTypes.Count} task types from {Settings["task-types"]}\n");
                Console.WriteLine($"Loaded {TaskList.Count} tasks from {Settings["tasks-dir"]} directory\n");
                Console.WriteLine($"Loaded {MessageOfTheDay.MessageOfTheDayStrings.Count} messages of the day from {Settings["motd"]}\n");
            }
            
            Console.WriteLine($"RNG seed: {RandomHelper.Rnd.GetState().AsNumber()}\n");

            var suggestionAlgorithm = new SuggestionAlgorithm(Settings, TaskTypes, TaskList);
            suggestionAlgorithm.Distribute();
            await suggestionAlgorithm.ProcessPrompt(); 
            suggestionAlgorithm.PrintWeekDistribution();

            if (Settings["obsidian-vault-dir"] != null)
            {
                var motd = MessageOfTheDay.LoadFromTextFile(Settings["motd"]);
                DateTime startDate = DateTime.Parse(Settings["start-date"]);
                var md = new MarkdownFileWriter(
                    startDate,
                    RandomHelper.Rnd.GetState().AsNumber().ToString(),
                    Settings["obsidian-vault-dir"], motd);
                md.Write(suggestionAlgorithm.WeekDistribution);
            }
            
            Console.WriteLine("Program completed.");
            return result;
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
        
        private static void LoadData(string settingsPath, string startDate)
        {
            Settings = WeekTasks.Settings.LoadFromJson(settingsPath);

            if (Settings.TryGetValue("task-types", out string taskTypesPath))
                TaskTypes = TaskType.LoadFromJson(taskTypesPath);

            TaskList = Tasks.LoadFromDirectory(Settings["tasks-dir"]);
            MessageOfTheDay = MessageOfTheDay.LoadFromTextFile(Settings["motd"]);
        }
    }
}