using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

string yourDeploymentName = "";
string yourEndpoint = "";
string yourApiKey = "";

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

