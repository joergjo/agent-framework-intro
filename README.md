# Microsoft Agent Framework demos

This repository contains small .NET 10 samples that show, step by step, how to build an AI coding assistant with `Microsoft.Extensions.AI` and Microsoft Agent Framework. The assistant, called **Gopilot**, helps write Go code. You do not need to install Go to run the samples: they generate code as text rather than execute it.

The first two samples, [`00_sdk`](./00_sdk) and [`01_meai`](./01_meai), do **not** use Agent Framework. They show how to call a model directly using the OpenAI SDK and then the `IChatClient` interface from `Microsoft.Extensions.AI`.
The remaining samples use Agent Framework to add conversation management, tools, document retrieval, memory, reusable instructions, and monitoring. Samples 00–08 run in the terminal; sample 09 runs as a web app.

## Key concepts

- **Model**: the AI service (LLM) that generates a response from the instructions and messages you send it.
- **Agent**: a model-based assistant with instructions and optional tools. Agent Framework manages running the assistant and supplying its context.
- **Session**: the state of a conversation, which lets the agent continue across multiple turns.
- **Tool**: a capability the model can call, such as looking up documentation or searching the web. MCP (Model Context Protocol) is a standard for connecting to tool servers.
- **Retrieval-augmented generation (RAG)**: finding relevant passages in your own documents and including them in the model's context before it answers. This does not retrain the model.
- **Context provider** (`AIContextProvider`): an Agent Framework component that supplies extra context, such as retrieved passages or remembered user preferences.
- **Skill**: a set of reusable instructions and optional supporting files that the agent can load when relevant.
- **Observability**: traces and metrics that help you understand what the app is doing and diagnose problems. These samples use OpenTelemetry to collect telemetry and Azure Monitor to store it.

## Demo overview

| Folder | Project | What it demonstrates | External requirements |
| --- | --- | --- | --- |
| [`00_sdk`](./00_sdk) | `ResponsesSample.csproj` | Calling the Responses API directly with the OpenAI SDK | Azure OpenAI |
| [`01_meai`](./01_meai) | `ChatSample.csproj` | Calling the same API through `IChatClient` from `Microsoft.Extensions.AI` | Azure OpenAI |
| [`02_agent`](./02_agent) | `BasicAgent.csproj` | Turning a chat client into an agent with instructions and sessions | Azure OpenAI |
| [`03_mcp`](./03_mcp) | `AgentWithMCPTool.csproj` | Connecting to the remote Context7 MCP server for documentation lookup | Azure OpenAI, Context7 API key |
| [`04_websearch`](./04_websearch) | `AgentWithWebSearch.csproj` | Combining MCP with hosted web search | Azure OpenAI, Context7 API key |
| [`05_rag`](./05_rag) | `AgentRAG.csproj` | Retrieval-augmented generation with Azure AI Search | Azure OpenAI, Context7 API key, Azure AI Search |
| [`06_memory`](./06_memory) | `AgentMemory.csproj` | Session memory via a custom `AIContextProvider` | Same as `05_rag` |
| [`07_skills`](./07_skills) | `AgentSkills.csproj` | Loading reusable skills from files | Same as `06_memory` |
| [`08_otel`](./08_otel) | `AgentOtel.csproj` | OpenTelemetry traces and metrics exported to Azure Monitor | Same as `07_skills`, plus Azure Monitor |
| [`09_host`](./09_host) | `AgentHost.csproj` | Hosting the agent as an ASP.NET Core web app with OpenAI-compatible endpoints and DevUI | Same as `08_otel` |

## Shared prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An Azure OpenAI or Microsoft Foundry resource with:
  - a model deployment of a reasoning model that supports the Responses API
  - support for hosted MCP tools for sample 03 onward, and hosted web search for sample 04 onward; check availability for your model and region
  - an embeddings deployment for samples 05–09
