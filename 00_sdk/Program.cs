using System.ClientModel.Primitives;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Responses;

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

// Create the OpenAI SDK client that can be used as a factory for all specific OpenAI clients (e.g. chat, embeddings, etc.)
var openAIClient = new OpenAIClient(
    new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
    new OpenAIClientOptions { Endpoint = new Uri(baseUri, "/openai/v1/") });

// Create a client for the Responses API
var responsesClient = openAIClient.GetResponsesClient();

const string instructions =
    """
    You are a friendly coding assistant that helps users review and write Go code and recommend improvements to existing Go code. 
    Create idiomatic and easy to understand Go code, but do use newer language features like generics where it makes sense. 
    """;

const string prompt =
    """
    Please create a simple "Hello World" web server.
    """;

// Create reponse options - model, instructions, reasoning effort level, etc.
var options = new CreateResponseOptions()
{
    Model = deployment,
    ReasoningOptions = new ResponseReasoningOptions()
    {
        ReasoningEffortLevel = ResponseReasoningEffortLevel.None,
    }
};

// Add the user prompt as an input item to the response options
options.InputItems.Add(ResponseItem.CreateDeveloperMessageItem(instructions));
options.InputItems.Add(ResponseItem.CreateUserMessageItem(prompt));

// Get full response
ResponseResult response = await responsesClient.CreateResponseAsync(options);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(prompt);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine(response.GetOutputText());
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Press any key");
Console.ResetColor();
Console.ReadKey(intercept: true);

const string anotherPrompt =
    """
    Update the code so that it only accepts GET and HEAD requests on "/".
    """;

options = new CreateResponseOptions()
{
    // Note that model deployment is used as the model name
    Model = deployment,
    ReasoningOptions = new ResponseReasoningOptions()
    {
        ReasoningEffortLevel = ResponseReasoningEffortLevel.None,
    },
    // Set the previous response id to maintain context
    PreviousResponseId = response.Id,
    // Enable streaming
    StreamingEnabled = true
};

// Add the user prompt as an input item to the response options
options.InputItems.Add(ResponseItem.CreateDeveloperMessageItem(instructions));
options.InputItems.Add(ResponseItem.CreateUserMessageItem(anotherPrompt));

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(anotherPrompt);
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Blue;

// Stream response
await foreach (var update in responsesClient.CreateResponseStreamingAsync(options))
{
    if (update is StreamingResponseOutputTextDeltaUpdate outputTextUpdate)
    {
        Console.Write(outputTextUpdate.Delta);
        // Dramatic pause to simulate streaming effect.
        await Task.Delay(50);
    }
}

Console.ResetColor();

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.