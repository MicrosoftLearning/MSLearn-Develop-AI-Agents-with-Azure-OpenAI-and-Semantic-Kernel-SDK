using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.SemanticKernel;

public class MusicLibraryPlugin
{
    [KernelFunction, Description("사용자가 최근에 재생한 음악 목록 가져오기")]
    public static string GetRecentPlays()
    {
        string dir = Directory.GetCurrentDirectory();
        string content = File.ReadAllText($"{dir}/data/recentlyplayed.txt");

        return content;
    }

    [KernelFunction, Description("최근 재생 목록에 노래 추가하기")]
    public static string AddToRecentlyPlayed(
    [Description("아티스트의 이름")] string artist,
    [Description("노래 제목")] string song,
    [Description("노래 장르")] string genre)
    {
        // Read the existing content from the file
        string filePath = "data/recentlyplayed.txt";
        string jsonContent = File.ReadAllText(filePath);
        var recentlyPlayed = (JsonArray)JsonNode.Parse(jsonContent);

        var newSong = new JsonObject
        {
            ["title"] = song,
            ["artist"] = artist,
            ["genre"] = genre
        };

        // 한글을 유니코드로 저장하지 않도록 설정
        var settings = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true
        };

        recentlyPlayed.Insert(0, newSong);
        File.WriteAllText(filePath, JsonSerializer.Serialize(recentlyPlayed, settings));

        return $"최근 재생한 노래에 '{song}' 추가'";
    }
}