using AgentHost;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// NEW: Use a WebApplication instead of a Console application. This allows us to take advantage of dependency injection, configuration, and other features of ASP.NET Core. 
// It also allows us to easily expose a Chat Completion or Responses endpoint (or both) for interacting with the agent. 
var builder = WebApplication.CreateBuilder(args);

var endpoint = builder.Configuration["Azure:OpenAI:Endpoint"] ?? throw new InvalidOperationException("Azure:OpenAI:Endpoint configuration is required.");
var deployment = builder.Configuration["Azure:OpenAI:Deployment"] ?? throw new InvalidOperationException("Azure:OpenAI:Deployment configuration is required.");
var embeddingDeployment = builder.Configuration["Azure:OpenAI:EmbeddingDeployment"] ?? throw new InvalidOperationException("Azure:OpenAI:EmbeddingDeployment configuration is required.");
var apiKey = builder.Configuration["Context7:ApiKey"] ?? throw new InvalidOperationException("Context7:ApiKey configuration is required.");
var searchEndpoint = builder.Configuration["Azure:Search:Endpoint"] ?? throw new InvalidOperationException("Azure:Search:Endpoint configuration is required.");
var searchIndex = builder.Configuration["Azure:Search:Index"] ?? throw new InvalidOperationException("Azure:Search:Index configuration is required.");
var otelConnectionString = builder.Configuration["Azure:Monitor:ConnectionString"] ?? throw new InvalidOperationException("Azure:Monitor:ConnectionString configuration is required.");

// Configure OpenTelemetry using the OpenTelemetry Distro for Azure. This simplifies the setup a bit compared to the previous example.
// For example, HttpClient instrumentation is included by default.
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(configure => configure.AddService(Gopilot.ServiceName, serviceVersion: Gopilot.ServiceVersion))
    .UseAzureMonitor(options => options.ConnectionString = otelConnectionString);
builder.Services.ConfigureOpenTelemetryTracerProvider(
    (_, configure) => configure.AddSource(Gopilot.SourceName));
builder.Services.ConfigureOpenTelemetryMeterProvider(
    (_, configure) => configure.AddRuntimeInstrumentation());

// Create Azure OpenAI client, IChatClient and IEmbeddingGenerator. 
var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());

var chatClient = openAIClient.GetResponsesClient()
    .AsIChatClient(defaultModelId: deployment)
    .AsBuilder()
    .UseOpenTelemetry(sourceName: Gopilot.SourceName)   // Note that this match the source name used when configuring the OpenTelemetry SDK above. 
    .Build();

var embeddingGenerator = openAIClient
    .GetEmbeddingClient(embeddingDeployment)
    .AsIEmbeddingGenerator();

// NEW: Register AIAgent and dependencies in DI container. 
builder.Services.AddAzureAISearchVectorStore(
    new Uri(searchEndpoint),
    new DefaultAzureCredential(),
    new AzureAISearchVectorStoreOptions
    {
        EmbeddingGenerator = embeddingGenerator
    });

builder.Services.AddChatClient(chatClient);
builder.Services.AddEmbeddingGenerator(embeddingGenerator);
builder.Services.AddAIAgent("Gopilot", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var embeddingGenerator = sp.GetRequiredService<IEmbeddingGenerator>();
    var vectorStore = sp.GetRequiredService<AzureAISearchVectorStore>();

    // We create these dependencies here in the factory method because we don't need them elsewhere.
    var collection = vectorStore.GetCollection<string, DocumentRecord>(searchIndex);
    var searchAdapter = new SearchAdapter(collection);
    var textSearchProvider = new TextSearchProvider(
        searchAdapter.RunAsync,
        new TextSearchProviderOptions
        {
            SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke
        });

    var context7 = 
        new HostedMcpServerTool(
            serverName: "Context7",
            serverAddress: "https://mcp.context7.com/mcp")
        {
            ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire
        };
    context7.Headers ??= new Dictionary<string, string>();
    context7.Headers.Add("CONTEXT7_API_KEY", apiKey);

    var userInfoMemory = new UserInfoMemory(chatClient);
    var skillsProvider = new AgentSkillsProvider("./.agents/skills");

    return chatClient.AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = key,
            Description = "An AI agent that helps users write Go code.",
            ChatOptions = new ChatOptions
            {
                Instructions = Gopilot.Instructions,
                Tools = [context7, new HostedWebSearchTool()],
            },
            AIContextProviders = [textSearchProvider, userInfoMemory, skillsProvider]
        })
        .AsBuilder()
        .UseOpenTelemetry(sourceName: Gopilot.SourceName)
        .Build();
});

// NEW: Register DevUI, Response and Conversation API services with dependency injection. 
builder.Services.AddDevUI();
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// We're hardcoding the URL and port here for simplicity, but in a real application you would likely want to make this configurable.
builder.WebHost.UseUrls("http://localhost:9900");

// New: Build the web application so that we can start handling requests.
var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();

// Note that in a real-world application, you would only map DevUI for the development environment.
// if (app.Environment.IsDevelopment())
// {
//     app.MapDevUI();
// }

app.Run();

#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.