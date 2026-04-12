using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiniTask.Models;
using TiniTask.Services;

namespace TiniTask;

public partial class MainWindow : Window
{
    private ObservableCollection<TaskItem> _tasks = new();
    private readonly Dictionary<TaskItem, CancellationTokenSource> _tokens = new();

    public MainWindow()
    {
        InitializeComponent();
        TaskList.ItemsSource = _tasks;
        LoadTasks();
    }

    private async void LoadTasks()
    {
        var tasks = await JsonService.LoadAsync();
        foreach (var task in tasks)
            _tasks.Add(task);

        TaskList.ItemsSource = _tasks;
    }

    private async Task SaveTasks()
    {
        await JsonService.SaveAsync(_tasks.ToList());
    }

    private async void AddTask_Click(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(IntervalInput.Text, out int interval))
            return;

        var task = new TaskItem
        {
            Text = TextInput.Text ?? "",
            Interval = interval
        };

        _tasks.Add(task);
        await SaveTasks();
    }

    private void StartTask_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not TaskItem task)
            return;

        if (_tokens.ContainsKey(task))
            return;

        var cts = new CancellationTokenSource();
        _tokens[task] = cts;
        task.IsRunning = true;

        RunTaskAsync(task, cts.Token);
    }

    private async void RunTaskAsync(TaskItem task, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await TypeTextAsync(task.Text);
                await Task.Delay(task.Interval * 1000, token);
            }
        }
        catch (TaskCanceledException) { }
    }

    private void StopTask_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not TaskItem task)
            return;

        if (_tokens.TryGetValue(task, out var cts))
        {
            cts.Cancel();
            _tokens.Remove(task);
            task.IsRunning = false;
        }
    }

    private async void DeleteTask_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not TaskItem task)
            return;

        StopTask_Click(sender, e);
        _tasks.Remove(task);
        await SaveTasks();
    }

    // Simulación de escritura
    private Task TypeTextAsync(string text)
    {
        Console.WriteLine(text);
        return Task.CompletedTask;
    }
}