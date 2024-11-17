using System.Text;
using WeekTasks.Utils.Random;

namespace WeekTasks
{
    public class MarkdownFileWriter
    {
        private readonly string _fileName;
        private readonly string _obsidianDirectory;
        private readonly MessageOfTheDay _messageOfTheDay;
        private readonly DateTime _startDate; // Store the startDate

        public MarkdownFileWriter(DateTime startDate, string seed, string obsidianDirectory,
            MessageOfTheDay messageOfTheDay)
        {
            _startDate = startDate;
            // File name in the format: yyyy-MM-dd.md
            _fileName = $"{startDate:yyyy-MM-dd} Week Plan ({seed}).md";
            _obsidianDirectory = obsidianDirectory;
            _messageOfTheDay = messageOfTheDay;
        }

        public void Write(WeekDistribution weekDistribution)
        {
            var filePath = Path.Combine(_obsidianDirectory, $"{_fileName}");
            var markdownBuilder = new StringBuilder();

            markdownBuilder.AppendLine("");

            // Loop through each day in the week distribution
            for (int dayIndex = 0; dayIndex < weekDistribution.Week.Count; dayIndex++)
            {
                // Calculate the current day’s date based on the startDate
                var currentDate = _startDate.AddDays(dayIndex);
                var formattedDate = currentDate.ToString("dddd dd-MMM-yyyy"); // Format: Monday 08-Nov-2024

                // Add a "Message of the Day" if available
                var message = RandomHelper.Rnd.FromList(_messageOfTheDay.MessageOfTheDayStrings);

                // Append the day header with the formatted date
                markdownBuilder.AppendLine("---");
                markdownBuilder.AppendLine($"### Day {dayIndex + 1}. {formattedDate}");
                markdownBuilder.AppendLine();
                markdownBuilder.AppendLine("> [!Tip of the day]");
                markdownBuilder.AppendLine($"> {message}");
                markdownBuilder.AppendLine();

                // Append each task for the day
                foreach (var slot in weekDistribution.Week[dayIndex].Tasks)
                {
                    if (slot.Task == null)
                    {
                        markdownBuilder.AppendLine($"- [ ] **{slot.TaskTypeName}**: [empty]");
                    }
                    else
                    {
                        // Format each task with the specified details
                        markdownBuilder.Append(
                            $"- [ ] **{slot.Task.TaskType}** ({slot.Task.TaskID}): {slot.Task.Description}");
                        if (!string.IsNullOrEmpty(slot.Task.Remarks))
                        {
                            markdownBuilder.Append($". {slot.Task.Remarks}");
                        }

                        if (!string.IsNullOrEmpty(slot.Task.PromptResult))
                        {
                            if (slot.Task.Prompt.StartsWith("google"))
                            {
                                markdownBuilder.Append($". {slot.Task.PromptResult}");
                            }

                            if (slot.Task.Prompt.StartsWith("ai"))
                            {
                                var promptResultLines =
                                    slot.Task.PromptResult.Split(new[] { "\r\n", "\r", "\n", "\n\n" },
                                        StringSplitOptions.None);
                                markdownBuilder.AppendLine($"\n> [!TIP]- {slot.Task.Prompt}");
                                for (int i = 0; i < promptResultLines.Length; i++)
                                {
                                    if (i == promptResultLines.Length - 1) // Last line
                                        markdownBuilder.Append($"> {promptResultLines[i]}");
                                    else // Any other line
                                        markdownBuilder.Append($"> {promptResultLines[i]}\n"); // Append with a newline
                                }
                            }
                        }

                        markdownBuilder.AppendLine();
                    }
                }

                markdownBuilder.AppendLine();
            }

            markdownBuilder.AppendLine("---");
            markdownBuilder.AppendLine("## Week review");
            markdownBuilder.AppendLine("- I followed my plan throughout the week");
            markdownBuilder.AppendLine("- AI summarize");
            markdownBuilder.AppendLine();
            markdownBuilder.AppendLine("#week-organizer #week-summary");


            // Write the Markdown content to the file
            File.WriteAllText(filePath, markdownBuilder.ToString());
            Console.WriteLine($"Markdown file written to: {filePath}");
        }
    }
}