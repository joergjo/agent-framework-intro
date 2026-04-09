using System.ClientModel.Primitives;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();
var endpoint = configuration["Azure:OpenAI:Endpoint"];
var deployment = configuration["Azure:OpenAI:Deployment"];

if (deployment is not { Length: > 0 } || endpoint is not { Length: > 0 })
{
    Console.WriteLine("Please set the following environment variables:");
    Console.WriteLine("Azure:OpenAI:Deployment");
    Console.WriteLine("Azure:OpenAI:Endpoint");
    Environment.Exit(1);
}

var baseUri = new Uri(endpoint);

// Create the OpenAI SDK client thta can be used as a factory for all specific OpenAI clients (e.g. chat, embeddings, etc.). 
var openAIClient = new OpenAIClient(
    new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
    new OpenAIClientOptions { Endpoint = new Uri(baseUri, "/openai/v1/") });

// Create a client for the Responses API. 
var openAIChatClient = openAIClient.GetResponsesClient();

// Create the Microsoft.Extensions.AI abstraction IChatClient abstraction from the concrete OpenAI ResponsesClient,
// using the specified deployment as the default model for all calls. 
var chatClient = openAIChatClient.AsIChatClient(defaultModelId: deployment);

// Typically, you rather want to chain these calls for conciseness:
// var chatClient = new OpenAIClient(
//     new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
//     new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
//     .GetResponsesClient()
//     .AsIChatClient(defaultModelId: deployment);

const string instructions = 
    """
    You are a friendly coding assistant that helps users review and write Go code and recommend improvements to existing Go code. 
    Create idiomatic and easy to understand Go code, but do use newer language features like generics where it makes sense. 
    """;

const string prompt = 
    """
    Please create a simple "Hello World" web server.
    """;

List<ChatMessage> messages = 
[
    new ChatMessage(ChatRole.System, instructions),
    new ChatMessage(ChatRole.User, prompt),
];

// Get full response
var response = await chatClient.GetResponseAsync(messages);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(prompt);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine(response.Text);
Console.WriteLine();
Console.ReadKey(intercept: true);

const string anotherPrompt = 
    """
    Update the code so that it only accepts GET and HEAD requests on "/".
    """;

messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));
messages.Add(new ChatMessage(ChatRole.User, anotherPrompt));

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(anotherPrompt);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Blue;

// Stream response
await foreach(var update in chatClient.GetStreamingResponseAsync(messages))
{
    Console.Write(update.Text);
    // Dramatic pause to simulate streaming effect
    await Task.Delay(50);
}

Console.ResetColor();

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.