using System.ComponentModel;
using Microsoft.SemanticKernel;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System.Text;

public class RAGPlugin
{
    private readonly SearchClient _searchClient;

    public RAGPlugin(string endpoint, string apiKey, string indexName)
    {
        _searchClient = new SearchClient(
            new Uri(endpoint),
            indexName,
            new AzureKeyCredential(apiKey));
    }

    [KernelFunction("retrieve_documents")]
    [Description("Retrieves relevant documents from Azure AI Search based on a query")]
    public async Task<string> RetrieveDocumentsAsync(string query)
    {
        var searchOptions = new SearchOptions
        {
            Size = 5 // Get top 5 results
        };

        // Add select fields specific to your vector index
        searchOptions.Select.Add("chunk");
        searchOptions.Select.Add("title");
        searchOptions.Select.Add("chunk_id");
        
        var searchResult = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions);
        
        var contextBuilder = new StringBuilder();
        
        var results = searchResult.Value.GetResults();
        
        contextBuilder.AppendLine("### Retrieved Information:");
        
        foreach (var result in results)
        {
            var document = result.Document;
            string title = "";
            string chunk = "";
            string chunkId = "";
            
            if (document.TryGetValue("title", out object? titleObj))
            {
                title = titleObj?.ToString() ?? "";
            }
            
            if (document.TryGetValue("chunk", out object? chunkObj))
            {
                chunk = chunkObj?.ToString() ?? "";
            }
            
            if (document.TryGetValue("chunk_id", out object? chunkIdObj))
            {
                chunkId = chunkIdObj?.ToString() ?? "";
            }
            
            double score = result.Score ?? 0.0;
            
            contextBuilder.AppendLine($"Document: {title}");
            contextBuilder.AppendLine($"Passage ID: {chunkId}");
            contextBuilder.AppendLine($"Relevance: {score:F2}");
            contextBuilder.AppendLine($"Content: {chunk}");
            contextBuilder.AppendLine();
        }
        
        return contextBuilder.ToString();
    }
} 