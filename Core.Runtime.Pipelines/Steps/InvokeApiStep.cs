using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Core.Runtime.Pipelines
{
    [Activity("Core.Runtime.Pipelines", "InvokeApiStep",
        Category = "rDex.steps",
        Description = "Send an HTTP request to an API endpoint.",
        DisplayName = "rPipe.InvokeApi",
        Kind = ActivityKind.Task,
        Version = 1)]
    [FlowNode("Pass", "Fail")]
    public class InvokeApiStep : Activity, IActivityWithResult
    {
        [Input(
          UIHint = InputUIHints.SingleLine,
          Name = "Url",
          DisplayName = "URL",
          Description = "The URL to send the request to.",
          CanContainSecrets = false,
          Category = "Props",
          DefaultSyntax = "CSharp",
          SupportedSyntaxes = new[] { "Literal", "CSharp" },
          Order = 1)]
        public Input<string> Url { get; set; } = default!;

        [Input(
            UIHint = InputUIHints.DropDown,
            Name = "Method",
            DisplayName = "Method",
            Description = "The HTTP method to use when sending the request.",
            CanContainSecrets = false,
            Category = "Props",
            Options = new[]
            {
                "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD"
            },
            DefaultValue = "GET",
            DefaultSyntax = "Literal",
            SupportedSyntaxes = new[] { "Literal", "CSharp" },
            Order = 2
        )]
        public Input<string> Method { get; set; } = default!;

        [Input(
            UIHint = InputUIHints.MultiLine,
            Name = "Headers",
            DisplayName = "Headers",
            Description = "The headers to send with the request. Format as an object with key-value pairs.",
            Category = "Props",
            DefaultSyntax = "CSharp",
            SupportedSyntaxes = new[] { "CSharp", "Literal", "JavaScript", "Liquid" },
            Order = 3
        )]
        public Input<object> Headers { get; set; } = default!;

        [Input(
            UIHint = InputUIHints.MultiLine,
            Name = "Body",
            DisplayName = "Body",
            Description = "The content to send with the request. Can be a string, an object, or a JObject.",
            Category = "Props",
            DefaultSyntax = "CSharp",
            SupportedSyntaxes = new[] { "CSharp", "Literal", "JavaScript", "Liquid" },
            Order = 4
        )]
        public Input<object> Body { get; set; } = default!;

        [Input(
            UIHint = InputUIHints.Checkbox,
            Name = "AuthenticationRequired",
            DisplayName = "Authentication Required",
            Description = "Whether authentication is required for this API call.",
            Category = "Authentication",
            DefaultValue = false,
            DefaultSyntax = "Literal",
            SupportedSyntaxes = new[] { "Literal", "CSharp" },
            Order = 5
        )]
        public Input<bool> AuthenticationRequired { get; set; } = default!;

        [Input(
            UIHint = InputUIHints.SingleLine,
            Name = "AuthToken",
            DisplayName = "Authentication Token",
            Description = "The authentication token to use (if auth is required).",
            Category = "Authentication",
            CanContainSecrets = true,
            DefaultSyntax = "CSharp",
            SupportedSyntaxes = new[] { "CSharp", "Literal" },
            Order = 6
        )]
        public Input<string> AuthToken { get; set; } = default!;

        [Input(
            UIHint = InputUIHints.Checkbox,
            Name = "ThrowOnError",
            DisplayName = "Throw On Error",
            Description = "Whether to throw an exception when the response indicates an error (4xx or 5xx status code).",
            Category = "Error Handling",
            DefaultValue = true,
            DefaultSyntax = "Literal",
            SupportedSyntaxes = new[] { "Literal", "CSharp" },
            Order = 7
        )]
        public Input<bool> ThrowOnError { get; set; } = default!;

        [Input(
            UIHint = InputUIHints.Checkbox,
            Name = "ParseJsonResponse",
            DisplayName = "Parse JSON Response",
            Description = "Whether to parse the response as JSON. If false, the raw string will be returned.",
            Category = "Response",
            DefaultValue = true,
            DefaultSyntax = "Literal",
            SupportedSyntaxes = new[] { "Literal", "CSharp" },
            Order = 8
        )]
        public Input<bool> ParseJsonResponse { get; set; } = default!;

        [Output(
           DisplayName = "Result",
           Name = "Result",
           IsBrowsable = true,
           Description = "The API response (as JObject if JSON, or string otherwise).",
           IsSerializable = true)]
        public Output? Result { get; set; }

        [Output(
           DisplayName = "StatusCode",
           Name = "StatusCode",
           IsBrowsable = true,
           Description = "The HTTP status code of the response.",
           IsSerializable = true)]
        public Output<int>? StatusCode { get; set; }

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var logger = context.GetRequiredService<ILogger<InvokeApiStep>>();

            try
            {
                // Get the inputs
                var url = context.Get(Url);
                var method = context.Get(Method);
                var headers = context.Get(Headers);
                var body = context.Get(Body);
                var authRequired = context.Get(AuthenticationRequired);
                var authToken = authRequired ? context.Get(AuthToken) : null;
                var throwOnError = context.Get(ThrowOnError);
                var parseJsonResponse = context.Get(ParseJsonResponse);

                logger.LogInformation("@@@@ Executing API request to {Url} with method {Method}", url, method);

                // Create HTTP client and configure request
                var client = context.GetRequiredService<HttpClient>();
                var request = new HttpRequestMessage
                {
                    Method = new HttpMethod(method),
                    RequestUri = new Uri(url)
                };

                // Add headers
                if (headers != null)
                {
                    AddHeaders(request, headers);
                }

                // Add authentication if required
                if (authRequired && !string.IsNullOrEmpty(authToken))
                {
                    request.Headers.Add("Authorization", $"Bearer {authToken}");
                }

                // Add content if present
                if (body != null)
                {
                    string bodyContent;

                    if (body is string stringBody)
                    {
                        bodyContent = stringBody;
                    }
                    else
                    {
                        // Convert object to JSON
                        bodyContent = Newtonsoft.Json.JsonConvert.SerializeObject(body);
                    }

                    logger.LogDebug("@@@@ Request body: {Body}", bodyContent);
                    request.Content = new StringContent(bodyContent, Encoding.UTF8, "application/json");
                }

                // Send the request
                var response = await client.SendAsync(request);

                // Get the status code
                int statusCode = (int)response.StatusCode;
                context.Set(StatusCode!, statusCode);

                logger.LogInformation("@@@@ API response received with status code {StatusCode}", statusCode);

                // Check for errors
                if (throwOnError && !response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogError("@@@@ API request failed with status code {StatusCode}. Response: {Response}", statusCode, errorContent);
                    throw new HttpRequestException($"API request failed with status code {statusCode}. Response: {errorContent}");
                }

                // Process response
                var responseContent = await response.Content.ReadAsStringAsync();

                if (parseJsonResponse && !string.IsNullOrEmpty(responseContent))
                {
                    try
                    {
                        var jsonResponse = JObject.Parse(responseContent);
                        logger.LogDebug("@@@@ Parsed JSON response");
                        context.SetResult(jsonResponse);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "@@@@ Failed to parse response as JSON, returning raw string");
                        context.SetResult(responseContent);
                    }
                }
                else
                {
                    logger.LogDebug("@@@@ Returning raw string response");
                    context.SetResult(responseContent);
                }

                await context.CompleteActivityWithOutcomesAsync("Pass");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "@@@@ Error executing API request in {StepName}", context.Activity.Name);
                await context.CompleteActivityWithOutcomesAsync("Fail");
            }
        }

        private void AddHeaders(HttpRequestMessage request, object headers)
        {
            // Handle different types of header inputs
            if (headers is IDictionary<string, object> dictHeaders)
            {
                foreach (var header in dictHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value?.ToString());
                }
            }
            else if (headers is JObject jObjectHeaders)
            {
                foreach (var property in jObjectHeaders.Properties())
                {
                    request.Headers.TryAddWithoutValidation(property.Name, property.Value.ToString());
                }
            }
            else if (headers is dynamic)
            {
                // Try to iterate through properties of dynamic object
                try
                {
                    IDictionary<string, object> dynamicDictionary = headers as IDictionary<string, object>;
                    if (dynamicDictionary != null)
                    {
                        foreach (var header in dynamicDictionary)
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value?.ToString());
                        }
                    }
                }
                catch
                {
                    // If dynamic approach fails, just add Content-Type as fallback
                    request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                }
            }
            else
            {
                // Default Content-Type if headers is not in expected format
                request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            }
        }
    }
}
