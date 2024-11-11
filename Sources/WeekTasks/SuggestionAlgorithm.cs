using System.Diagnostics;
using WeekTasks.Utils.Random;

namespace WeekTasks
{
    public class SuggestionAlgorithm
    {
        private const int DaysInWeek = 7; // in case you want to change your planing milestone length
        private const int MaxTasksPerDay = 8;

        private Dictionary<string, int> _taskTypesPlacedCount; // Track placed task counts by type

        private int _safetyIterationCounter;

        private readonly IReadOnlyDictionary<string, string> _settings;
        private readonly IReadOnlyDictionary<string, TaskType> _taskTypes;
        private readonly List<Tasks.Task> _taskList;
        private WeekDistribution _weekDistribution;

        public WeekDistribution WeekDistribution => _weekDistribution;

        public SuggestionAlgorithm(Dictionary<string, string> settings, Dictionary<string, TaskType> taskTypes,
            List<Tasks.Task> taskList)
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
            Console.WriteLine("Before adjustment:");
            PrintWeekDistribution();
            Adjust(_weekDistribution);
        }

        public async Task ProcessPrompt()
        {

            
            // // Instantiate the OpenAIHelper with your API key
            // IArtificialIntelligence AIBridge = new LocalAI();
            //
            // // Filter tasks with a non-empty Prompt
            // var tasksWithPrompts = _taskList
            //     .Where(task => !string.IsNullOrWhiteSpace(task.Prompt) && task.Prompt.Length >= 3).ToList();
            //
            // foreach (var task in tasksWithPrompts)
            // {
            //     try
            //     {
            //         // Get AI response for each task's Prompt
            //         Console.WriteLine($"prompting: {task.Prompt}");
            //         var response = await AIBridge.GetResponseFromAIAsync(task.Prompt);
            //         task.AIResponce = response; // Set the AI response to the task's AIResponce property
            //     }
            //     catch (Exception ex)
            //     {
            //         Console.WriteLine($"Error processing task {task.TaskID}: {ex.Message}");
            //     }
            // }
        }
        
        
        private Dictionary<string, HashSet<int>> GetTaskUsage(WeekDistribution weekDistribution)
        {
            var taskTypeDayUsage = new Dictionary<string, HashSet<int>>();

            // Track which days each task type appears on
            for (int dayIndex = 0; dayIndex < DaysInWeek; dayIndex++)
            {
                foreach (var slot in weekDistribution.Week[dayIndex].Tasks)
                {
                    if (!taskTypeDayUsage.ContainsKey(slot.TaskTypeName))
                    {
                        taskTypeDayUsage[slot.TaskTypeName] = new HashSet<int>();
                    }
                    taskTypeDayUsage[slot.TaskTypeName].Add(dayIndex); // Track unique days
                }
            }

            return taskTypeDayUsage;
        }

        private void Adjust(WeekDistribution weekDistribution)
        {
            const int safeStepCounter = 4024;
            int i = 0;
            int levelOfStrict = 0;
            
            while (i++<safeStepCounter)
            {
                var k = i / (float)safeStepCounter;
                levelOfStrict = (int)(k * 3); // 0..1..2
                
                var taskTypeDayUsage = GetTaskUsage(_weekDistribution);
                var day = RandomHelper.Rnd.FromList(_weekDistribution.Week);
                var task = RandomHelper.Rnd.FromList(day.Tasks);
                var maxWeeklyAmount = _taskTypes[task.TaskTypeName].WeeklyAmountDays.to;
                var dailyAmountMax = _taskTypes[task.TaskTypeName].DailyAmount.to;
                var deleteOnlyDisconnected = true;

                if (levelOfStrict > 0)
                {
                    maxWeeklyAmount = (_taskTypes[task.TaskTypeName].WeeklyAmountDays.from +
                                       _taskTypes[task.TaskTypeName].WeeklyAmountDays.to) / 2;
                    dailyAmountMax = Math.Max(1, dailyAmountMax - 1);
                }

                if (levelOfStrict > 1)
                {
                    deleteOnlyDisconnected = false;
                    maxWeeklyAmount = _taskTypes[task.TaskTypeName].WeeklyAmountDays.from;
                    
                }
                
                var taskTypeThisDayCount = day.Tasks.Count(x => x.TaskTypeName == task.TaskTypeName);
                if (taskTypeThisDayCount > dailyAmountMax)
                {
                    if(day.Tasks.Count <= MaxTasksPerDay)
                        continue;
                    if(task.PlacedFocused)
                        continue;
                    if(task.PlacedWithPreference)
                        continue;
                    if (deleteOnlyDisconnected)
                    {
                        if (task.PreviousConnected != null)
                            continue;
                        if (task.NextConnected != null)
                            continue;
                    }
                    day.Tasks.Remove(task);
                    Console.WriteLine("b.removing "+ task.TaskTypeName);
                }


                // task used more then max week amount in settings 
                if (taskTypeDayUsage[task.TaskTypeName].Count > maxWeeklyAmount)
                {
                    if(day.Tasks.Count <= MaxTasksPerDay)
                        continue;
                    if(task.PlacedFocused)
                        continue;
                    if(task.PlacedWithPreference)
                        continue;
                    if (deleteOnlyDisconnected)
                    {
                        if (task.PreviousConnected != null)
                            continue;
                        if (task.NextConnected != null)
                            continue;
                    }


                    
                    day.Tasks.Remove(task);
                    Console.WriteLine("a.removing "+ task.TaskTypeName);
                }
                
                var successCounter = 0;
                foreach (var d in weekDistribution.Week)
                {
                    if (d.Tasks.Count <= MaxTasksPerDay)
                        successCounter++;
                }

                if (successCounter == DaysInWeek)
                {
                    Console.WriteLine("Adjusting done, condition reached");
                    break;
                }
            }
        }


