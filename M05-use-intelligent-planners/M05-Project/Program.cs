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

string prompt = @"This is a list of music available to the user:
    {{MusicLibraryPlugin.GetMusicLibrary}} 

    This is a list of music the user has recently played:
    {{MusicLibraryPlugin.GetRecentPlays}}

    Based on their recently played music, suggest a song from
    the list to play next";

var result = await kernel.InvokePromptAsync(prompt);
Console.WriteLine(result);