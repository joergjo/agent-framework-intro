# Microsoft Agent Framework demos

This repository contains a set of small `.NET 10` console apps that progressively demonstrate how to build AI experiences with `Microsoft.Extensions.AI` and the Microsoft Agent Framework.

The progression starts with `00_meai`, which is intentionally **not** an Agent Framework sample. It shows the lower-level `Microsoft.Extensions.AI` building block directly. The remaining demos layer in Agent Framework concepts such as sessions, tools, retrieval, memory, skills, and observability.

## Demo overview

| Folder | Project | What it demonstrates | External requirements |
| --- | --- | --- | --- |
| `00_meai` | `ChatSample.csproj` | Bare-bones chat client built directly on `Microsoft.Extensions.AI` | Azure OpenAI |
| `01_agent` | `BasicAgent.csproj` | Turning a chat client into an agent with instructions and sessions | Azure OpenAI |
| `02_mcp` | `AgentWithMCPTool.csproj` | Adding a hosted MCP tool (`Context7`) for documentation lookup | Azure OpenAI, Context7 API key |
| `03_websearch` | `AgentWithWebSearch.csproj` | Combining MCP with hosted web search | Azure OpenAI, Context7 API key |
| `04_rag` | `AgentRAG.csproj` | Retrieval-augmented generation with Azure AI Search | Azure OpenAI, Context7 API key, Azure AI Search |
| `05_memory` | `AgentMemory.csproj` | Session memory via a custom `AIContextProvider` | Same as `04_rag` |
| `06_skills` | `AgentSkills.csproj` | Loading reusable skills from `.agents/skills` | Same as `05_memory` |
| `07_otel` | `AgentOtel.csproj` | OpenTelemetry tracing exported to Azure Monitor | Same as `06_skills`, plus Azure Monitor |

## Shared prerequisites

- `.NET 10 SDK`
- An Azure OpenAI resource with:
  - a chat/completions deployment
  - an embeddings deployment for the RAG, memory, skills, and OTel demos
- Local Azure authentication that works with `DefaultAzureCredential`
  - most commonly: `az login`
- A Context7 API key for the demos that use the hosted MCP tool
- For `04_rag` and later:
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
- For `07_otel`:
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
| Chat deployment name | `Azure__OpenAI__Deployment` |
| Embeddings deployment name | `Azure__OpenAI__EmbeddingDeployment` |
| Context7 API key | `Context7__ApiKey` |
| Azure AI Search endpoint | `Azure__Search__Endpoint` |
| Azure AI Search index name | `Azure__Search__Index` |
| Azure Monitor connection string | `Azure__Monitor__ConnectionString` |

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

### `00_meai` - `Microsoft.Extensions.AI` building block

Purpose:

- Shows direct `IChatClient` usage without the Agent Framework
- Demonstrates both full-response and streaming-response calls
- Manually manages chat history

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`

Run:

```bash
dotnet run --project 00_meai/ChatSample.csproj
```

Notes:

- This demo uses the low-level OpenAI SDK client directly (not `AzureOpenAIClient`). It authenticates with `DefaultAzureCredential` scoped to `https://ai.azure.com/.default` and appends `/openai/v1/` to the configured endpoint. This is the **Azure AI Foundry** unified inference API pattern.
- `Azure__OpenAI__Endpoint` should therefore be an Azure AI Foundry project endpoint, e.g. `https://<project>.openai.azure.com/`.
- Demos `01_agent` through `07_otel` use `AzureOpenAIClient` from the `Azure.AI.OpenAI` package and work with both classic Azure OpenAI and Azure AI Foundry endpoints.

### `01_agent` - first Agent Framework sample

Purpose:

- Wraps the chat client with `.AsAIAgent(...)`
- Introduces agent instructions, sessions, and multi-turn conversation handling
- Replaces manual message lists with `CreateSessionAsync()` and `RunAsync()`

Required configuration:

- `Azure__OpenAI__Endpoint`
- `Azure__OpenAI__Deployment`

Run:

```bash
dotnet run --project 01_agent/BasicAgent.csproj
```

### `02_mcp` - hosted MCP tool integration

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
dotnet run --project 02_mcp/AgentWithMCPTool.csproj
```

### `03_websearch` - hosted web search plus MCP

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
dotnet run --project 03_websearch/AgentWithWebSearch.csproj
```

### `04_rag` - retrieval-augmented generation

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
dotnet run --project 04_rag/AgentRAG.csproj
```

Notes:

- The sample expects an Azure AI Search index containing document chunks and vectors derived from a Go book PDF that you have ingested.
- The model class maps the index fields as `chunk_id`, `chunk`, `title`, and `text_vector`.
- If you index a different book, the bundled prompts may no longer be a natural fit. Adjust them so they ask about topics that are actually covered by your indexed content.

### `05_memory` - custom session memory

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
dotnet run --project 05_memory/AgentMemory.csproj
```

### `06_skills` - reusable skills from files

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
dotnet run --project 06_skills/AgentSkills.csproj
```

Notes:

- The repository includes a `use-modern-go` skill under `06_skills/.agents/skills`.
- Skill versions are pinned in `skills-lock.json`.

### `07_otel` - tracing and observability

Purpose:

- Builds on the skills sample
- Enables OpenTelemetry on both the chat client and the agent
- Exports traces to Azure Monitor using the Azure Monitor OpenTelemetry exporter

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
dotnet run --project 07_otel/AgentOtel.csproj
```

## Suggested learning path

If you are new to the stack, a good order is:

1. `00_meai`
2. `01_agent`
3. `02_mcp`
4. `03_websearch`
5. `04_rag`
6. `05_memory`
7. `06_skills`
8. `07_otel`

That path mirrors how the samples add capabilities on top of one another.