        private List<WeekDistribution.TaskSlot> GetConnectedSlots(WeekDistribution.TaskSlot slot)
        {
            var connectedSlots = new List<WeekDistribution.TaskSlot> { slot };

            // Traverse previous and next connected slots to gather all linked slots
            var current = slot.PreviousConnected;
            while (current != null)
            {
                connectedSlots.Add(current);
                current = current.PreviousConnected;
            }

            current = slot.NextConnected;
            while (current != null)
            {
                connectedSlots.Add(current);
                current = current.NextConnected;
            }

            return connectedSlots;
        }


        public void PrintWeekDistribution()
        {
            Console.WriteLine("Week Distribution:");
            for (int dayIndex = 0; dayIndex < DaysInWeek; dayIndex++)
            {
                Console.WriteLine($"Day {dayIndex + 1}:");

                foreach (var slot in _weekDistribution.Week[dayIndex].Tasks)
                {
                    string taskInfo = slot.Task == null
                        ? $"#{slot.TaskTypeName}: [empty]"
                        : $"#{slot.TaskTypeName}: {slot.Task.TaskID}";
                    Console.WriteLine($"    {taskInfo}");
                }
            }

            Console.WriteLine();
        }

        // Fill with not connected slots to the maximum state avoiding WeeklyAmountDays restriction
        private WeekDistribution FillToMaximum()
        {
            var weekDistribution = new WeekDistribution(DaysInWeek, MaxTasksPerDay);

            foreach (var taskTypeDescriptor in _taskTypes)
            {
                var taskTypeName = taskTypeDescriptor.Key;
                var taskDailyAmountMax = taskTypeDescriptor.Value.DailyAmount.to;

                for (int d = 0; d < DaysInWeek; ++d)
                {
                    for (int t = 0; t < taskDailyAmountMax; ++t)
                    {
                        var taskSlot = new WeekDistribution.TaskSlot(taskTypeName);
                        weekDistribution.Week[d].Tasks.Add(taskSlot);
                    }
                }
            }

            if (_settings["log-level"] == "verbose")
            {
                Console.WriteLine("Maximized:");
                Console.WriteLine(weekDistribution.ToString());
            }

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
                DistributeTasksImpl(currentTasks, currentTasks == focusTasks);
            }

