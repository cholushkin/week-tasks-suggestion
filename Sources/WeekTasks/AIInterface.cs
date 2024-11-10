
using System.Net.Http.Json;
using System.Text.Json;

namespace WeekTasks
{
    interface IArtificialIntelligence
    {
        Task<string> GetResponseFromAIAsync(string prompt);
    }


    public class LocalAI : IArtificialIntelligence
    {
        private readonly HttpClient _httpClient;
        private static readonly string apiBaseUrl = "http://127.0.0.1:5000/v1/chat/completions";

        public LocalAI()
        {
            _httpClient = new HttpClient();
        }
        
        public async Task<string> GetResponseFromAIAsync(string prompt)
        {
            prompt = $"{prompt}. Make short response in a continuous and short block of text";
            
            // Define the request body for ChatCompletion API
            var request = new
            {
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 150 // Adjust as needed
            };

            int maxRetries = 5;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync(apiBaseUrl, request);
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
    
    public class OpenAIHelper : IArtificialIntelligence
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenAIHelper(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<string> GetResponseFromAIAsync(string prompt)
        {
            // Request format for GPT-4 model (or other available models on the paid plan)
            var request = new
            {
                model = "gpt-4o-mini", // Use GPT-4 on the paid plan, or "gpt-3.5-turbo" for high-volume requests
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                max_tokens = 200, // Adjust max tokens based on response length needs
                temperature = 0.7 // Optional: Controls creativity (0.7 is moderately creative)
            };

            var requestUri = "https://api.openai.com/v1/chat/completions";

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await _httpClient.PostAsJsonAsync(requestUri, request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                    return jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API request failed with status code {response.StatusCode}: {response.ReasonPhrase}. Response: {errorResponse}");
                }
            }
            catch (Exception ex)
            {
                // Log the exception message with detailed error info
                throw new Exception("Error occurred while calling OpenAI API. Details: " + ex.Message, ex);
            }
        }

    }
}