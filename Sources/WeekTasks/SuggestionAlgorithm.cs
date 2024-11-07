using WeekTasks.Utils.Random;

namespace WeekTasks
{
    public class SuggestionAlgorithm
    {
        private enum TaskPlacementResult
        {
            Success,
        }
        private const int DaysInWeek = 7; // in case you want to change your planing milestone length
        private const int MaxTasksPerDay = 8;
        // private List<Tasks.Task> _availableTasks;
        // private List<Tasks.Task>[] _weekDistribution;
        // private List<Tasks.Task>[] _weekBufferDistribution;
        
        private Dictionary<string, int> _taskTypesPlacedCount; // Track placed task counts by type

        private int _safetyIterationCounter;

        private readonly IReadOnlyDictionary<string, string> _settings;
        private readonly IReadOnlyDictionary<string, TaskType> _taskTypes;
        private readonly List<Tasks.Task> _taskList;
        private WeekDistribution _weekDistribution;

        public SuggestionAlgorithm(Dictionary<string, string> settings, Dictionary<string, TaskType> taskTypes, List<Tasks.Task> taskList)
        {
            _settings = settings;
            _taskTypes = taskTypes;
            _taskList = taskList;
        }

        public void Distribute()
        {
            _safetyIterationCounter = 0;
            _weekDistribution = FillToMaximum();
            PopulateWithTasks(_weekDistribution);
        }
        
        
        // Fill with not connected slots to the maximum state
        private WeekDistribution FillToMaximum()
        {
            var weekDistribution = new WeekDistribution(DaysInWeek, MaxTasksPerDay);
            
            foreach (var taskTypeDescriptor in _taskTypes)
            {
                var taskTypeName = taskTypeDescriptor.Key;
                var taskDailyAmountMax = taskTypeDescriptor.Value.DailyAmount.to;
                var taskWeeklyAmountMax = taskTypeDescriptor.Value.WeeklyAmountDays.to;

                for (int d = 0; d < taskWeeklyAmountMax; ++d)
                {
                    for (int t = 0; t < taskDailyAmountMax; ++t)
                    {
                        var taskSlot = new WeekDistribution.TaskSlot(taskTypeName);
                        weekDistribution.Week[d].Tasks.Add(taskSlot);
                    }
                }
            }

            if (_settings["log-level"] == "verbose")
                Console.WriteLine(weekDistribution.ToString());

            return weekDistribution;
        }

        private void PopulateWithTasks(WeekDistribution weekDistribution)
        {
            var focusTasks = _taskList.Where(task => task.HasPreference(Tasks.Task.Pref.FOCUS)).ToList();
            var regularTasks = _taskList.Where(task => !task.HasPreference(Tasks.Task.Pref.FOCUS)).ToList();
            var taskQueue = new Queue<List<Tasks.Task>>();
            taskQueue.Enqueue(focusTasks);
            taskQueue.Enqueue(regularTasks);
            
            while (taskQueue.Count > 0)
            {
                var currentTasks = taskQueue.Dequeue(); // focused or regular tasks to work with
                DistributeTasksImpl(currentTasks);
            }
        }

        private void DistributeTasksImpl(List<Tasks.Task> availableTasks)
        {
            while (availableTasks.Count > 0)
            {
                var availableTypes = GetAvailableTypesToPlace(_weekDistribution);
                
                var importantTypes = availableTypes
                    .OrderByDescending(taskTypeName => _taskTypes[taskTypeName].Importance)
                    .ToList();

                foreach (var taskType in importantTypes)
                {
                    var tasks = availableTasks.Where(t => t.TaskType == taskType).ToList();
                    if (tasks.Count > 0)
                    {
                        var taskToPlace = RandomHelper.Rnd.SpawnEvent(tasks, task => (float)task.PickUpPriority, out _);
                        availableTasks.Remove(taskToPlace);
                        TryToPlace(_weekDistribution, taskToPlace);
                    }
                }
                
                if (_safetyIterationCounter++ > 1000)
                    return;
            }
        }

