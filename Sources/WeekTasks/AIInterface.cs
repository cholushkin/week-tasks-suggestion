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
        private bool _connectionProblem = false;
        private string _lastErrorMessage;

        public LocalAI()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3) // todo: consider passing timeout from constructor
            };
        }

        public async Task<string> GetResponseFromAIAsync(string prompt)
        {
            prompt = $"{prompt}. Make a short response in a continuous and short block of text";

            if (_connectionProblem)
            {
                Console.WriteLine($"Connection problem detected. Last error: {_lastErrorMessage}");
                return "[[howto-setup-ai]]";
            }

            // Prepare the request payload
            var request = new
            {
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 150
            };

            try
            {
                // Send the request to the local AI API
                var response = await _httpClient.PostAsJsonAsync(WeekTasksProgram.Settings["ai-api-url"], request);

                // Handle non-success HTTP status codes
                if (!response.IsSuccessStatusCode)
                {
                    _connectionProblem = true;
                    _lastErrorMessage = $"API Error: {response.StatusCode} - {response.ReasonPhrase}";
                    Console.WriteLine(_lastErrorMessage);
                    return "[[howto-setup-ai]]";
                }

                // Attempt to read and parse the response
                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (jsonResponse.TryGetProperty("choices", out JsonElement choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out JsonElement message) &&
                    message.TryGetProperty("content", out JsonElement content))
                {
                    return content.GetString();
                }
                else
                {
                    _connectionProblem = true;
                    _lastErrorMessage = "Error: Unexpected response format received from the AI API.";
                    Console.WriteLine(_lastErrorMessage);
                    return "[[howto-setup-ai]]";
                }
            }
            catch (HttpRequestException ex)
            {
                _connectionProblem = true;
                _lastErrorMessage = $"HTTP Error: {ex.Message}";
                Console.WriteLine($"HTTP request failed: {ex.Message}");
                return "[[howto-setup-ai]]";
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _connectionProblem = true;
                _lastErrorMessage = "Error: Request timed out.";
                Console.WriteLine("Request timed out.");
                return "[[howto-setup-ai]]";
            }
            catch (JsonException ex)
            {
                _connectionProblem = true;
                _lastErrorMessage = $"Error: Failed to parse response JSON. {ex.Message}";
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                return "[[howto-setup-ai]]";
            }
            catch (Exception ex)
            {
                _connectionProblem = true;
                _lastErrorMessage = $"Unexpected error: {ex.Message}";
                Console.WriteLine($"Unexpected error: {ex}");
                return "[[howto-setup-ai]]";
            }
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
                    return jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content")
                        .GetString();
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    throw new Exception(
                        $"API request failed with status code {response.StatusCode}: {response.ReasonPhrase}. Response: {errorResponse}");
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