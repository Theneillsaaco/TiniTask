using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using TiniTask.Models;
using TiniTask.Services;

namespace TiniTask;

internal record ImageEntry(string FullPath, string Name);

public partial class MainWindow : Window
{
    private ObservableCollection<TaskItem> _tasks = new();
    private readonly Dictionary<TaskItem, CancellationTokenSource> _tokens = new();
    private readonly ObservableCollection<ImageEntry> _selectedImages  = new();

    public MainWindow()
    {
        InitializeComponent();
        TaskList.ItemsSource = _tasks;
        SelectedImagesControl.ItemsSource = _selectedImages;
        LoadTasks();
    }

    private async void LoadTasks()
    {
        var tasks = await JsonService.LoadAsync();
        foreach (var task in tasks)
            _tasks.Add(task);
        TaskList.ItemsSource = _tasks;
    }

    private async Task SaveTasks() =>
        await JsonService.SaveAsync(_tasks.ToList());

    // ── File Picker ───────────────────────────────────────────────────────────

    private async void PickImage_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Agregar imágenes de plantilla",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imágenes") { Patterns = new[] { "*.png", "*.jpg", "*.bmp" } }
            }
        });

        foreach (var file in files)
        {
            var full = file.Path.LocalPath;
            // No agregar duplicados
            if (_selectedImages.Any(e => e.FullPath == full)) continue;
            _selectedImages.Add(new ImageEntry(full, Path.GetFileName(full)));
        }
    }

    private void RemoveImage_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string fullPath)
        {
            var entry = _selectedImages.FirstOrDefault(x => x.FullPath == fullPath);
            if (entry is not null) _selectedImages.Remove(entry);
        }
    }

    // ── Agregar tarea ─────────────────────────────────────────────────────────

    private async void AddTask_Click(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(IntervalInput.Text, out int interval))
            return;

        TaskItem task;

        if (ClickRadio.IsChecked == true)
        {
            if (!int.TryParse(XInput.Text, out int x) || !int.TryParse(YInput.Text, out int y))
                return;
            task = new TaskItem { Type = TaskType.Click, X = x, Y = y, Interval = interval };
        }
        else if (DetectRadio.IsChecked == true)
        {
            if (_selectedImages.Count == 0) return;
            task = new TaskItem
            {
                Type       = TaskType.Detect,
                ImagePaths = _selectedImages.Select(e => e.FullPath).ToList(),
                Interval   = interval
            };
        }
        else
        {
            if (string.IsNullOrWhiteSpace(TextInputBox.Text)) return;
            task = new TaskItem { Type = TaskType.Type, Text = TextInputBox.Text, Interval = interval };
        }

        _tasks.Add(task);
        await SaveTasks();

        TextInputBox.Text = "";
        XInput.Text = "";
        YInput.Text = "";
        IntervalInput.Text = "";
        _selectedImages.Clear();
    }

    // ── Iniciar / Detener / Eliminar ──────────────────────────────────────────

    private void StartTask_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not TaskItem task) return;
        if (_tokens.ContainsKey(task)) return;

        var cts = new CancellationTokenSource();
        _tokens[task] = cts;
        task.IsRunning = true;

        StatusText.Text = task.Type switch
        {
            TaskType.Click => $"Ejecutando click en ({task.X}, {task.Y})",
            TaskType.Detect => $"Detectando {task.ImagePaths.Count} template(s)...",
            _ => $"Ejecutando: {task.Text}"
        };

        _ = RunTaskAsync(task, cts.Token);
    }

    private async Task RunTaskAsync(TaskItem task, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                switch (task.Type)
                {
                    case TaskType.Click: await DoClickAsync(task.X, task.Y); break;
                    case TaskType.Detect: await DoDetectAndClickAsync(task.ImagePaths); break;
                    default: await TypeTextAsync(task.Text); break;
                }
                await Task.Delay(task.Interval * 1000, token);
            }
        }
        catch (TaskCanceledException) { }
    }

    private void StopTask_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not TaskItem task) return;
        if (_tokens.TryGetValue(task, out var cts))
        {
            cts.Cancel();
            _tokens.Remove(task);
            task.IsRunning = false;
        }
        StatusText.Text = "Estado: Listo";
    }

    private async void DeleteTask_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not TaskItem task) return;
        StopTask_Click(sender, e);
        _tasks.Remove(task);
        await SaveTasks();
    }

    // ── Acciones ──────────────────────────────────────────────────────────────

    private async Task TypeTextAsync(string text)
    {
        await Task.Delay(500);
        try
        {
            StatusText.Text = $"Escribiendo: {text}";
            await KeyboardService.TypeAsync(text);
            StatusText.Text = "Estado: Listo";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async Task DoClickAsync(int x, int y)
    {
        await Task.Delay(300);
        try
        {
            StatusText.Text = $"Clickeando en ({x}, {y})";
            await MouseService.ClickAtAsync(x, y);
            StatusText.Text = "Estado: Listo";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async Task DoDetectAndClickAsync(List<string> imagePaths)
    {
        try
        {
            StatusText.Text = $"Buscando ({imagePaths.Count} template(s))...";
            var match = await ImageDetectionService.FindOnScreenAsync(imagePaths);

            if (match.HasValue)
            {
                StatusText.Text = $"Encontrado en ({match.Value.cx}, {match.Value.cy}) – clickeando";
                await MouseService.ClickAtAsync(match.Value.cx, match.Value.cy);
                StatusText.Text = "Estado: Listo";
            }
            else
            {
                StatusText.Text = "No encontrado en pantalla";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error detección: {ex.Message}";
        }
    }
}
