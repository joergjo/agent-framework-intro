using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.VectorData;

namespace AgentRAG;

// NEW: Our SearchAdapter implements the vector search. Here, we're using the vector store abstractions from MEAI,
// but this architecture allows you to implement your own SearchAdapter that retrieves data from any source using any
// retrieval API.  
public class SearchAdapter(VectorStoreCollection<string, DocumentRecord> collection, int top = 5) 
{
    public async Task<IEnumerable<TextSearchProvider.TextSearchResult>> RunAsync(string query, CancellationToken cancellationToken = default)
    {
        List<TextSearchProvider.TextSearchResult> results = [];
        await foreach (var result in collection.SearchAsync(query, top, cancellationToken: cancellationToken))
        {
            results.Add(new TextSearchProvider.TextSearchResult
            {
                Text = result.Record.Text,
                SourceName = result.Record.Title,
                RawRepresentation = result
            });
        }

        return results;
    }
}

// NEW: The .NET representation of an object in the vector store.
public class DocumentRecord
{
    [VectorStoreKey]
    [JsonPropertyName("chunk_id")]
    public required string Key { get; set; }

    [VectorStoreData]
    [JsonPropertyName("chunk")]
    public required string Text { get; set; }

    [VectorStoreData(IsIndexed = true)]
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
    [JsonPropertyName("text_vector")]
    public ReadOnlyMemory<float> Embedding { get; set; }
}