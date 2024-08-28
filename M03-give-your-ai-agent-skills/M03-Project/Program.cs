using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .Build();

// Set your values in appsettings.json or user secrets
string yourDeploymentName = config.GetRequiredSection("yourDeploymentName").Value!;
string yourEndpoint = config.GetRequiredSection("yourEndpoint").Value!;
string yourApiKey = config.GetRequiredSection("yourApiKey").Value!;

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    yourDeploymentName,
    yourEndpoint,
    yourApiKey,
    "gpt-35-turbo-16k");

var kernel = builder.Build();
var result = await kernel.InvokePromptAsync(
    "Give me a list of breakfast foods with eggs and cheese");
    
Console.WriteLine(result);