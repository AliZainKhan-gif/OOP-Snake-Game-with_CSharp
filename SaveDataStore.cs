using System;
using System.IO;
using System.Text.Json;

namespace SnakeGameWinForms;

public sealed class PlayerData
{
    public int HighScore { get; set; }
    public int LastScore { get; set; }
    public int TotalGames { get; set; }
    public DateTime LastPlayedUtc { get; set; }
}

public sealed class SaveDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SaveDataStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folder = Path.Combine(appData, "SnakeGameWinForms");
        Directory.CreateDirectory(folder);
        FilePath = Path.Combine(folder, "save-data.json");
    }

    public string FilePath { get; }

    public PlayerData Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new PlayerData();
            }

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<PlayerData>(json) ?? new PlayerData();
        }
        catch
        {
            return new PlayerData();
        }
    }

    public void Save(PlayerData data)
    {
        data.LastPlayedUtc = DateTime.UtcNow;
        string json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
