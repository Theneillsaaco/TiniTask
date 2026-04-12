using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TiniTask.Models;

namespace TiniTask.Services;

public static class JsonService
{
    private static readonly string FilePath = "tasks.json";

    public static async Task<List<TaskItem>> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return new List<TaskItem>();
        
        var json = await File.ReadAllTextAsync(FilePath);
        return JsonSerializer.Deserialize<List<TaskItem>>(json)
            ?? new List<TaskItem>();
    }

    public static async Task SaveAsync(List<TaskItem> tasks)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        
        var json = JsonSerializer.Serialize(tasks, options);
        await File.WriteAllTextAsync(FilePath, json);
    }
}