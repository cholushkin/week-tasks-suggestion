using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WeekTasks;

public class TestChatGpt
{
    private static readonly string apiKey = "";
    private static readonly HttpClient httpClient = new HttpClient();
    
 
    public static async Task Run()
    {
        Console.Write("Enter your prompt: ");
        string prompt = Console.ReadLine();
        string response = await GetChatGPTResponseAsync(prompt);
        Console.WriteLine("ChatGPT Response:");
        Console.WriteLine(response);
    }

    public static async Task<string> GetChatGPTResponseAsync(string prompt)
    {
        var request = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 20
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        int maxRetries = 5;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                    return jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                }
                else if ((int)response.StatusCode == 429) // Too Many Requests
                {
                    // Check if Retry-After header is present
                    if (response.Headers.TryGetValues("Retry-After", out var values))
                    {
                        var retryAfterSeconds = int.Parse(values.First());
                        Console.WriteLine($"Rate limit hit, retrying after {retryAfterSeconds} seconds...");
                        await Task.Delay(retryAfterSeconds * 1000);
                    }
                    else
                    {
                        Console.WriteLine("Rate limit hit, retrying after default 1-second delay...");
                        await Task.Delay(1000); // Default retry delay if header is missing
                    }
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
