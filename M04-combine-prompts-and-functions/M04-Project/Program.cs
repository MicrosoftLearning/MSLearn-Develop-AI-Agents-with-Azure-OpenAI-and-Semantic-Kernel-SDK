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

kernel.ImportPluginFromType<MusicLibraryPlugin>();

var result = await kernel.InvokeAsync(
    "MusicLibraryPlugin", 
    "AddToRecentlyPlayed", 
    new() {
        ["artist"] = "Tiara", 
        ["song"] = "Danse", 
        ["genre"] = "French pop, electropop, pop"
    }
);

Console.WriteLine(result);