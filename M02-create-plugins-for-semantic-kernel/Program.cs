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

string filePath = Path.GetFullPath("../../appsettings.json");
var config = new ConfigurationBuilder()
    .AddJsonFile(filePath)
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
var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// Build the kernel
Kernel kernel = builder.Build();

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
    
    // Add a system message with the RAG context (but don't show it to the user)
    chatHistory.Insert(chatHistory.Count - 1, new ChatMessageContent(
        AuthorRole.System,
        $"Use the following information to help answer the user's question, but don't explicitly mention that you're using this retrieved information: {retrievedContext}"
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

// Prompt the LLM
chatHistory.AddSystemMessage("You are a helpful travel assistant.");
chatHistory.AddSystemMessage("Recommend a destination to the traveler based on their background and preferences.");

// Get information about the user's plans
AddMessage("Tell me about your travel plans.");
GetInput();
await GetReply();

// Offer recommendations
AddMessage("Would you like some activity recommendations?");
GetInput();
await GetReply();

// Offer language tips
AddMessage("Would you like some helpful phrases in the local language?");
GetInput();
await GetReply();

Console.WriteLine("Chat Ended.\n");
Console.WriteLine("Chat History:");

for (int i = 0; i < chatHistory.Count; i++)
{
    Console.WriteLine($"{chatHistory[i].Role}: {chatHistory[i]}");
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

