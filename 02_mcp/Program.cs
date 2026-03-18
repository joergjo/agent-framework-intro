using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();
var endpoint = configuration["Azure:OpenAI:Endpoint"];
var deployment = configuration["Azure:OpenAI:Deployment"];
var apiKey = configuration["Context7:ApiKey"];

if (deployment is not { Length: > 0 } || endpoint is not { Length: > 0 } || apiKey is not { Length: > 0 })
{
    Console.WriteLine("Please set the following environment variables:");
    Console.WriteLine("Azure:OpenAI:Deployment");
    Console.WriteLine("Azure:OpenAI:Endpoint");
    Console.WriteLine("Context7:ApiKey");
    Environment.Exit(1);
}

// NEW: Define tools for our agent to use
var context7 = new HostedMcpServerTool(
    serverName: "Context7",
    serverAddress: "https://mcp.context7.com/mcp")
{
    ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
    // We can restrict the agent to only use certain tools an MCP server provides.
    // If AllowedTools is null, the tools are unrestricted.
    // AllowedTools = null
};
// Context7 requires the API key to be passed in a custom HTTP header.
context7.Headers.Add("CONTEXT7_API_KEY", apiKey);

const string instructions =
    """
    You are a friendly coding assistant that helps users review and write Go code and recommend improvements to existing Go code. 
    Create idiomatic and easy to understand Go code, but do use newer language features like generics where it makes sense. 

    Use Context7 to look up Go's standard library or third party packages and APIs. This means you must use the Context7 
    MCP tool to resolve a library id and get library docs without me having to explicitly ask. 
    
    If you are using Context7 to look up library documentation, put a Go comment at the top of the code snippet that lists the 
    libraries you used and their versions.
    """;

// NEW: Add the tool to the agent's configuration. The agent will be able to call the tool during conversations.
var agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deployment)
    .AsIChatClient()
    .AsAIAgent(instructions: instructions, name: "Gopilot", description: "An AI agent that helps users write Go code.", 
        tools: [context7]);

const string prompt =
    """
    Please create a simple "Hello World" web server, but make sure it it only accepts GET and HEAD 
    requests on "/". 
    """;

var session = await agent.CreateSessionAsync();
var response = await agent.RunAsync(prompt, session);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(prompt);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine(response.Text);
Console.WriteLine();
Console.ReadKey(intercept: true);

const string anotherPrompt = 
    """
    Please create the same web server again, but using go-chi as mux. Keep it as simple as possible and only use 
    the most basic features of go-chi.
    """;

response = await agent.RunAsync(anotherPrompt, session);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(anotherPrompt);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine(response.Text);

// await foreach (var update in agent.RunStreamingAsync(anotherPrompt, session))
// {
//     Console.Write(update.Text);
//     // Dramatic pause to simulate streaming effect
//     await Task.Delay(50);
// }

Console.ResetColor();

#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.