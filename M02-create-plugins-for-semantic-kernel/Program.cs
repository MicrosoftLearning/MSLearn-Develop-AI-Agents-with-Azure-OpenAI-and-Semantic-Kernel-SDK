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
        // Get the last user message
        string userQuery = chatHistory.Last(m => m.Role == AuthorRole.User).Content!;
        
        // Retrieve context from Azure AI Search
        string retrievedContext = await RetrieveAndAugmentAsync(userQuery);
        
        // Add a system message with the RAG context and citation instructions
        chatHistory.Insert(chatHistory.Count - 1, new ChatMessageContent(
            AuthorRole.System,
            @"Use the following information to help answer the user's question. 
            When you use information from these sources, include a citation with the source number in square brackets [X].
            If you directly quote text, follow it with a citation like: ""quote"" [X].
            If you paraphrase information, include the citation at the end of the sentence [X].
            Include a 'Sources:' section at the end with a list of all sources you cited.
            If the information doesn't help to answer the question, just respond based on your knowledge.
            
            Retrieved Information:
            " + retrievedContext
        ));
        
        // Get completion with the augmented context
        ChatMessageContent reply = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            kernel: kernel
        );
        
        Console.WriteLine(reply.ToString());
        chatHistory.AddAssistantMessage(reply.ToString());
        
        // Remove the RAG context message to keep the history clean for further interactions
        chatHistory.RemoveAt(chatHistory.Count - 3);
    }

    // Replace the fixed conversation with an interactive loop:

    chatHistory = new ChatHistory();
    chatHistory.AddSystemMessage("You are a helpful Finnish immigration policy assistant. You can answer questions about Finland's immigration policies, integration programs, and requirements for immigrants.");
    chatHistory.AddSystemMessage("Always answer truthfully based on the provided information. If you're not sure about something, acknowledge that limitation.");

    AddMessage("Hello! I'm your Finnish immigration policy assistant. What would you like to know about Finland's immigration policies, requirements, or integration programs?");

    bool continueChat = true;
    while (continueChat)
    {
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

// Create a kernel with Azure OpenAI chat completion
// var builder = Kernel.CreateBuilder();
// builder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// // Build the kernel
// Kernel kernel = builder.Build();

// string prompt = """
//     <message role="system">Instructions: Identify the from and to destinations 
//     and dates from the user's request</message>

//     <message role="user">Can you give me a list of flights from Seattle to Tokyo? 
//     I want to travel from March 11 to March 18.</message>

//     <message role="assistant">
//     Origin: Seattle
//     Destination: Tokyo
//     Depart: 03/11/2025 
//     Return: 03/18/2025
//     </message>

//     <message role="user">{{input}}</message>
//     """;
    
// string input = "I want to travel from June 1 to July 22. I want to go to Greece. I live in Chicago.";

// // Create the kernel arguments
// var arguments = new KernelArguments { ["input"] = input };

// // Create the prompt template config using handlebars format
// var templateFactory = new HandlebarsPromptTemplateFactory();
// var promptTemplateConfig = new PromptTemplateConfig()
// {
//     Template = prompt,
//     TemplateFormat = "handlebars",
//     Name = "FlightPrompt",
// };

// // Invoke the prompt function
// var function = kernel.CreateFunctionFromPrompt(promptTemplateConfig, templateFactory);
// var response = await kernel.InvokeAsync(function, arguments);
// Console.WriteLine(response);



// Create a kernel builder with Azure OpenAI chat completion
// var builder = Kernel.CreateBuilder();
// builder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// // Build the kernel
// var kernel = builder.Build();


// string prompt = """
//     You are a helpful travel guide. 
//     I'm visiting {{$city}}. {{$background}}. What are some activities I should do today?
//     """;
// string city = "Barcelona";
// string background = "I really enjoy art and dance.";

// // Create the kernel function from the prompt
// var activitiesFunction = kernel.CreateFunctionFromPrompt(prompt);

// // Create the kernel arguments
// var arguments = new KernelArguments { ["city"] = city, ["background"] = background };

// // InvokeAsync on the kernel object
// var result = await kernel.InvokeAsync(activitiesFunction, arguments);
// Console.WriteLine(result);

