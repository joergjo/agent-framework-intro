using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

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

const string instructions = 
    """
    You are a friendly coding assistant that helps users review and write Go code and recommend improvements to existing Go code. 
    Create idiomatic and easy to understand Go code, but do use newer language features like generics where it makes sense. 
    """;

// NEW: Create an Agent Framework agent from the IChatClient with instructions, name and description
var agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetResponsesClient()
    .AsIChatClient(defaultModelId: deployment)
    .AsAIAgent(instructions: instructions, name: "Gopilot", description: "An AI agent that helps users write Go code.");

const string prompt = 
    """
    Please create a simple "Hello World" web server.
    """;

// NEW: Create a session instead of manually maintaining a list of messages
var session = await agent.CreateSessionAsync();

// Get full response
var response = await agent.RunAsync(prompt, session);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(prompt);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine(response.Text);
Console.WriteLine();
Console.ReadKey(intercept: true);

const string anotherPrompt = """Update the code so that it only accepts GET and HEAD requests on "/".""";

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(anotherPrompt);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Blue;

// Stream response
await foreach(var update in agent.RunStreamingAsync(anotherPrompt, session))
{
    Console.Write(update.Text);
    // Dramatic pause to simulate streaming effect
    await Task.Delay(50);
}

Console.ResetColor();

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.