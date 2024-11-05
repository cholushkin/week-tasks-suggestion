using System;
using System.Collections.Generic;
using System.IO;

namespace WeekTasks
{
    public class MessageOfTheDay
    {
        // List to store each line from the file as a separate message
        public List<string> MessageOfTheDayStrings { get; private set; }

        // Static method to load messages from a text file
        public static MessageOfTheDay LoadFromTextFile(string filePath)
        {
            var messageOfTheDay = new MessageOfTheDay();

            try
            {
                // Read all lines from the file and store them in the list
                messageOfTheDay.MessageOfTheDayStrings = new List<string>(File.ReadAllLines(filePath));
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Error: The file '{filePath}' does not exist.");
                messageOfTheDay.MessageOfTheDayStrings = new List<string>(); // Initialize empty list if file not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error loading messages: {ex.Message}");
                messageOfTheDay.MessageOfTheDayStrings = new List<string>(); // Initialize empty list on any error
            }

            return messageOfTheDay;
        }
    }
}