using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace ChatClient;

#pragma warning disable 

public class ChatBotInference
{
    public string ChatURL = "https://code-server:5050/chat/";

    public Prompts.Language LanguageCode = Prompts.Language.EN;
    public Prompts.Detail DetailLevel = Prompts.Detail.Concise;

    public bool Verbose = false;

    public string ModelName { get; set; } = string.Empty;
    public string ClaudeApiKey { get; set; } = string.Empty;
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string GeminiApiKey { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.7f;
    public event EventHandler<(string, string)>? ResultEvent;
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }


    public class ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = new();

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        public class Choice
        {
            [JsonPropertyName("message")]
            public Message Message { get; set; } = new();

            [JsonPropertyName("finish_reason")]
            public string FinishReason { get; set; } = string.Empty;
        }
    }

    public async Task<bool> AnalyzeCode(string code_prompt, string logText = "", string issue_prompt = "", int issue_count = 0)
    {
        if (!string.IsNullOrEmpty(OpenAIApiKey))
        {
            return await ExamineCodeUsingOpenAIServer(code_prompt, logText, issue_prompt, issue_count);
        }
        if (!string.IsNullOrEmpty(GeminiApiKey))
        {
            return await ExamineCodeUsingGoogleGemini(code_prompt, logText, issue_prompt, issue_count);
        }
        if (!string.IsNullOrEmpty(ClaudeApiKey))
        {
            return await ExamineCodeUsingClaude(code_prompt, logText, issue_prompt, issue_count);
        }
        return await ExamineCodeUsingJuraCodeServer(code_prompt, logText, issue_prompt, issue_count);
    }


    public async Task<bool> ExamineCodeUsingJuraCodeServer(string code_prompt, string logText = "", string issue_prompt = "", int issue_count = 0)
    {
        Console.WriteLine($"sending code with {issue_count} issues...");
        var user_prompt = "";
        var system_prompt = "";
        if (issue_count > 0) 
        {
            Console.WriteLine("analysis w/issues: sending code with issues to " + ModelName + "...");
            system_prompt = Prompts.CodeWithIssuesSystem(LanguageCode, DetailLevel);
            user_prompt = Prompts.CodeWithIssuesUser(LanguageCode, DetailLevel).Replace("{code_prompt}", code_prompt).Replace("{issue_prompt}", issue_prompt);
            user_prompt = "Step 1 -- " + user_prompt;
            user_prompt += "\n\nStep 2 -- ";
            if (issue_count == 1)
                user_prompt += Prompts.CodeWithIssuesIssue(LanguageCode, DetailLevel).Replace("{issue_prompt}", issue_prompt).Replace("{code_prompt}", code_prompt);
            else
                user_prompt += Prompts.CodeWithIssuesMultipleIssues(LanguageCode, DetailLevel).Replace("{issue_prompt}", issue_prompt).Replace("{code_prompt}", code_prompt);
        }
        else
        {
            Console.WriteLine("code-only analysis: sending code to " + ModelName + "...");
            system_prompt = Prompts.CodeOnlySystem(LanguageCode, DetailLevel);
            user_prompt = Prompts.CodeOnlyUser(LanguageCode, DetailLevel).Replace("{code_prompt}", code_prompt);
        }

        try
        {
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("system", Prompts.CodeWithIssuesSystem(LanguageCode, DetailLevel)),
                new KeyValuePair<string, string>("user", user_prompt),
                new KeyValuePair<string, string>("model", ModelName),
                new KeyValuePair<string, string>("temperature", Temperature.ToString())
            });
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(15)
            };

            var response = await client.PostAsync(ChatURL, form);

            // Handle the response.
            if (response.IsSuccessStatusCode)
            {
                var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
                if (chatResponse.Choices != null)
                {
                    if (chatResponse.Choices.Count == 0)
                    {
                        Console.WriteLine("Warning: No choices in the response from " + ModelName);
                    }
                    else
                    {
                        var message = chatResponse.Choices[0].Message.Content;
                        if (message != null) {
                            if (Verbose) Console.WriteLine(message);
                            if (ResultEvent != null)
                            {
                                ResultEvent(this, (message, code_prompt + "\n" + issue_prompt));
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"ChatBot Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChatBot Exception occurred: {ex.Message}");
        }
        return true;
    }

    public async Task<bool> ExamineCodeUsingOpenAIServer(string code_prompt, string logText = "", string issue_prompt = "", int issue_count = 0)
    {
        var user_prompt = "";
        var system_prompt = Prompts.CodeWithIssuesSystem(LanguageCode, DetailLevel);

        if (issue_count > 0)
        {
            Console.WriteLine("analysis w/issues: sending code with issues to " + ModelName + "...");
            user_prompt = Prompts.CodeWithIssuesUser(LanguageCode, DetailLevel).Replace("{code_prompt}", code_prompt).Replace("{issue_prompt}", issue_prompt);
            user_prompt = "Step 1 -- " + user_prompt;
            user_prompt += "\n\nStep 2 -- ";
            if (issue_count == 1)
                user_prompt += Prompts.CodeWithIssuesIssue(LanguageCode, DetailLevel).Replace("{issue_prompt}", issue_prompt).Replace("{code_prompt}", code_prompt);
            else
                user_prompt += Prompts.CodeWithIssuesMultipleIssues(LanguageCode, DetailLevel).Replace("{issue_prompt}", issue_prompt).Replace("{code_prompt}", code_prompt);
        }
        else
        {
            Console.WriteLine("code-only analysis: sending code to " + ModelName + "...");
            user_prompt = Prompts.CodeOnlyUser(LanguageCode, DetailLevel).Replace("{code_prompt}", code_prompt);
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAIApiKey}");

            var requestBody = new
            {
                model = ModelName,
                messages = new[]
                {
                new { role = "system", content = system_prompt },
                new { role = "user", content = user_prompt }
            },
                temperature = Temperature,
                max_tokens = 1500
            };

            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);

            // Handle the response.
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var chatResponse = System.Text.Json.JsonSerializer.Deserialize<ChatResponse>(responseContent);

                if (chatResponse?.Choices != null && chatResponse.Choices.Count > 0)
                {
                    var message = chatResponse.Choices[0].Message.Content;
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (Verbose) Console.WriteLine(message);
                        ResultEvent.Invoke(this, (message, code_prompt + "\n" + issue_prompt));
                    }
                }
                else
                {
                    Console.WriteLine("Warning: No choices in the response from " + ModelName);
                }
            }
            else
            {
                Console.WriteLine($"ChatBot Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChatBot Exception occurred: {ex.Message}");
        }
        return true;
    }

    public async Task<bool> ExamineCodeUsingGoogleGemini(string code_prompt, string logText = "", string issue_prompt = "", int issue_count = 0)
    {
        var user_prompt = "";
        var system_prompt = Prompts.CodeWithIssuesSystem(LanguageCode, DetailLevel);

        if (issue_count > 0)
        {
            Console.WriteLine("analysis w/issues: sending code with issues to Google Gemini...");
            user_prompt = Prompts.CodeWithIssuesUser(LanguageCode, DetailLevel).Replace("{code_prompt}", code_prompt).Replace("{issue_prompt}", issue_prompt);
            user_prompt = "Step 1 -- " + user_prompt;
            user_prompt += "\n\nStep 2 -- ";
            if (issue_count == 1)
                user_prompt += Prompts.CodeWithIssuesIssue(LanguageCode, DetailLevel).Replace("{issue_prompt}", issue_prompt).Replace("{code_prompt}", code_prompt);
            else
                user_prompt += Prompts.CodeWithIssuesMultipleIssues(LanguageCode, DetailLevel).Replace("{issue_prompt}", issue_prompt).Replace("{code_prompt}", code_prompt);
        }
        else
        {
            Console.WriteLine("code-only analysis: sending code to Google Gemini...");
            user_prompt = Prompts.CodeOnlyUser(LanguageCode, DetailLevel).Replace("{code_prompt}", code_prompt);
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GeminiApiKey}");

            var requestBody = new
            {
                model = ModelName,
                messages = new[]
                {
                new { role = "system", content = system_prompt },
                new { role = "user", content = user_prompt }
            },
                temperature = Temperature,
                max_tokens = 1500
            };

            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("https://api.google.com/v1/gemini/completions", jsonContent);

            // Handle the response.
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var chatResponse = System.Text.Json.JsonSerializer.Deserialize<ChatResponse>(responseContent);

                if (chatResponse?.Choices != null && chatResponse.Choices.Count > 0)
                {
                    var message = chatResponse.Choices[0].Message.Content;
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (Verbose) Console.WriteLine(message);
                        ResultEvent.Invoke(this, (message, code_prompt + "\n" + issue_prompt));
                    }
                }
                else
                {
                    Console.WriteLine("Warning: No choices in the response from Google Gemini");
                }
            }
            else
            {
                Console.WriteLine($"Google Gemini Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Google Gemini Exception occurred: {ex.Message}");
        }
        return true;
    }


    public async Task<bool> ExamineCodeUsingClaude(string code_prompt, string logText = "", string issue_prompt = "", int issue_count = 0)
    {
        var user_prompt = "";
        var system_prompt = Prompts.CodeWithIssuesSystem(LanguageCode, DetailLevel);

        if (issue_count > 0)
        {
            Console.WriteLine("analysis w/issues: sending code with issues to Claude...");
            user_prompt = Prompts.CodeWithIssuesUser(LanguageCode, DetailLevel).Replace("{code_prompt}", code_prompt).Replace("{issue_prompt}", issue_prompt);
            user_prompt = "Step 1 -- " + user_prompt;
            user_prompt += "\n\nStep 2 -- ";
            if (issue_count == 1)
                user_prompt += Prompts.CodeWithIssuesIssue(LanguageCode, DetailLevel).Replace("{issue_prompt}", issue_prompt).Replace("{code_prompt}", code_prompt);
            else
                user_prompt += Prompts.CodeWithIssuesMultipleIssues(LanguageCode, DetailLevel).Replace("{issue_prompt}", issue_prompt).Replace("{code_prompt}", code_prompt);
        }
        else
        {
            Console.WriteLine("code-only analysis: sending code to Claude...");
            user_prompt = Prompts.CodeOnlyUser(LanguageCode, DetailLevel).Replace("{code_prompt}", code_prompt);
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ClaudeApiKey}");

            var requestBody = new
            {
                model = ModelName,
                messages = new[]
                {
                new { role = "system", content = system_prompt },
                new { role = "user", content = user_prompt }
            },
                temperature = Temperature,
                max_tokens = 1500
            };

            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("https://api.anthropic.com/v1/claude/completions", jsonContent);

            // Handle the response.
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var chatResponse = System.Text.Json.JsonSerializer.Deserialize<ChatResponse>(responseContent);

                if (chatResponse?.Choices != null && chatResponse.Choices.Count > 0)
                {
                    var message = chatResponse.Choices[0].Message.Content;
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (Verbose) Console.WriteLine(message);
                        ResultEvent.Invoke(this, (message, code_prompt + "\n" + issue_prompt));
                    }
                }
                else
                {
                    Console.WriteLine("Warning: No choices in the response from Claude");
                }
            }
            else
            {
                Console.WriteLine($"Claude Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Claude Exception occurred: {ex.Message}");
        }
        return true;
    }

}
