using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System.Text;
using System.IO;
using System.Threading;

// Get the path to the existing appsettings.json using a reliable method
string relativePath = Path.Combine("..", "appsettings.json");
string filePath = Path.GetFullPath(relativePath);
Console.WriteLine($"Using config file at: {filePath}");

try {
    if (!File.Exists(filePath)) {
        Console.WriteLine($"Warning: File not found at {filePath}");
        throw new FileNotFoundException($"Could not find appsettings.json file at {filePath}");
    }
    
    var config = new ConfigurationBuilder()
        // Important: Use the physical path here, not the relative path
        .AddJsonFile(filePath, optional: false)
        // Add user secrets as a fallback
        .AddUserSecrets<Program>(optional: true)
        .Build();
    
    // Set your values in appsettings.json
    string modelId = config["modelId"]!;
    string endpoint = config["endpoint"]!;
    string apiKey = config["apiKey"]!;
    
    // Azure AI Search configuration
    string searchEndpoint = config["azureAISearch:endpoint"]!;
    string searchApiKey = config["azureAISearch:apiKey"]!;
    string indexName = config["azureAISearch:indexName"]!;
    
    // Read max conversation turns (default to 5 if not specified)
    int maxConversationTurns = 5;
    if (int.TryParse(config["maxConversationTurns"], out int configuredTurns))
    {
        maxConversationTurns = configuredTurns;
    }
    Console.WriteLine($"Using maximum of {maxConversationTurns} conversation turns in history");
    
    // Create a kernel with Azure OpenAI chat completion
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

    // Build the kernel
    Kernel kernel = kernelBuilder.Build();

    // Get chat completion service.
    var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

    // Create search client for RAG
    var searchClient = new SearchClient(
        new Uri(searchEndpoint),
        indexName,
        new AzureKeyCredential(searchApiKey));

    // Create a chat history object
    ChatHistory chatHistory = [];

    // After building the kernel, register the RAG plugin
    var ragPlugin = new RAGPlugin(searchEndpoint, searchApiKey, indexName);
    kernel.ImportPluginFromObject(ragPlugin, "RAG");

    // Function to perform RAG search and augment the prompt
    async Task<string> RetrieveAndAugmentAsync(string userQuery)
    {
        // Use the RAG plugin to retrieve documents
        var arguments = new KernelArguments { ["query"] = userQuery };
        var result = await kernel.InvokeAsync("RAG", "retrieve_documents", arguments);
        return result.ToString();
    }

    void AddMessage(string msg) {
        Console.WriteLine(msg);
        chatHistory.AddAssistantMessage(msg);
    }

    void GetInput() {
        string input = Console.ReadLine()!;
        chatHistory.AddUserMessage(input);
    }

    async Task GetReply() {
        try {
            // Get the last user message
            string userQuery = chatHistory.Last(m => m.Role == AuthorRole.User).Content!;
            
            Console.WriteLine("Processing your question: " + userQuery);
            
            // Add debugging to help track where the issue is occurring
            Console.WriteLine("Retrieving documents...");
            
            // Retrieve context from Azure AI Search
            string retrievedContext = await RetrieveAndAugmentAsync(userQuery);
            
            Console.WriteLine("Documents retrieved successfully. Building augmented history...");
            
            // Create a fresh chat history for this request
            var augmentedChatHistory = new ChatHistory();
            
            // Step 1: Add all SYSTEM messages from the original history 
            foreach (var msg in chatHistory.Where(m => m.Role == AuthorRole.System))
            {
                augmentedChatHistory.Add(msg);
                Console.WriteLine($"Added system message to augmented history: {msg.Content!.Substring(0, Math.Min(30, msg.Content!.Length))}...");
            }
            
            // Step 2: Add the RAG context as a SYSTEM message
            var contextMessage = new ChatMessageContent(
                AuthorRole.System,
                @"Use the following information to help answer the user's question. 
                When you use information from these sources, include a citation with the source number in square brackets [X].
                If you directly quote text, follow it with a citation like: ""quote"" [X].
                If you paraphrase information, include the citation at the end of the sentence [X].
                Include a 'Sources:' section at the end with a list of all sources you cited.
                If the information doesn't help to answer the question, just respond based on your knowledge.
                
                Retrieved Information:
                " + retrievedContext
            );
            augmentedChatHistory.Add(contextMessage);
            Console.WriteLine("Added RAG context as system message");
            
            // Step 3: Add conversation history (limited by maxConversationTurns)
            var conversationMessages = chatHistory
                .Where(m => m.Role == AuthorRole.User || m.Role == AuthorRole.Assistant)
                .ToList();
                
            // Calculate how many messages to include (2 messages = 1 turn: user question + assistant response)
            int messagesToInclude = Math.Min(conversationMessages.Count, maxConversationTurns * 2);
            
            // Add the most recent conversation messages, starting from the appropriate index
            for (int i = conversationMessages.Count - messagesToInclude; i < conversationMessages.Count; i++)
            {
                augmentedChatHistory.Add(conversationMessages[i]);
            }
            
            // Get completion with the augmented context
            ChatMessageContent reply = await chatCompletionService.GetChatMessageContentAsync(
                augmentedChatHistory,
                new OpenAIPromptExecutionSettings { 
                    MaxTokens = 1000,
                    Temperature = 0.7
                },
                kernel: kernel
            );
            
            Console.WriteLine(reply.ToString());
            
            // Add the response to the main chat history
            chatHistory.AddAssistantMessage(reply.ToString());
        }
        catch (Exception ex) {
            Console.WriteLine($"Error getting reply: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            chatHistory.AddAssistantMessage("I'm sorry, I encountered an error processing your request.");
        }
    }

    // Replace the fixed conversation with an interactive loop:

    chatHistory = new ChatHistory();
    chatHistory.AddSystemMessage("You are a helpful Finnish immigration policy assistant. You can answer questions about Finland's immigration policies, integration programs, and requirements for immigrants.");
    chatHistory.AddSystemMessage("Always answer truthfully based on the provided information. If you're not sure about something, acknowledge that limitation.");

    AddMessage("Hello! I'm your Finnish immigration policy assistant. What would you like to know about Finland's immigration policies, requirements, or integration programs?");

    bool continueChat = true;
    while (continueChat)
    {
        try {
            GetInput();
            await GetReply();
            
            Console.WriteLine("\nWould you like to ask another question? (Type 'exit' to end the conversation)");
            string response = Console.ReadLine()?.Trim().ToLower() ?? "";
            if (response == "exit" || response == "quit" || response == "no")
            {
                continueChat = false;
                Console.WriteLine("\nThank you for using the Finnish immigration policy assistant. Goodbye!");
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error in chat loop: {ex.Message}");
            Console.WriteLine("Let's continue with a new question.");
        }
    }

    Console.WriteLine("Chat Ended.\n");
    Console.WriteLine("Chat History:");

    for (int i = 0; i < chatHistory.Count; i++)
    {
        Console.WriteLine($"{chatHistory[i].Role}: {chatHistory[i]}");
    }
}
catch (Exception ex) {
    Console.WriteLine($"Error loading configuration: {ex.Message}");
    throw;
}

