using AgentMemory;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();
var endpoint = configuration["Azure:OpenAI:Endpoint"];
var deployment = configuration["Azure:OpenAI:Deployment"];
var embeddingDeployment = configuration["Azure:OpenAI:EmbeddingDeployment"];
var apiKey = configuration["Context7:ApiKey"];
var searchEndpoint = configuration["Azure:Search:Endpoint"];
var searchIndex = configuration["Azure:Search:Index"];

if (deployment is not { Length: > 0 } ||
    endpoint is not { Length: > 0 } ||
    embeddingDeployment is not { Length: > 0 } ||
    apiKey is not { Length: > 0 } ||
    searchEndpoint is not { Length: > 0 } ||
    searchIndex is not { Length: > 0 })
{
    Console.WriteLine("Please set the following environment variables:");
    Console.WriteLine("Azure:OpenAI:Deployment");
    Console.WriteLine("Azure:OpenAI:Endpoint");
    Console.WriteLine("Azure:OpenAI:EmbeddingDeployment");
    Console.WriteLine("Context7:ApiKey");
    Console.WriteLine("Azure:Search:Endpoint");
    Console.WriteLine("Azure:Search:Index");
    Environment.Exit(1);
}

var context7 = new HostedMcpServerTool(
    serverName: "Context7",
    serverAddress: "https://mcp.context7.com/mcp")
{
    ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire
};
// Context7 requires the API key to be passed in a custom HTTP header.
context7.Headers ??= new Dictionary<string, string>();
context7.Headers.Add("CONTEXT7_API_KEY", apiKey);

const string instructions =
    """
    You are a friendly coding assistant that helps users review and write Go code and recommend improvements to existing Go code. 
    Create idiomatic and easy to understand Go code, but do use newer language features like generics where it makes sense. 

    Use Context7 to look up Go's standard library or third party packages and APIs. This means you must use the Context7 
    MCP tool to resolve a library id and get library docs without me having to explicitly ask. 
    
    If you are using Context7 to look up library documentation, put a Go comment at the top of the code snippet that lists the 
    libraries you used and their versions.

    Use the Web Search tool to search the web for more recent information, which is especially useful for programming tasks. 
    For example, you can use it to find out about new Go language features or popular third-party libraries.

    Include the provided context in your answers where it applies and cite the source document when available.
    """;

var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());

var embeddingGenerator = openAIClient
    .GetEmbeddingClient(embeddingDeployment)
    .AsIEmbeddingGenerator();

var vectorStore = new AzureAISearchVectorStore(
    new SearchIndexClient(
        new Uri(searchEndpoint),
        new DefaultAzureCredential()),
        new AzureAISearchVectorStoreOptions
        {
            EmbeddingGenerator = embeddingGenerator
        });
var collection = vectorStore.GetCollection<string, DocumentRecord>(searchIndex);
var searchAdapter = new SearchAdapter(collection);

var textSearchProvider = new TextSearchProvider(
    searchAdapter.RunAsync,
    new TextSearchProviderOptions
    {
        SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke
    });

// NEW: We need to the IChatClient for our UserInfoMemory AIContextProvider, so we store a reference to it.
var chatClient = openAIClient.GetResponsesClient().AsIChatClient(defaultModelId: deployment);

// NEW: We also provide our UserInfoMemory AIContextProvider
var userInfoMemory = new UserInfoMemory(chatClient);

var agent = chatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        Name = "Gopilot",
        Description = "An AI agent that helps users write Go code.",
        ChatOptions = new ChatOptions
        {
            Instructions = instructions,
            Tools = [context7, new HostedWebSearchTool()],
        },
        AIContextProviders = [textSearchProvider, userInfoMemory]
    });

const string prompt =
    """
    Apply the following rules when generating Go code: 
    - Use log/slog for logging
    - Prefer "any" over "interface{}"
    - When logging timestamps, always use UTC
    """;

var session = await agent.CreateSessionAsync();
var response = await agent.RunAsync(prompt, session);

// NEW: We serialize the current session to a JSON element. For simplicity, we're just 
// keeping the serialized session in memory, but you could also store it in a database or file 
// for later retrieval. 
var serializedSession = await agent.SerializeSessionAsync(session);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(prompt);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine(response.Text);
Console.WriteLine();
Console.ReadKey(intercept: true);

const string anotherPrompt =
    """
    Create a simple "Hello World" web server.
    """;

// NEW: We deserialize the previous session and continue where we left off. 
session = await agent.DeserializeSessionAsync(serializedSession);
response = await agent.RunAsync(anotherPrompt, session);

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Resuming session with previously saved memory...");
Console.WriteLine();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(anotherPrompt);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine(response.Text);

Console.ResetColor();

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.