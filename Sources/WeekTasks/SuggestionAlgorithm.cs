using System.Diagnostics;
using WeekTasks.Utils.Random;

namespace WeekTasks
{
    public class SuggestionAlgorithm
    {
        private enum TaskPlacementResult
        {
            Success,
        }
        private const int DaysInWeek = 7;
        private const int MaxTasksPerDay = 10;
        private List<Tasks.Task> _availableTasks;
        private List<Tasks.Task>[] _weekDistribution;
        private List<Tasks.Task>[] _weekBufferDistribution;
        
        private Dictionary<string, int> _taskTypesPlacedCount; // Track placed task counts by type

        private int _safeCounter;
        
        
        public void Generate(Dictionary<string, string> settings, Dictionary<string, TaskType> taskTypes, List<Tasks.Task> taskList)
        {
            Debug.Assert(DaysInWeek == 7);
            _availableTasks = new List<Tasks.Task>(taskList);
            _weekDistribution = new List<Tasks.Task>[DaysInWeek];
            for (int i = 0; i < _weekDistribution.Length; i++)
                _weekDistribution[i] = [];
            _safeCounter = 0;
            
            while (!IsCompleted())
            {
                var focusTasks = taskList.Where(task => task.HasPreference(Tasks.Task.Pref.FOCUS)).ToArray();
                if (focusTasks.Length > 0)
                {
                    var taskToPlace = RandomHelper.Rnd.SpawnEvent(focusTasks, task => (float)task.PickUpPriority, out _);
                    _weekBufferDistribution = CloneWeekDistribution(_weekDistribution);
                    var days = RandomHelper.Rnd.FromRangeInt(taskToPlace.Days);
                    
                    Debug.Assert(days >= 0 && days <= DaysInWeek);
                    
                    bool weekEndPreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_END);
                    bool weekStartPreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_START);
                    bool weekMiddlePreferable = taskToPlace.HasPreference(Tasks.Task.Pref.WEEK_MIDDLE);

                    int placedCounter = 0;
                    
                    if (weekStartPreferable)
                    {
                        // Start from Monday (index 0) and go to the end of the week
                        for (int i = 0; i < days; i++)
                        {
                            if (TryToPlace(taskToPlace, i)) // Distribute from Monday to Sunday
                                placedCounter++;
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
                            if( TryToPlace(taskToPlace, dayToPlace) )
                                placedCounter++;
                        }
                    }
                    else if (weekEndPreferable)
                    {
                        // Start from Sunday (index 6) and move to the start of the week
                        for (int i = 0; i < days; i++)
                        {
                            if(TryToPlace(taskToPlace, 6 - i)) // Distribute from Sunday to Monday
                                placedCounter++;
                        }
                    }

                    // Can't event place a minimum amount on preferred position ?
                    if (placedCounter < taskToPlace.Days.from)
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
                    else
                    {
                        Place(_weekBufferDistribution, placedCounter, taskToPlace);
                    }
                }
                else
                {
                    
                }
                _safeCounter++;
            }
            
            Console.WriteLine($"Seed: {RandomHelper.Rnd.GetState().AsNumber()}");
        }

        private void Place(List<Tasks.Task>[] weekBufferDistribution, int placedCounter, Tasks.Task focusedTask)
        {
            _weekDistribution = CloneWeekDistribution(weekBufferDistribution);
            UpdateTaskTypesCount(focusedTask.TaskType, placedCounter);
            _availableTasks.Remove(focusedTask);
        }

        private bool TryToPlace(Tasks.Task task, int dayOfWeekIndex)
        {
            var day = _weekBufferDistribution[dayOfWeekIndex];
            if (day.Count >= MaxTasksPerDay)
                return false;
            day.Add(task);
            return true;
        }
        
        
        private void UpdateTaskTypesCount(string taskType, int placedCount)
        {
            if (!_taskTypesPlacedCount.TryAdd(taskType, placedCount))
                _taskTypesPlacedCount[taskType] += placedCount;
        }
        
        private List<Tasks.Task>[] CloneWeekDistribution(List<Tasks.Task>[] weekDistribution)
        {
            var clonedDistribution = new List<Tasks.Task>[weekDistribution.Length];
            for (int i = 0; i < weekDistribution.Length; i++)
            {
                // Create a new list for each day and add a copy of the tasks
                clonedDistribution[i] = new List<Tasks.Task>(weekDistribution[i]);
            }
            return clonedDistribution;
        }

        private bool IsCompleted()
        {
            // No tasks to distribute
            if (_availableTasks.Count < 1)
            {
                Console.WriteLine("Reason to finish: No tasks to distribute");
                return true;
            }

            if (_safeCounter > 1000)
            {
                Console.WriteLine("Reason to finish: Safe counter");
                return true;
            }

            return false;
        }
    }
}