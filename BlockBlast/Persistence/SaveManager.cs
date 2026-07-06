using System;
using System.IO;
using System.Text.Json;

namespace BlockBlast.Persistence;

public static class SaveManager
{
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BlockBlast",
        "save.json");

    public static int LoadBestScore()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<SaveData>(json);
                return data?.BestScore ?? 0;
            }
        }
        catch (Exception)
        {
        }
        return 0;
    }

    public static void SaveBestScore(int bestScore)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            var data = new SaveData { BestScore = bestScore };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data));
        }
        catch (Exception)
        {
        }
    }
}