- Local Azure authentication:
  - samples 00–08 use `DefaultAzureCredential`, which can use your Azure CLI sign-in
  - sample 09 temporarily uses `AzureCliCredential` instead of `DefaultAzureCredential` to work around an [Azure.Core bug](https://github.com/Azure/azure-sdk-for-net/issues/59961)
  - the signed-in account needs permission to invoke the model and embeddings deployments and, for samples 05–09, query the search index; signing in alone does not grant access
- A [Context7](https://context7.com/) API key for samples 03–09
- For samples 05–09:
  - an Azure AI Search service
  - a suitable Go book in PDF form that you ingest and index into that search service
  - chunked content and embeddings generated from that book so the retrieval step has relevant material to return
  - a book whose content matches the kinds of questions you ask in the sample prompts
  - an index that matches the sample schema:
    - `chunk_id` as key
    - `chunk` as the text payload
    - `title` as filterable/indexed metadata
    - `text_vector` as a 1,536-dimensional embedding vector using cosine similarity
  - use the same embedding model and vector dimensions for indexing the book and for queries; the sample record type declares 1,536 dimensions
  - use a book you have permission to process; adapt the prompts in `05_rag` to fit its content
  - see this [quickstart](https://learn.microsoft.com/en-us/azure/search/search-get-started-portal-import-vectors?tabs=storage-access%2Cblob-storage%2Caoai%2Cvectorize-images)
    if you are not familiar with Azure AI Search
- For samples 08–09:
  - an Azure Monitor / Application Insights connection string

The samples connect to existing services. They do not create Azure resources, ingest the book, or build the search index. Model calls, hosted tools, search, and telemetry can incur charges.

## Important note about environment variable names

The code accesses configuration keys such as `Azure:OpenAI:Endpoint`, but when you set them in shells you should use **double underscores**:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`

That is the standard `.NET` environment-variable mapping for hierarchical configuration and works across bash, Command Prompt, and PowerShell.

## Authenticate to Azure

If you are using the Azure CLI, run the following in any of the shells shown below:

```text
az login
```

This supplies credentials for `DefaultAzureCredential` in samples 00–08. It is required for sample 09 because of the temporary `AzureCliCredential` workaround described above.

## Common configuration

These variables are used throughout the repo.

| Purpose | Environment variable | Required by samples |
| --- | --- | --- |
| Azure OpenAI endpoint | `Azure__OpenAI__Endpoint` | 00–09 |
| Model deployment name | `Azure__OpenAI__Deployment` | 00–09 |
| Embeddings deployment name | `Azure__OpenAI__EmbeddingDeployment` | 05–09 |
| Context7 API key | `Context7__ApiKey` | 03–09 |
| Azure AI Search endpoint | `Azure__Search__Endpoint` | 05–09 |
| Azure AI Search index name | `Azure__Search__Index` | 05–09 |
| Azure Monitor connection string | `Azure__Monitor__ConnectionString` | 08–09 |

> Use the resource's Azure OpenAI base URL, for example `https://<your-resource>.openai.azure.com/`, not a Foundry project URL. Do not append `/openai/v1/`: the code adds that path.

Replace the placeholders below with your own values. Deployment names are the names you assigned in Azure, not necessarily the underlying model names. You only need the variables required by the sample you choose.
### bash

```bash
export Azure__OpenAI__Endpoint="https://<your-openai-resource>.openai.azure.com/"
export Azure__OpenAI__Deployment="<chat-deployment>"
export Azure__OpenAI__EmbeddingDeployment="<embedding-deployment>"
export Context7__ApiKey="<context7-api-key>"
export Azure__Search__Endpoint="https://<your-search-service>.search.windows.net"
export Azure__Search__Index="<search-index>"
export Azure__Monitor__ConnectionString="<application-insights-connection-string>"
```

### Windows Command Prompt

```bat
set "Azure__OpenAI__Endpoint=https://<your-openai-resource>.openai.azure.com/"
set "Azure__OpenAI__Deployment=<chat-deployment>"
set "Azure__OpenAI__EmbeddingDeployment=<embedding-deployment>"
set "Context7__ApiKey=<context7-api-key>"
set "Azure__Search__Endpoint=https://<your-search-service>.search.windows.net"
set "Azure__Search__Index=<search-index>"
set "Azure__Monitor__ConnectionString=<application-insights-connection-string>"
```

### PowerShell

```powershell
$env:Azure__OpenAI__Endpoint = "https://<your-openai-resource>.openai.azure.com/"
$env:Azure__OpenAI__Deployment = "<chat-deployment>"
$env:Azure__OpenAI__EmbeddingDeployment = "<embedding-deployment>"
$env:Context7__ApiKey = "<context7-api-key>"
$env:Azure__Search__Endpoint = "https://<your-search-service>.search.windows.net"
$env:Azure__Search__Index = "<search-index>"
$env:Azure__Monitor__ConnectionString = "<application-insights-connection-string>"
```

Run the sample from the same terminal where you set these variables. These commands set them for the current shell session; they do not save them permanently.

## Build the repo

From the repository root:

```bash
dotnet build AgentFrameworkIntro.slnx
```

## Running the demos

For a first run, set just the two Azure OpenAI variables and start sample 00. Add the other services as you progress.

Run the commands below from the repository root. Samples 00–08 send predefined prompts, print the answers, and pause for a keypress before the next prompt. They are not interactive chat loops. To try your own questions, edit the prompt strings in the sample's `Program.cs`. Sample 09 provides a browser-based chat interface instead.

### `00_sdk` - OpenAI SDK with Responses API

Purpose:

- Shows the bare-bones OpenAI SDK without any abstraction layers
- Uses the OpenAI Responses API directly via `ResponsesClient`
- Demonstrates both full-response and streaming-response calls
- Maintains conversation context using `PreviousResponseId`

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`

Run:

```bash
dotnet run --project 00_sdk/ResponsesSample.csproj
```

### `01_meai` - `Microsoft.Extensions.AI` building block

Purpose:

- Shows direct `IChatClient` usage without the Agent Framework
- Demonstrates both full-response and streaming-response calls
- Manually manages chat history

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`

Run:

```bash
dotnet run --project 01_meai/ChatSample.csproj
```

### `02_agent` - first Agent Framework sample

Purpose:

- Wraps the chat client with `.AsAIAgent(...)`
- Introduces agent instructions, sessions, and multi-turn conversation handling
- Replaces manual message lists with `CreateSessionAsync()` and `RunAsync()`

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`

Run:

```bash
dotnet run --project 02_agent/BasicAgent.csproj
```

### `03_mcp` - hosted MCP tool integration

Purpose:

- Adds the remote Context7 MCP server through `HostedMcpServerTool`; the model service calls the server, so you do not run an MCP server locally
- Lets the agent look up Go library and API documentation during a run
- Demonstrates passing provider-specific headers to a hosted MCP tool
- Allows calls to the server without asking for user approval (`NeverRequire`); review that choice before reusing it with other servers

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`
- `Context7__ApiKey`

Run:

```bash
dotnet run --project 03_mcp/AgentWithMCPTool.csproj
```

### `04_websearch` - hosted web search plus MCP

Purpose:

- Keeps the `Context7` MCP tool
- Adds `HostedWebSearchTool` for current information beyond static library docs
- Demonstrates combining multiple tools in a single agent

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`
- `Context7__ApiKey`

Run:

```bash
dotnet run --project 04_websearch/AgentWithWebSearch.csproj
```

### `05_rag` - retrieval-augmented generation

Purpose:

- Converts each search query into an embedding (a numeric representation of text) and searches for similar document passages in Azure AI Search
- Uses a `TextSearchProvider` to add those passages to the agent's context before the model is called
- Implements the search in `SearchAdapter`, which returns up to five matching passages and their source titles
- Keeps the Context7 and hosted web search tools from the previous sample

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`
- `Azure__OpenAI__EmbeddingDeployment`
- `Context7__ApiKey`
- `Azure__Search__Endpoint`
- `Azure__Search__Index`

Run:

```bash
dotnet run --project 05_rag/AgentRAG.csproj
```

Notes:

- The sample expects an Azure AI Search index containing document chunks and vectors derived from a Go book PDF that you have ingested.
- The model class maps the index fields as `chunk_id`, `chunk`, `title`, and `text_vector`.
- If you index a different book, the bundled prompts may no longer be a natural fit. Adjust them so they ask about topics that are actually covered by your indexed content.

### `06_memory` - custom session memory

Purpose:

- Builds on the RAG sample
- Adds a custom `UserInfoMemory` `AIContextProvider`
- Extracts reusable user preferences from prompts and feeds them back in later turns
- Demonstrates session serialization and deserialization

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`
- `Azure__OpenAI__EmbeddingDeployment`
- `Context7__ApiKey`
- `Azure__Search__Endpoint`
- `Azure__Search__Index`

Run:

```bash
dotnet run --project 06_memory/AgentMemory.csproj
```

Notes:

- The first prompt supplies coding preferences. `UserInfoMemory` uses a model call to extract them and stores them in the session for later turns.
- The serialized session stays in memory in this demo. It is not saved to disk or shared across users or new sessions.

### `07_skills` - reusable skills from files

Purpose:

- Builds on the memory sample
- Loads bundled skill files from the build output's `skills` directory using `AgentSkillsProvider`
- Lets the agent load reusable Go coding guidance when relevant
- Automatically approves loading skills and reading their resources; this is not blanket approval to execute arbitrary code

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`
- `Azure__OpenAI__EmbeddingDeployment`
- `Context7__ApiKey`
- `Azure__Search__Endpoint`
- `Azure__Search__Index`

Run:

```bash
dotnet run --project 07_skills/AgentSkills.csproj
```

Notes:

- The repository includes a `use-modern-go` skill under [`07_skills/.agents/skills`](./07_skills/.agents/skills), with copies in samples 08 and 09.
- In samples 07–09, MSBuild copies `.agents/skills` into a `skills` directory next to the app during build and publish, preserving its subfolders. `AgentSkillsProvider` loads that directory at runtime; no manual copying is needed.
- `skills-lock.json` records the skill's source and content hash; the app loads local files and does not download skills from that lock file.

### `08_otel` - observability

Purpose:

- Builds on the skills sample
- Enables OpenTelemetry on both the chat client and the agent
- Exports traces and metrics to Azure Monitor using the Azure Monitor OpenTelemetry exporter

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`
- `Azure__OpenAI__EmbeddingDeployment`
- `Context7__ApiKey`
- `Azure__Search__Endpoint`
- `Azure__Search__Index`
- `Azure__Monitor__ConnectionString`

Run:

```bash
dotnet run --project 08_otel/AgentOtel.csproj
```

This sample uses `DefaultAzureCredential` for model, embedding, and Azure AI Search calls, just like the preceding samples.

### `09_host` - hosted web application

Purpose:

- Builds on the OTel sample
- Replaces the console host with an ASP.NET Core `WebApplication`
- Registers the agent with `AddAIAgent` and uses dependency injection (DI) to supply the services it needs
- Exposes OpenAI-compatible Responses and Conversations endpoints via `MapOpenAIResponses` and `MapOpenAIConversations`; it does not map a Chat Completions endpoint
- Includes the Agent Framework development UI (DevUI) for browser-based interaction
- Uses the Azure Monitor OpenTelemetry distribution (`UseAzureMonitor`) to simplify telemetry setup

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`
- `Azure__OpenAI__EmbeddingDeployment`
- `Context7__ApiKey`
- `Azure__Search__Endpoint`
- `Azure__Search__Index`
- `Azure__Monitor__ConnectionString`

Run:

```bash
dotnet run --project 09_host/AgentHost.csproj
```

Notes:

- Run `az login` first: this sample temporarily uses `AzureCliCredential` for model, embedding, and Azure AI Search calls to work around an [Azure.Core bug](https://github.com/Azure/azure-sdk-for-net/issues/59961). This is a workaround, not an intentional change in authentication approach.
- The app is configured to listen on `http://localhost:9900`.
- Open `http://localhost:9900/devui` in a browser to interact with the agent through the DevUI.
- Press Ctrl+C in the terminal to stop the app.
- This is a local development demo: DevUI is always enabled, and the endpoints have no authentication configured. Add authentication and restrict DevUI before exposing the app beyond your machine.

## Suggested learning path

If you are new to the stack, a good order is:

1. `00_sdk`
2. `01_meai`
3. `02_agent`
4. `03_mcp`
5. `04_websearch`
6. `05_rag`
7. `06_memory`
8. `07_skills`
9. `08_otel`
10. `09_host`

That path mirrors how the samples add capabilities on top of one another.
