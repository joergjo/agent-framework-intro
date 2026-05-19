using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.VectorData;

namespace AgentOtel;

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