        private bool TryToPlace(WeekDistribution weekDistribution, Tasks.Task taskToPlace)
        {
            WeekDistribution.TaskSlot GetEmptySlot(int dayIndex, string taskTypeName)
            {
                var slot =  weekDistribution.Week[dayIndex].Tasks.FirstOrDefault(t => t.TaskTypeName == taskTypeName);
                if (slot == null)
                    return null;
                if (slot.Task == null)
                    return slot;
                return null;
            }
            
            bool weekEndPreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_END);
            bool weekStartPreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_START);
            bool weekMiddlePreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_MIDDLE);
            bool specificDaysPreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_DAYS);
            var days = RandomHelper.Rnd.FromRangeInt(taskToPlace.Days);
            int placedCounter = 0;

            List<WeekDistribution.TaskSlot> emptySlots = new List<WeekDistribution.TaskSlot>();

            if (weekStartPreferable)
            {
                // Start from Monday (index 0) and go to the end of the week
                for (int i = 0; i < days; i++)
                {
                    var slot = GetEmptySlot(i, taskToPlace.TaskType);
                    if(slot == null)
                        break;
                    emptySlots.Add(slot);
                }
            }
            else if (weekMiddlePreferable)
            {
                // Start from Wednesday (index 3) and move forward
                int middleIndex = 3;
                for (int i = 0; i < days; i++)
                {
                    int dayToPlace = middleIndex + i;
                    if (dayToPlace >= DaysInWeek)
                        break;
                    var slot = GetEmptySlot(i, taskToPlace.TaskType);
                    if(slot == null)
                        break;
                    emptySlots.Add(slot);
                }
            }
            else if (weekEndPreferable)
            {
                // Start from Sunday (index 6) and move to the start of the week
                for (int i = 0; i < days; i++)
                {
                    var slot = GetEmptySlot(6 - i, taskToPlace.TaskType);
                    if(slot == null)
                        break;
                    emptySlots.Add(slot);
                }
                
            }
            else if (specificDaysPreferable)
            {
                throw new NotImplementedException();
            }

            // Can't event place a minimum amount on preferred position ?
            if (emptySlots.Count < taskToPlace.Days.from)
            {
                _weekBufferDistribution = CloneWeekDistribution(_weekDistribution);
                placedCounter = 0;

                // shuffle days of week and try to place requested amount in random days
                int[] rndDays = new[] { 0, 1, 2, 3, 4, 5, 6 };
                RandomHelper.Rnd.ShuffleInplace(rndDays);

                for (int i = 0; i < days; i++)
                {
                    if (TryToPlace(taskToPlace, rndDays[i])) // Distribute from Monday to Sunday
                        placedCounter++;
                }

                if (placedCounter >= taskToPlace.Days.from)
                    Place(_weekBufferDistribution, placedCounter, taskToPlace);
            }
            
            
        }

        private HashSet<string> GetAvailableTypesToPlace(WeekDistribution weekDistribution)
        {
            var availableSlotNames = new HashSet<string>();

            // Iterate through each day in the week
            foreach (var day in weekDistribution.Week)
            {
                // Iterate through each task slot in the day
                foreach (var slot in day.Tasks)
                {
                    // Check if the slot has no assigned task (Task is null)
                    if (slot.Task == null)
                    {
                        // Add the TaskTypeName of the available slot to the list
                        availableSlotNames.Add(slot.TaskTypeName);
                    }
                }
            }
            return availableSlotNames;
        }



        // public void Generate(Dictionary<string, string> settings, Dictionary<string, TaskType> taskTypes, List<Tasks.Task> taskList)
        // {
        //     Debug.Assert(DaysInWeek == 7);
        //     _availableTasks = new List<Tasks.Task>(taskList);
        //     _weekDistribution = new List<Tasks.Task>[DaysInWeek];
        //     for (int i = 0; i < _weekDistribution.Length; i++)
        //         _weekDistribution[i] = [];
        //     _safetyIterationCounter = 0;
        //     
        //     while (!IsCompleted())
        //     {
        //         var focusTasks = taskList.Where(task => task.HasPreference(Tasks.Task.Pref.FOCUS)).ToArray();
        //         if (focusTasks.Length > 0)
        //         {
        //             var taskToPlace = RandomHelper.Rnd.SpawnEvent(focusTasks, task => (float)task.PickUpPriority, out _);
        //             _weekBufferDistribution = CloneWeekDistribution(_weekDistribution);
        //             var days = RandomHelper.Rnd.FromRangeInt(taskToPlace.Days);
        //             
        //             Debug.Assert(days >= 0 && days <= DaysInWeek);
        //             
        //             bool weekEndPreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_END);
        //             bool weekStartPreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_START);
        //             bool weekMiddlePreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_MIDDLE);
        //
        //             int placedCounter = 0;
        //             
        //             if (weekStartPreferable)
        //             {
        //                 // Start from Monday (index 0) and go to the end of the week
        //                 for (int i = 0; i < days; i++)
        //                 {
        //                     if (TryToPlace(taskToPlace, i)) // Distribute from Monday to Sunday
        //                         placedCounter++;
        //                 }
        //             }
        //             else if (weekMiddlePreferable)
        //             {
        //                 // Start from Wednesday (index 3) and move forward
        //                 int middleIndex = 3;
        //                 for (int i = 0; i < days; i++)
        //                 {
        //                     int dayToPlace = middleIndex + i;
        //                     if (dayToPlace >= DaysInWeek)
        //                         break;
        //                     if( TryToPlace(taskToPlace, dayToPlace) )
        //                         placedCounter++;
        //                 }
        //             }
        //             else if (weekEndPreferable)
        //             {
        //                 // Start from Sunday (index 6) and move to the start of the week
        //                 for (int i = 0; i < days; i++)
        //                 {
        //                     if(TryToPlace(taskToPlace, 6 - i)) // Distribute from Sunday to Monday
        //                         placedCounter++;
        //                 }
        //             }
        //
        //             // Can't event place a minimum amount on preferred position ?
        //             if (placedCounter < taskToPlace.Days.from)
        //             {
        //                 _weekBufferDistribution = CloneWeekDistribution(_weekDistribution);
        //                 placedCounter = 0;
        //                 
        //                 // shuffle days of week and try to place requested amount in random days
        //                 int[] rndDays = new[] { 0, 1, 2, 3, 4, 5, 6 };
        //                 RandomHelper.Rnd.ShuffleInplace(rndDays);
        //                 
        //                 for (int i = 0; i < days; i++)
        //                 {
        //                     if (TryToPlace(taskToPlace, rndDays[i])) // Distribute from Monday to Sunday
        //                         placedCounter++;
        //                 }
        //
        //                 if (placedCounter >= taskToPlace.Days.from)
        //                     Place(_weekBufferDistribution, placedCounter, taskToPlace);
        //             }
        //             else
        //             {
        //                 Place(_weekBufferDistribution, placedCounter, taskToPlace);
        //             }
        //         }
        //         else
        //         {
        //             
        //         }
        //         _safetyIterationCounter++;
        //     }
        //     
        //     Console.WriteLine($"Seed: {RandomHelper.Rnd.GetState().AsNumber()}");
        // }
        //
        // private void Place(List<Tasks.Task>[] weekBufferDistribution, int placedCounter, Tasks.Task focusedTask)
        // {
        //     _weekDistribution = CloneWeekDistribution(weekBufferDistribution);
        //     UpdateTaskTypesCount(focusedTask.TaskType, placedCounter);
        //     _availableTasks.Remove(focusedTask);
        // }
        //
        // private bool TryToPlace(Tasks.Task task, int dayOfWeekIndex)
        // {
        //     var day = _weekBufferDistribution[dayOfWeekIndex];
        //     if (day.Count >= MaxTasksPerDay)
        //         return false;
        //     day.Add(task);
        //     return true;
        // }
        //
        //
        // private void UpdateTaskTypesCount(string taskType, int placedCount)
        // {
        //     if (!_taskTypesPlacedCount.TryAdd(taskType, placedCount))
        //         _taskTypesPlacedCount[taskType] += placedCount;
        // }
        //
        // private List<Tasks.Task>[] CloneWeekDistribution(List<Tasks.Task>[] weekDistribution)
        // {
        //     var clonedDistribution = new List<Tasks.Task>[weekDistribution.Length];
        //     for (int i = 0; i < weekDistribution.Length; i++)
        //     {
        //         // Create a new list for each day and add a copy of the tasks
        //         clonedDistribution[i] = new List<Tasks.Task>(weekDistribution[i]);
        //     }
        //     return clonedDistribution;
        // }
        //
    }
}