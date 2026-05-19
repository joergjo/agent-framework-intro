# Microsoft Agent Framework demos

This repository contains a set of small `.NET 10` console apps that progressively demonstrate how to build AI experiences with `Microsoft.Extensions.AI` and the Microsoft Agent Framework.

The progression starts with `00_sdk`and `01_meai`, which are intentionally **not** Agent Framework samples. They show the lower-level programming models of the OpenAI SDK and  `Microsoft.Extensions.AI`. 
The remaining demos layer in Agent Framework concepts such as sessions, tools, retrieval, memory, skills, and observability.

## Demo overview

| Folder | Project | What it demonstrates | External requirements |
| --- | --- | --- | --- |
| [`00_sdk`](./00_sdk) | `ResponsesSample.csproj` | Bare-bones OpenAI Responses client built with the OpenAI SDK | Azure OpenAI |
| [`01_meai`](./01_meai) | `ChatSample.csproj` | Bare-bones `IChatClient` built with `Microsoft.Extensions.AI` | Azure OpenAI |
| [`02_agent`](./02_agent) | `BasicAgent.csproj` | Turning a chat client into an agent with instructions and sessions | Azure OpenAI |
| [`03_mcp`](./03_mcp) | `AgentWithMCPTool.csproj` | Adding a hosted MCP tool (`Context7`) for documentation lookup | Azure OpenAI, Context7 API key |
| [`04_websearch`](./04_websearch) | `AgentWithWebSearch.csproj` | Combining MCP with hosted web search | Azure OpenAI, Context7 API key |
| [`05_rag`](./05_rag) | `AgentRAG.csproj` | Retrieval-augmented generation with Azure AI Search | Azure OpenAI, Context7 API key, Azure AI Search |
| [`06_memory`](./06_memory) | `AgentMemory.csproj` | Session memory via a custom `AIContextProvider` | Same as `05_rag` |
| [`07_skills`](./07_skills) | `AgentSkills.csproj` | Loading reusable skills from `.agents/skills` | Same as `06_memory` |
| [`08_otel`](./08_otel) | `AgentOtel.csproj` | OpenTelemetry traces and metrics exported to Azure Monitor | Same as `07_skills`, plus Azure Monitor |
| [`09_host`](./09_host) | `AgentHost.csproj` | Hosting the agent as an ASP.NET Core web app with OpenAI-compatible endpoints and DevUI | Same as `08_otel` |

## Shared prerequisites

- `.NET 10 SDK`
- An Azure OpenAI or Microsoft Foundry resource with:
  - an OpenAI model deployment (e.g., gpt-5.2)
  - an embeddings deployment for the RAG, memory, skills, and OTel demos
- Local Azure authentication that works with `DefaultAzureCredential`
  - most commonly: `az login`
