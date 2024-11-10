using System.Net.Http.Json;
using System.Text.Json;

namespace LocalAPIChat
{
    public static class TestLocalChatAPI
    {
        // Set this to the base URL of your local API
        private static readonly string apiBaseUrl = "http://127.0.0.1:5000/v1/chat/completions";
        private static readonly string apiKey = "your-api-key"; // Optional, only if your server requires it
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task Run()
        {
            Console.Write("Enter your prompt: ");
            string prompt = Console.ReadLine();
            string response = await GetChatResponseAsync(prompt);
            Console.WriteLine("Response:");
            Console.WriteLine(response);
        }

        public static async Task<string> GetChatResponseAsync(string prompt)
        {
            // Define the request body for ChatCompletion API
            var request = new
            {
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 50 // Adjust as needed
            };

            // Set authorization header if needed
            // if (!string.IsNullOrEmpty(apiKey))
            // {
            //     httpClient.DefaultRequestHeaders.Authorization = 
            //         new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            // }

            int maxRetries = 5;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var response = await httpClient.PostAsJsonAsync(apiBaseUrl, request);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                        return jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                    }
                    else if ((int)response.StatusCode == 429) // Too Many Requests
                    {
                        // Retry logic if needed
                        Console.WriteLine("Rate limit hit, retrying after delay...");
                        await Task.Delay(1000);
                    }
                    else
                    {
                        return $"Error: {response.StatusCode} - {response.ReasonPhrase}";
                    }
                }
                catch (Exception ex)
                {
                    return $"Exception: {ex.Message}";
                }
            }

            return "Failed to get a response after multiple attempts.";
        }
    }
}
