namespace WeekTasks
{
    public class PromptProcessor
    {
        private string _promptValue;
        private IArtificialIntelligence _aiBridge = new LocalAI();

        public async Task<string> Process(string value)
        {
            _promptValue = value;
            if (string.IsNullOrEmpty(_promptValue))
                return null;

            if (_promptValue.StartsWith("google:"))
            {
                // Cut "google:" from the beginning and trim
                string googlePrompt = _promptValue.Substring("google:".Length).Trim();
                return ProcessGoogle(googlePrompt);
            }
            else if (_promptValue.StartsWith("ai:"))
            {
                // Cut "ai:" from the beginning and trim
                string aiPrompt = _promptValue.Substring("ai:".Length).Trim();
                return await ProcessLocalAI(aiPrompt);
            }
            else
            {
                Console.WriteLine($"Warning: prompt should start from one of the supported commands: 'google' or 'ai' followed by a colon ':'. Data:{_promptValue}");
            }
            return null;
        }

        private string ProcessGoogle(string googlePrompt)
        {
            // Format the Google search URL
            string encodedPrompt = Uri.EscapeDataString(googlePrompt);
            string googleSearchLink = $"[google it](https://www.google.com/search?q={encodedPrompt})";
            return googleSearchLink;
        }


        private async Task<string> ProcessLocalAI(string aiPrompt)
        {
            return await _aiBridge.GetResponseFromAIAsync(aiPrompt);
        }
    }
}