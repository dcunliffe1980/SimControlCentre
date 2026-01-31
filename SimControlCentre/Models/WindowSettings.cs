namespace SimControlCentre.Models;

/// <summary>
/// Window size and position settings
/// </summary>
public class WindowSettings
{
    public double Width { get; set; } = 900;
    public double Height { get; set; } = 700;
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public bool StartMinimized { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
}

