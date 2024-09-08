using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using System.IO;

using Microsoft.SemanticKernel;

public class TodoListPlugin
{
    [KernelFunction, Description("할 일 목록 항목을 완료로 표시")]
    public static string CompleteTask([Description("완료해야 할 작업")] string task)
    {
        // Read the JSON file
        string jsonFilePath = $"{Directory.GetCurrentDirectory()}/Plugins/todo.txt";
        string jsonContent = File.ReadAllText(jsonFilePath);

        // Parse the JSON content
        JsonNode todoData = JsonNode.Parse(jsonContent);

        // Find the task and mark it as complete
        JsonArray todoList = (JsonArray)todoData["todoList"];
        foreach (JsonNode taskNode in todoList)
        {
            if (taskNode["task"].ToString() == task)
            {
                taskNode["completed"] = true;
                break;
            }
        }

        // 한글을 유니코드로 저장하지 않도록 설정
        var settings = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true
        };

        // Save the modified JSON back to the file
        File.WriteAllText(jsonFilePath, JsonSerializer.Serialize(todoData, settings));
        return $"할 일 '{task}'를 완료 처리하였습니다.";
    }
}