using System;
using System.Collections.Generic;
using System.IO;

namespace WeekTasks
{
    public static class Tasks
    {
        public class Task
        {
            public enum Pref
            {
                FOCUS,
                WEEK_END,
                WEEK_MIDDLE,
                WEEK_START,
                WEEK_DAYS
            }
            
            public string TaskType;
            public string TaskID; // Updated from Task to TaskID for clarity
            public string Description;
            public double PickUpPriority;
            public (int from, int to) Days;
            public string Prefs;
            public string Remarks;
            public string Prompt;
            public string PromptResult;

            public bool HasPreference(Pref pref)
            {
                return Prefs.Contains(pref.ToString());
            }
            
        }

        // Load tasks from the directory and return a list of tasks
        public static List<Task> LoadFromDirectory(string directoryPath)
        {
            var taskList = new List<Task>(); // Create a new list to hold tasks

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Error: The directory {directoryPath} does not exist.");
                return null; // You might also consider returning an empty list instead
            }

            // Get all CSV files in the directory
            var csvFiles = Directory.GetFiles(directoryPath, "*.csv");

            foreach (var file in csvFiles)
            {
                LoadTasksFromFile(file, taskList); // Pass the list to fill
            }

            return taskList; // Return the populated list
        }

        private static void LoadTasksFromFile(string filePath, List<Task> taskList)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);

                // Skip the header and parse each line
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var columns = line.Split(';'); // Assumes columns are separated by semicolons

                    // Parse the columns into a Task object
                    var taskEntry = new Task
                    {
                        TaskType = columns[0],
                        TaskID = columns[1], // Use TaskID instead of Task
                        Description = ProcessStringValue(columns[2]),
                        PickUpPriority = double.TryParse(columns[3], out double priority) ? priority : 0.0,
                        Days = ParseDays(columns[4]),
                        Prefs = ProcessStringValue(columns[5]),
                        Remarks = ProcessStringValue(columns[6]),
                        Prompt = ProcessStringValue(columns[7])
                    };

                    taskList.Add(taskEntry); // Add the task entry to the provided list
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
            }
        }

        private static string ProcessStringValue(string strValue)
        {
            if (string.IsNullOrEmpty(strValue))
                return "";
            strValue = strValue.Trim();
            if (strValue == "-")
                strValue = "";
            return strValue;
        }

        private static (int from, int to) ParseDays(string days)
        {
            // Handle single day or range
            var parts = days.Split('-');
            if (parts.Length == 1 && int.TryParse(parts[0], out int singleDay))
            {
                // If there's only one number, set 'from' and 'to' to that number
                return (singleDay, singleDay);
            }
            else if (parts.Length == 2 && 
                     int.TryParse(parts[0], out int fromDay) && 
                     int.TryParse(parts[1], out int toDay))
            {
                // If it's a range, set 'from' and 'to' accordingly
                return (fromDay, toDay);
            }
            else
            {
                // Handle invalid input; default to (0, 0)
                return (0, 0);
            }
        }
    }
}
