namespace SecurityRule.Web.Services;

public class ThemeState
{
    public bool IsDarkMode { get; private set; } = false;

    public event Action? OnChange;

    public void Toggle()
    {
        IsDarkMode = !IsDarkMode;
        OnChange?.Invoke();
    }

    public void SetDarkMode(bool value)
    {
        IsDarkMode = value;
        OnChange?.Invoke();
    }
}