- A Context7 API key for the demos that use the hosted MCP tool
- For `05_rag` and later samples:
  - an Azure AI Search service
  - a suitable Go book in PDF form that you ingest and index into that search service
  - chunked content and embeddings generated from that book so the retrieval step has relevant material to return
  - a book whose content matches the kinds of questions you ask in the sample prompts
  - an index that matches the sample schema:
    - `chunk_id` as key
    - `chunk` as the text payload
    - `title` as filterable/indexed metadata
    - `text_vector` as the embedding vector
  - note that there are free Go ebooks available online, but the exact prompts in `04_rag` may need to be adapted to fit the book you indexed
  - see this [quickstart](https://learn.microsoft.com/en-us/azure/search/search-get-started-portal-import-vectors?tabs=storage-access%2Cblob-storage%2Caoai%2Cvectorize-images)
    if you are not familiar with Azure AI Search
- For `08_otel` and later samples:
  - an Azure Monitor / Application Insights connection string


## Important note about environment variable names

The code accesses configuration keys such as `Azure:OpenAI:Endpoint`, but when you set them in shells you should use **double underscores**:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`

That is the standard `.NET` environment-variable mapping for hierarchical configuration and works across bash, Command Prompt, and PowerShell.

## Authenticate to Azure

Before running any sample, sign in so `DefaultAzureCredential` can authenticate:

### bash

```bash
az login
```

### Windows Command Prompt

```bat
az login
```

### PowerShell

```powershell
az login
```

## Common configuration

These variables are used throughout the repo.

| Purpose | Environment variable |
| --- | --- |
| Azure OpenAI endpoint | `Azure__OpenAI__Endpoint` |
| Model deployment name | `Azure__OpenAI__Deployment` |
| Embeddings deployment name | `Azure__OpenAI__EmbeddingDeployment` |
| Context7 API key | `Context7__ApiKey` |
| Azure AI Search endpoint | `Azure__Search__Endpoint` |
| Azure AI Search index name | `Azure__Search__Index` |
| Azure Monitor connection string | `Azure__Monitor__ConnectionString` |

> Note: When using the Azure OpenAI endpoint of a Microsoft Foundry project, make sure *not* to include
> any path like `openai/v1`. Just use the base URL, ie.e, `https://<your-resource>.openai.azure.com/`.



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
set Azure__OpenAI__Endpoint=https://<your-openai-resource>.openai.azure.com/
set Azure__OpenAI__Deployment=<chat-deployment>
set Azure__OpenAI__EmbeddingDeployment=<embedding-deployment>
set Context7__ApiKey=<context7-api-key>
set Azure__Search__Endpoint=https://<your-search-service>.search.windows.net
set Azure__Search__Index=<search-index>
set Azure__Monitor__ConnectionString=<application-insights-connection-string>
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

You only need to set the variables required by the demo you are running.

## Build the repo

From the repository root:

```bash
dotnet build AgentFrameworkIntro.slnx
```

## Running the demos

Each sample is a console app. Run them from the repo root with `dotnet run --project ...`.

### `00_sdk` - OpenAI SDK with Responses API

Purpose:

- Shows the bare-bones OpenAI SDK without any abstraction layers
- Uses the OpenAI Responses API directly via `OpenResponsesClient`
- Demonstrates both full-response and streaming-response calls
- Maintains conversation context using `PreviousResponseId`

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`

Run:

```bash
dotnet run --project 00_sdk/ResponsesSample.csproj
```

Notes:

- This demo uses `BearerTokenPolicy` with `DefaultAzureCredential` for authentication against Azure OpenAI.
- The endpoint must include the base URL only (no path like `/openai/v1` — the SDK appends it automatically).

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

Notes:

- This demo uses the low-level OpenAI SDK client directly (not `AzureOpenAIClient`). The other demos
  use `AzureOpenAIClient` for conciseness.

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

- Adds a hosted `Context7` MCP server as an agent tool
- Lets the agent look up Go library and API documentation during a run
- Demonstrates passing provider-specific headers to a hosted MCP tool

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

- Adds embeddings and Azure AI Search-backed retrieval
- Uses a `TextSearchProvider` to inject retrieved documents into the agent context
- Demonstrates a simple vector-store-backed `SearchAdapter`

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

### `07_skills` - reusable skills from files

Purpose:

- Builds on the memory sample
- Loads skills from `.agents/skills` using `FileAgentSkillsProvider`
- Demonstrates how local skill definitions can influence agent behavior

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

- The repository includes a `use-modern-go` skill under `06_skills/.agents/skills`.
- Skill versions are pinned in `skills-lock.json`.

### `08_otel` - observability

Purpose:

- Builds on the skills sample
- Enables OpenTelemetry on both the chat client and the agent
- Exports traces and metrcis to Azure Monitor using the Azure Monitor OpenTelemetry exporter

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

### `09_host` - hosted web application

Purpose:

- Builds on the OTel sample
- Replaces the console host with an ASP.NET Core `WebApplication`
- Registers the agent and its dependencies in the DI container using `AddAIAgent`
- Exposes OpenAI-compatible Chat Completion and Responses endpoints via `MapOpenAIResponses` and `MapOpenAIConversations`
- Includes the Agent Framework DevUI for browser-based interaction
- Uses the Azure Monitor OpenTelemetry Distro (`UseAzureMonitor`) for simplified observability setup

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

- The app listens on `http://localhost:9900` by default.
- Open `http://localhost:9900/devui` in a browser to interact with the agent through the DevUI.

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
