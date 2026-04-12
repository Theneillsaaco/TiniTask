namespace TiniTask.Models;

public class TaskItem
{
    public string Text { get; set; } = "";

    public int Interval { get; set; } = 10;
    
    public bool IsRunning { get; set; } = false;
}