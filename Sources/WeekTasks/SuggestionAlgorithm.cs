
namespace WeekTasks
{
    public class SuggestionAlgorithm
    {
        public void Generate(Dictionary<string, string> settings, Dictionary<string, TaskType> taskTypes, List<Tasks.Task> taskList)
        {
            // Filter tasks that have PickUpPriority == "FOCUS"
            var focusTasks = taskList.Where(task => task.HasPreference(Tasks.Task.Pref.FOCUS));

            // Display each task's details
            Console.WriteLine("Tasks with PickUpPriority 'FOCUS':");
            foreach (var task in focusTasks)
            {
                Console.WriteLine($"Task ID: {task.TaskID}");
                Console.WriteLine($"Description: {task.Description}");
                Console.WriteLine($"Days: {task.Days.from} - {task.Days.to}");
                Console.WriteLine($"Preferences: {task.Prefs}");
                Console.WriteLine($"Remarks: {task.Remarks}");
                Console.WriteLine($"Prompt: {task.Prompt}");
                Console.WriteLine("-----------------------------------------");
            }
        }
    }
}