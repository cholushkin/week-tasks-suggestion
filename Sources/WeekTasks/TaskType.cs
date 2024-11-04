using System;
using System.Collections.Generic;

namespace WeekTasks
{
    public class TaskType
    {
        public string Description { get; set; }
        public int WeeklyAmountDays { get; set; }
        public int DailyAmount { get; set; }
        public int PickUpPriority { get; set; }
        public int SortingIndex { get; set; }
        public string Color { get; set; }

        // Constructor to initialize all properties
        public TaskType(string description, int weeklyAmountDays, int dailyAmount, int pickUpPriority, int sortingIndex, string color)
        {
            Description = description;
            WeeklyAmountDays = weeklyAmountDays;
            DailyAmount = dailyAmount;
            PickUpPriority = pickUpPriority;
            SortingIndex = sortingIndex;
            Color = color;
        }

        // Method to create a TaskType instance from a dictionary
        public static TaskType FromDictionary(Dictionary<string, object> data)
        {
            return new TaskType(
                description: data["description"].ToString(),
                weeklyAmountDays: Convert.ToInt32(data["weekly-amount-days"]),
                dailyAmount: Convert.ToInt32(data["daily-amount"]),
                pickUpPriority: Convert.ToInt32(data["pick-up-priority"]),
                sortingIndex: Convert.ToInt32(data["sorting-index"]),
                color: data["color"].ToString()
            );
        }
    }
}