            // DistributeForEmptySlots(_taskList.Where(task =>
            //     (string.IsNullOrEmpty(task.Prefs) || task.Prefs.Length < 2) && task.Days.to == 1).ToList());
        }


        // - Get set of available task types in availableTasks -> A
        // - Get set of available task types in week which are not taken -> B
        // - C is intersection
        // - while availableTasks is not empty and slots are still available
        //    -try to spawn task (removing them from available tasks in any case)
        private void DistributeTasksImpl(List<Tasks.Task> availableTasks, bool focused)
        {
            // Loop until all tasks are distributed or there are no available slots
            while (availableTasks.Count > 0)
            {
                // Step 1: Determine sets A, B, and C
                var availableTypesInTasks = new HashSet<string>(availableTasks.Select(t => t.TaskType)); // Set A
                var availableTypesInWeek = GetAvailableTypesToPlace(_weekDistribution); // Set B
                var spawnableTypes = availableTypesInTasks.Intersect(availableTypesInWeek).ToList(); // C = A ∩ B


                // Step 2: Prioritize spawnable task types by importance
                var prioritizedTypes = spawnableTypes
                    .OrderByDescending(type => _taskTypes[type].Importance)
                    .ToList();
                bool taskPlaced = false;

                // Step 3: Try to place tasks from the prioritized types
                foreach (var taskType in prioritizedTypes)
                {
                    // Filter tasks of the current type
                    var tasksOfType = availableTasks.Where(t => t.TaskType == taskType).ToList();

                    if (tasksOfType.Count > 0)
                    {
                        // Select a task with a weighted random approach based on priority
                        var taskToPlace =
                            RandomHelper.Rnd.SpawnEvent(tasksOfType, task => (float)task.PickUpPriority, out _);

                        // Remove the selected task from available tasks
                        availableTasks.Remove(taskToPlace);

                        // Attempt to place the task in the distribution
                        if (TryToPlace(_weekDistribution, taskToPlace, focused))
                        {
                            taskPlaced = true;
                            break;
                        }
                    }
                }

                // If no tasks were placed in this iteration, exit to avoid an infinite loop
                if (!taskPlaced)
                    return;
                // Safety check to prevent excessive iterations
                if (_safetyIterationCounter++ > 1000)
                {
                    Console.WriteLine("Safety iteration limit reached. Exiting task distribution.");
                    return;
                }
            }
        }
        
        private WeekDistribution.TaskSlot GetEmptySlot(WeekDistribution weekDistribution, int dayIndex,
            string taskTypeName)
        {
            var slot = weekDistribution.Week[dayIndex].Tasks.FirstOrDefault(
                t => t.Task == null && t.TaskTypeName == taskTypeName);
            if (slot == null)
                return null;
            return slot;
        }

        private bool TryToPlace(WeekDistribution weekDistribution, Tasks.Task taskToPlace, bool focused)
        {
            bool weekEndPreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_END);
            bool weekStartPreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_START);
            bool weekMiddlePreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_MIDDLE);
            bool specificDaysPreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_DAYS);
            var days = RandomHelper.Rnd.FromRangeInt(taskToPlace.Days);

            List<WeekDistribution.TaskSlot> emptySlots = new List<WeekDistribution.TaskSlot>();

            if (weekStartPreferable)
            {
                // Start from Monday (index 0) and go to the end of the week
                for (int i = 0; i < days; i++)
                {
                    var slot = GetEmptySlot(_weekDistribution, i, taskToPlace.TaskType);

                    if (slot == null)
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
                    var slot = GetEmptySlot(_weekDistribution, i, taskToPlace.TaskType);
                    if (slot == null)
                        break;
                    emptySlots.Add(slot);
                }
            }
            else if (weekEndPreferable)
            {
                // Start from Sunday (index 6) and move to the start of the week
                for (int i = 0; i < days; i++)
                {
                    var slot = GetEmptySlot(_weekDistribution, 6 - i, taskToPlace.TaskType);
                    if (slot == null)
                        break;
                    emptySlots.Add(slot);
                }

                emptySlots.Reverse();
            }
            else if (specificDaysPreferable)
            {
                //throw new NotImplementedException();
            }

            // Can't event place a minimum amount on preferred position ?
            var usePreferencedDistribution = true;
            if (emptySlots.Count < taskToPlace.Days.from)
            {
                // get all empty slots from every day
                List<WeekDistribution.TaskSlot> allDays = new List<WeekDistribution.TaskSlot>();

                for (int i = 0; i < days; i++)
                {
                    var slot = GetEmptySlot(_weekDistribution, i, taskToPlace.TaskType);
                    if (slot != null)
                        allDays.Add(slot);
                }

                if (allDays.Count > emptySlots.Count)
                {
                    usePreferencedDistribution = false;
                    emptySlots = allDays;
                }
            }

            // Place task to empty slots and link them together
            WeekDistribution.TaskSlot prev = null;
            foreach (var slot in emptySlots)
            {
                Debug.Assert(taskToPlace.TaskType == slot.TaskTypeName);
                slot.Task = taskToPlace;
                slot.PlacedWithPreference = usePreferencedDistribution;
                slot.PlacedFocused = focused;

                // Linking
                slot.PreviousConnected = prev;
                if (prev != null)
                    prev.NextConnected = slot;

                prev = slot;
            }

            if (_settings["log-level"] == "verbose")
                Console.WriteLine(
                    $"Trying to place {taskToPlace.TaskType}:{taskToPlace.TaskID}. Placed: {emptySlots.Count}, usePreferencedDistribution: {usePreferencedDistribution}");

            return emptySlots.Count > 0;
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
    }
}