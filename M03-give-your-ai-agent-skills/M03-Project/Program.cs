using Microsoft.SemanticKernel;

string yourDeploymentName = "";
string yourEndpoint = "";
string yourKey = "";

var builder = Kernel.CreateBuilder();
builder.Services.AddAzureOpenAIChatCompletion(
    yourDeploymentName,
    yourEndpoint,
    yourKey,
    "gpt-35-turbo-16k");

var kernel = builder.Build();
var result = await kernel.InvokePromptAsync(
    "Give me a list of breakfast foods with eggs and cheese");
    
Console.WriteLine(result);