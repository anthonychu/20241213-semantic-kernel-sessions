using System.Runtime.CompilerServices;
using System.Text;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core.CodeInterpreter;

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var modelId = "gpt-35-turbo";
var azureOpenAIEndpoint = configuration.GetValue<string>("AZURE_OPENAI_ENDPOINT");
var poolManagementEndpoint = configuration.GetValue<string>("POOL_MANAGEMENT_ENDPOINT");

// Logger for program scope
ILogger logger = NullLogger.Instance;

DefaultAzureCredential credential = new DefaultAzureCredential();

/// <summary>
/// Acquire a token for the Azure Container Apps service
/// </summary>
async Task<string> TokenProvider()
{
    string resource = "https://acasessions.io/.default";
    // Attempt to get the token
    var accessToken = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext([resource])).ConfigureAwait(false);
    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation("Access token obtained successfully");
    }
    return accessToken.Token;
}

var settings = new SessionsPythonSettings(
        sessionId: Guid.NewGuid().ToString(),
        endpoint: new Uri(poolManagementEndpoint));

Console.WriteLine("=== Code Interpreter With Azure Container Apps Plugin Demo ===\n");

Console.WriteLine("Start your conversation with the assistant. Type enter or an empty message to quit.");

var builder =
    Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(modelId, azureOpenAIEndpoint, new DefaultAzureCredential());

// Change the log level to Trace to see more detailed logs
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole().SetMinimumLevel(LogLevel.Warning)
        .AddFilter("Microsoft.SemanticKernel", LogLevel.Trace);
});
builder.Services.AddHttpClient();
builder.Services.AddSingleton((sp)
    => new SessionsPythonPlugin(
        settings,
        sp.GetRequiredService<IHttpClientFactory>(),
        TokenProvider,
        sp.GetRequiredService<ILoggerFactory>()));
var kernel = builder.Build();

logger = kernel.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
kernel.Plugins.AddFromObject(kernel.GetRequiredService<SessionsPythonPlugin>());
var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

var chatHistory = new ChatHistory();

StringBuilder fullAssistantContent = new();

while (true)
{
    Console.Write("\nUser: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) { break; }

    chatHistory.AddUserMessage(input);

    Console.WriteLine("Assistant: ");
    fullAssistantContent.Clear();
    await foreach (var content in chatCompletion.GetStreamingChatMessageContentsAsync(
        chatHistory,
        new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() },
        kernel)
        .ConfigureAwait(false))
    {
        Console.Write(content.Content);
        fullAssistantContent.Append(content.Content);
    }
    chatHistory.AddAssistantMessage(fullAssistantContent.ToString());
}