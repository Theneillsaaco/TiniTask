using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace TiniTask.Models;

public class TaskItem : INotifyPropertyChanged
{
    private bool _isRunning;
    
    public string Text { get; set; } = "";

    public int Interval { get; set; } = 10;
    
    public TaskType Type { get; set; } = TaskType.Type;
    
    public int X { get; set; }
    
    public int Y { get; set; }

    public List<string> ImagePaths { get; set; } = new();

    public string DisplayText => Type switch
    {
        TaskType.Click => $"Click en ({X}, {Y})",
        TaskType.Detect => ImagePaths.Count switch
        {
            0 => "Detectar: (sin imágenes)",
            1 => $"Detectar: {Path.GetFileName(ImagePaths[0])}",
            _ => $"Detectar: {Path.GetFileName(ImagePaths[0])} +{ImagePaths.Count - 1} más"
        },
        _ => Text
    };
    
    public string StatusDisplay => IsRunning ? "● Activo" : "○ Inactivo";

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            _isRunning = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusDisplay)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}