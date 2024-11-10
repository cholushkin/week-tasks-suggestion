namespace WeekTasks
{
    public class WeekDistribution
    {
        public class TaskSlot
        {
            public string TaskTypeName { get; private set; }
            public TaskSlot PreviousConnected { get; set; }
            public TaskSlot NextConnected { get; set; }
            public bool PlacedWithPreference;
            public bool PlacedFocused;

            public Tasks.Task Task
            {
                get => _task;
                set
                {
                    if (value != null)
                        TaskTypeName = value.TaskType;
                    _task = value;
                }
            }

            public TaskSlot(string taskTypeName)
            {
                TaskTypeName = taskTypeName;
            }

            // Deep copy constructor for TaskSlot
            public TaskSlot(TaskSlot other)
            {
                TaskTypeName = other.TaskTypeName;
            }

            private Tasks.Task _task;
        }

        public class Day
        {
            public List<TaskSlot> Tasks = new List<TaskSlot>();

            // Deep copy constructor for Day
            public Day(Day other)
            {
                // Deep copy each TaskSlot in the list
                foreach (var taskSlot in other.Tasks)
                {
                    Tasks.Add(new TaskSlot(taskSlot));
                }
            }

            public Day()
            {
            }
        }

        public List<Day> Week { get; private set; }

        // Constructor to initialize an empty WeekDistribution
        public WeekDistribution(int daysInWeek, int maxTasksPerDay)
        {
            Week = new List<Day>(daysInWeek);
            for (int d = 0; d < daysInWeek; d++)
            {
                Week.Add(new Day());
            }
        }

        // Deep copy constructor for WeekDistribution
        public WeekDistribution(WeekDistribution other)
        {
            // Deep copy each Day
            Week = new List<Day>(other.Week.Count);
            foreach (var day in other.Week)
            {
                Week.Add(new Day(day));
            }

            // Now we need to reconnect the TaskSlots' PreviousConnected and NextConnected links
            for (int dayIndex = 0; dayIndex < Week.Count; dayIndex++)
            {
                var originalDay = other.Week[dayIndex];
                var copiedDay = Week[dayIndex];

                for (int taskIndex = 0; taskIndex < originalDay.Tasks.Count; taskIndex++)
                {
                    var originalTask = originalDay.Tasks[taskIndex];
                    var copiedTask = copiedDay.Tasks[taskIndex];

                    // Reconnect PreviousConnected and NextConnected if they exist
                    if (originalTask.PreviousConnected != null)
                    {
                        int previousIndex = originalDay.Tasks.IndexOf(originalTask.PreviousConnected);
                        copiedTask.PreviousConnected = copiedDay.Tasks[previousIndex];
                    }

                    if (originalTask.NextConnected != null)
                    {
                        int nextIndex = originalDay.Tasks.IndexOf(originalTask.NextConnected);
                        copiedTask.NextConnected = copiedDay.Tasks[nextIndex];
                    }
                }
            }
        }

        public override string ToString()
        {
            var result = new System.Text.StringBuilder();

            for (int dayIndex = 0; dayIndex < Week.Count; dayIndex++)
            {
                result.Append($"Day {dayIndex + 1}: ");

                foreach (var taskSlot in Week[dayIndex].Tasks)
                {
                    result.Append($"{taskSlot.TaskTypeName} ");
                }
                result.AppendLine(); // Add an extra newline for better separation between days
            }

            return result.ToString();
        }
    }
}