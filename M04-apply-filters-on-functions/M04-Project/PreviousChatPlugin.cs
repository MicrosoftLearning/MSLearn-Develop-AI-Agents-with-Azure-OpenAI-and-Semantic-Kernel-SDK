using System.ComponentModel;
using Microsoft.SemanticKernel;

public class PreviousChatPlugin
{
    private const string FilePath = "chatHistory.html";

    [KernelFunction("get_previous_conversation")]
    [Description("Gets the previous conversation between the user and assistant")]
    public string GetPreviousConversation()
    {
        return File.ReadAllText(FilePath);
    }
}
