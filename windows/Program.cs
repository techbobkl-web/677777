// Roblox Farm — a harmless prank for Windows.
//
// Pops up Roblox-launcher-looking windows all over the screen and fills them with "677777" as if
// someone were pasting it over and over, until you press Shift+1 (works even when another
// window is in front). Task Manager can always end it too. The windows are drawn by this app:
// it doesn't type into other programs, touch the clipboard, start with Windows, or change any
// files, and it never has more than 150 windows open.
//
// Usage: RobloxFarm.exe   (--seconds N makes it stop by itself, handy for testing)

using System.Runtime.InteropServices;

namespace RobloxFarm;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        TimeSpan? stopAfter = null;
        int flag = Array.FindIndex(args, a => a.Equals("--seconds", StringComparison.OrdinalIgnoreCase));
        if (flag >= 0 && flag + 1 < args.Length && double.TryParse(args[flag + 1], out double seconds))
        {
            stopAfter = TimeSpan.FromSeconds(Math.Max(1, seconds));
        }

        Application.Run(new PrankContext(stopAfter));
    }
}

/// <summary>Opens the windows, keeps "pasting" into them, and stops on Shift+1.</summary>
internal sealed class PrankContext : ApplicationContext
{
    private const int MaxWindows = 150;

    private readonly List<RobloxLookalike> _windows = new();
    private readonly System.Windows.Forms.Timer _spawn = new() { Interval = 150 };
    private readonly System.Windows.Forms.Timer _paste = new() { Interval = 60 };
    private readonly System.Windows.Forms.Timer? _stop;
    private readonly StopHotKey _hotKey;
    private readonly Icon? _robloxIcon = LoadRobloxIcon();

    public PrankContext(TimeSpan? stopAfter)
    {
        _hotKey = new StopHotKey(ExitThread);

        _spawn.Tick += (_, _) => OpenWindow();
        _paste.Tick += (_, _) =>
        {
            foreach (var window in _windows)
            {
                window.Paste();
            }
        };
        _spawn.Start();
        _paste.Start();

        if (stopAfter is { } after)
        {
            _stop = new System.Windows.Forms.Timer { Interval = (int)Math.Min(after.TotalMilliseconds, int.MaxValue) };
            _stop.Tick += (_, _) => ExitThread();
            _stop.Start();
        }

        OpenWindow();
    }

    protected override void ExitThreadCore()
    {
        _spawn.Stop();
        _paste.Stop();
        _stop?.Stop();
        _hotKey.Dispose();
        foreach (var window in _windows.ToList())
        {
            window.Close();
        }

        base.ExitThreadCore();
    }

    private void OpenWindow()
    {
        if (_windows.Count >= MaxWindows)
        {
            _windows[0].Close(); // FormClosed removes it from the list
        }

        var window = new RobloxLookalike(_robloxIcon, ExitThread);
        window.FormClosed += (_, _) => _windows.Remove(window);
        _windows.Add(window);
        window.Show();
    }

    private static Icon? LoadRobloxIcon()
    {
        // Use the real Roblox Player icon if it's installed, so the windows look the part; else none.
        try
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string versions = Path.Combine(local, "Roblox", "Versions");
            if (Directory.Exists(versions))
            {
                foreach (string exe in Directory.EnumerateFiles(versions, "RobloxPlayerBeta.exe", SearchOption.AllDirectories))
                {
                    return Icon.ExtractAssociatedIcon(exe);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
        }

        return null;
    }
}

/// <summary>Registers Shift+1 as a system-wide hot key (RegisterHotKey: no hooks, no admin rights).</summary>
internal sealed class StopHotKey : NativeWindow, IDisposable
{
    private const int WmHotKey = 0x0312;
    private const uint ModShift = 0x0004;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkOne = 0x31;
    private const int Id = 0x6777;

    private readonly Action _onPressed;
    private bool _registered;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public StopHotKey(Action onPressed)
    {
        _onPressed = onPressed;
        CreateHandle(new CreateParams());
        // If another program already owns Shift+1, the windows still catch it themselves.
        _registered = RegisterHotKey(Handle, Id, ModShift | ModNoRepeat, VkOne);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotKey && m.WParam == (IntPtr)Id)
        {
            _onPressed();
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_registered)
        {
            UnregisterHotKey(Handle, Id);
            _registered = false;
        }

        DestroyHandle();
    }
}

/// <summary>A window drawn to look like the "Roblox is starting" launcher, with text that keeps filling up.</summary>
internal sealed class RobloxLookalike : Form
{
    private const string SpamText = "677777 ";
    private static readonly Color PageColor = Color.FromArgb(20, 23, 29);
    private static readonly Color TextBgColor = Color.FromArgb(14, 16, 20);

    private readonly TextBox _text;

    public RobloxLookalike(Icon? icon, Action stop)
    {
        var area = Screen.AllScreens[Random.Shared.Next(Screen.AllScreens.Length)].WorkingArea;
        var size = new Size(Random.Shared.Next(380, 600), Random.Shared.Next(220, 380));

        Text = "Roblox";
        if (icon != null)
        {
            Icon = icon;
        }

        StartPosition = FormStartPosition.Manual;
        Size = size;
        Location = new Point(
            area.Left + Random.Shared.Next(Math.Max(1, area.Width - size.Width)),
            area.Top + Random.Shared.Next(Math.Max(1, area.Height - size.Height)));
        TopMost = true;
        BackColor = PageColor;
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.Shift && e.KeyCode == Keys.D1)
            {
                stop();
            }

            e.SuppressKeyPress = true;
        };

        var heading = new Label
        {
            Text = "⬤  Starting Roblox Farm…",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = Color.White,
            BackColor = PageColor,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
        };

        _text = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BackColor = TextBgColor,
            ForeColor = RandomBrightColor(),
            BorderStyle = BorderStyle.None,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", Random.Shared.Next(12, 22), FontStyle.Bold),
        };

        // Fill first, then the heading docked above it.
        Controls.Add(_text);
        Controls.Add(heading);
    }

    public void Paste()
    {
        if (_text.TextLength > 8000)
        {
            _text.Text = _text.Text[4000..];
        }

        _text.AppendText(string.Concat(Enumerable.Repeat(SpamText, Random.Shared.Next(1, 4))));
    }

    private static Color RandomBrightColor()
    {
        return Color.FromArgb(
            Random.Shared.Next(120, 256),
            Random.Shared.Next(120, 256),
            Random.Shared.Next(120, 256));
    }
}
