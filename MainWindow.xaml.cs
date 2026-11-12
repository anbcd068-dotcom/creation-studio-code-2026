using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;

namespace CreationStudio2026;

class DocTab
{
    public TabItem Item = new();
    public TextEditor Editor = new();
    public TextBlock Title = new() { VerticalAlignment = VerticalAlignment.Center };
    public string Name = "untitled";
    public string? Path;
    public bool Dirty;
    public LanguageDef Lang = null!;
}

public partial class MainWindow : Window
{
    readonly ProcessRunner _runner = new();
    readonly List<DocTab> _tabs = new();
    bool _suppressLang;
    int _untitled = 1;
    string _terminalWorkingDirectory = Environment.CurrentDirectory;
    static readonly UTF8Encoding Utf8 = new(false);

    public MainWindow()
    {
        InitializeComponent();
        LanguageRegistry.Load();
        RefreshLanguageUi();
        var first = LanguageRegistry.All.FirstOrDefault(l => l.Installed && l.Run != "") ?? LanguageRegistry.Plain;
        AddTab(null, first.Template, first);
    }

    DocTab? Current => _tabs.FirstOrDefault(t => ReferenceEquals(t.Item, Tabs.SelectedItem));

    static Brush B(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;

    // ---------- language UI ----------
    void RefreshLanguageUi()
    {
        _suppressLang = true;
        LangBox.ItemsSource = null;
        LangBox.ItemsSource = LanguageRegistry.All;
        _suppressLang = false;
        var ok = LanguageRegistry.All.Count(l => l.Installed && l.Run != "");
        LangStatus.Text = $"{ok} runnable languages detected";
        SyncCombo();
    }

    void SyncCombo()
    {
        _suppressLang = true;
        LangBox.SelectedItem = Current?.Lang;
        _suppressLang = false;
    }

    void Lang_Changed(object s, SelectionChangedEventArgs e)
    {
        if (_suppressLang || Current == null || LangBox.SelectedItem is not LanguageDef l) return;
        Current.Lang = l;
        Current.Editor.SyntaxHighlighting = Highlighting.Get(l);
    }

    void Rescan_Click(object s, RoutedEventArgs e)
    {
        var langs = _tabs.ToDictionary(t => t, t => t.Lang.Name);
        LanguageRegistry.Load();
        foreach (var t in _tabs)
            t.Lang = LanguageRegistry.All.FirstOrDefault(l =>
                l.Name.Equals(langs[t], StringComparison.OrdinalIgnoreCase))
                ?? LanguageRegistry.Plain;
        foreach (var t in _tabs)
            t.Editor.SyntaxHighlighting = Highlighting.Get(t.Lang);
        RefreshLanguageUi();
        StatusText.Text = "Re-scanned PATH";
    }

    void ShowDetected_Click(object s, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        foreach (var l in LanguageRegistry.All.Where(l => l.Installed && l.Run != ""))
            sb.AppendLine($"✔ {l.Name}   ({l.ResolvedExe})");
        sb.AppendLine();
        foreach (var l in LanguageRegistry.All.Where(l => !l.Installed))
            sb.AppendLine($"✘ {l.Name}   (needs: {string.Join(" / ", l.ExeList)})");
        MessageBox.Show(sb.ToString(), "Detected languages");
    }

    void OpenCustom_Click(object s, RoutedEventArgs e)
    {
        var f = LanguageRegistry.UserFile;
        Directory.CreateDirectory(Path.GetDirectoryName(f)!);
        if (!File.Exists(f))
            File.WriteAllText(f, """
[
  {
    "Name": "My Language",
    "Extensions": ".mylang",
    "Exes": "mylangc",
    "Run": "{exe} \"{file}\"",
    "Comment": "//",
    "Keywords": "if else while",
    "Template": "print(\"hi\")"
  }
]
""");
        Process.Start(new ProcessStartInfo(f) { UseShellExecute = true });
    }

    void About_Click(object s, RoutedEventArgs e) =>
        MessageBox.Show("Creation Studio 2026\nA multi-language code editor & runner.\n\n" +
            "Any language installed on your PC (on PATH) is detected and can be run with F5.\n" +
            "Add more via Languages > Open custom languages file.", "About");

    // ---------- tabs ----------
    DocTab AddTab(string? path, string text, LanguageDef lang)
    {
        var t = new DocTab { Path = path, Lang = lang };
        t.Name = path != null ? Path.GetFileName(path) : $"untitled-{_untitled++}{lang.ExtList.FirstOrDefault() ?? ".txt"}";

        t.Editor = new TextEditor
        {
            FontFamily = new FontFamily("Consolas"), FontSize = 14, ShowLineNumbers = true,
            Background = B("#1E1E1E"), Foreground = B("#D4D4D4"), LineNumbersForeground = B("#858585"),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Options = { ConvertTabsToSpaces = lang.Name != "Go", IndentationSize = 4, HighlightCurrentLine = true },
            Text = text
        };
        t.Editor.SyntaxHighlighting = Highlighting.Get(lang);
        t.Editor.TextChanged += (_, _) => { if (!t.Dirty) { t.Dirty = true; UpdateTitle(t); } };

        var close = new Button { Content = "✕", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(3, 0, 3, 0),
            Background = Brushes.Transparent, Foreground = B("#999999"), BorderThickness = new Thickness(0) };
        close.Click += (_, _) => CloseTab(t);
        UpdateTitle(t);
        t.Item.Header = new StackPanel { Orientation = Orientation.Horizontal, Children = { t.Title, close } };
        t.Item.Content = t.Editor;

        _tabs.Add(t);
        Tabs.Items.Add(t.Item);
        Tabs.SelectedItem = t.Item;
        t.Dirty = false; UpdateTitle(t);
        t.Editor.Focus();
        return t;
    }

    void UpdateTitle(DocTab t) => t.Title.Text = t.Name + (t.Dirty ? " •" : "");

    void Tabs_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource != Tabs) return;
        SyncCombo();
        StatusText.Text = Current?.Path ?? "Unsaved file";
    }

    bool CloseTab(DocTab t)
    {
        if (t.Dirty)
        {
            var r = MessageBox.Show($"Save changes to {t.Name}?", "Creation Studio 2026", MessageBoxButton.YesNoCancel);
            if (r == MessageBoxResult.Cancel) return false;
            if (r == MessageBoxResult.Yes && !Save(t)) return false;
        }
        Tabs.Items.Remove(t.Item);
        _tabs.Remove(t);
        return true;
    }

    // ---------- file ops ----------
    void New_Click(object s, RoutedEventArgs e)
    {
        var l = Current?.Lang ?? LanguageRegistry.Plain;
        AddTab(null, l.Template, l);
    }

    void Open_Click(object s, RoutedEventArgs e)
    {
        var exts = string.Join(";", LanguageRegistry.All.SelectMany(l => l.ExtList).Distinct().Select(x => "*" + x));
        var d = new OpenFileDialog { Multiselect = true, Filter = $"Code files|{exts}|All files|*.*" };
        if (d.ShowDialog() == true) foreach (var f in d.FileNames) OpenPath(f);
    }

    void OpenPath(string path)
    {
        var existing = _tabs.FirstOrDefault(t => t.Path == path);
        if (existing != null) { Tabs.SelectedItem = existing.Item; return; }
        try
        {
            AddTab(path, File.ReadAllText(path), LanguageRegistry.ForFile(path));
            _terminalWorkingDirectory = Path.GetDirectoryName(path) ?? _terminalWorkingDirectory;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Cannot open file"); }
    }

    void Save_Click(object s, RoutedEventArgs e) { if (Current != null) Save(Current); }
    void SaveAs_Click(object s, RoutedEventArgs e) { if (Current != null) Save(Current, true); }

    bool Save(DocTab t, bool saveAs = false)
    {
        if (t.Path == null || saveAs)
        {
            var ext = t.Lang.ExtList.FirstOrDefault() ?? ".txt";
            var d = new SaveFileDialog { FileName = t.Name, DefaultExt = ext, Filter = $"{t.Lang.Name}|*{ext}|All files|*.*" };
            if (d.ShowDialog() != true) return false;
            t.Path = d.FileName; t.Name = Path.GetFileName(d.FileName);
        }
        File.WriteAllText(t.Path, t.Editor.Text, Utf8);
        _terminalWorkingDirectory = Path.GetDirectoryName(t.Path) ?? _terminalWorkingDirectory;
        t.Dirty = false; UpdateTitle(t);
        StatusText.Text = "Saved " + t.Path;
        return true;
    }

    void Exit_Click(object s, RoutedEventArgs e) => Close();

    void Window_Closing(object? s, System.ComponentModel.CancelEventArgs e)
    {
        foreach (var t in _tabs.ToList())
            if (!CloseTab(t)) { e.Cancel = true; return; }
        _runner.Stop();
    }

    // ---------- explorer ----------
    void OpenFolder_Click(object s, RoutedEventArgs e)
    {
        var d = new OpenFolderDialog();
        if (d.ShowDialog() != true) return;
        _terminalWorkingDirectory = d.FolderName;
        Explorer.Items.Clear();
        var root = Node(d.FolderName, true);
        Explorer.Items.Add(root);
        root.IsExpanded = true;
    }

    TreeViewItem Node(string path, bool dir)
    {
        var name = Path.GetFileName(path) is { Length: > 0 } nm ? nm : path;
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var color = dir ? "#D7BA7D" : extension switch
        {
            ".cs" => "#4FC1FF", ".xaml" => "#C586C0", ".xml" or ".html" or ".htm" => "#E8A05A",
            ".css" => "#569CD6", ".js" or ".ts" => "#DCDCAA", ".json" => "#CE9178",
            ".py" => "#6A9955", ".md" or ".txt" => "#C8C8C8", ".png" or ".jpg" or ".ico" => "#C586C0",
            ".dll" or ".exe" => "#D16969", _ => "#9CDCFE"
        };
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(new TextBlock { Text = dir ? "◆ " : "◇ ", Foreground = B(dir ? "#D7BA7D" : "#858585") });
        header.Children.Add(new TextBlock { Text = name, Foreground = B(color) });
        var n = new TreeViewItem { Header = header, Tag = path, Foreground = B(color) };
        if (dir) { n.Items.Add("…"); n.Expanded += Node_Expanded; }
        return n;
    }

    void Node_Expanded(object sender, RoutedEventArgs e)
    {
        var n = (TreeViewItem)sender;
        if (n.Items.Count != 1 || n.Items[0] is not string) return;
        n.Items.Clear();
        try
        {
            foreach (var d in Directory.GetDirectories((string)n.Tag).OrderBy(x => x)) n.Items.Add(Node(d, true));
            foreach (var f in Directory.GetFiles((string)n.Tag).OrderBy(x => x)) n.Items.Add(Node(f, false));
        }
        catch { }
        e.Handled = true;
    }

    void Explorer_DoubleClick(object s, MouseButtonEventArgs e)
    {
        if (Explorer.SelectedItem is TreeViewItem { Tag: string p } && File.Exists(p)) OpenPath(p);
    }

    // ---------- run ----------
    void Log(string text) => Dispatcher.BeginInvoke(() => { Output.AppendText(text); Output.ScrollToEnd(); });
    void ClearOutput_Click(object s, RoutedEventArgs e) => Output.Clear();

    string? EnsureFile(DocTab t)
    {
        if (t.Path != null)
        {
            if (t.Dirty && !Save(t)) return null;
            return t.Path;
        }
        var dir = Path.Combine(Path.GetTempPath(), "CreationStudio2026", "scratch");
        Directory.CreateDirectory(dir);
        var f = Path.Combine(dir, "main" + (t.Lang.ExtList.FirstOrDefault() ?? ".txt"));
        File.WriteAllText(f, t.Editor.Text, Utf8);
        return f;
    }

    static string Expand(LanguageDef l, string file)
    {
        var name = Path.GetFileNameWithoutExtension(file);
        var outDir = Path.Combine(Path.GetTempPath(), "CreationStudio2026", "bin");
        Directory.CreateDirectory(outDir);
        return l.Run.Replace("{exe}", l.ResolvedExe)
                    .Replace("{file}", file)
                    .Replace("{dir}", Path.GetDirectoryName(file))
                    .Replace("{name}", name)
                    .Replace("{out}", Path.Combine(outDir, name));
    }

    async void Run_Click(object s, RoutedEventArgs e)
    {
        var t = Current; if (t == null || _runner.IsRunning) return;
        var l = t.Lang;
        Output.Clear();
        if (string.IsNullOrEmpty(l.Run)) { Log($"{l.Name} has no run command.\n"); return; }
        if (!l.Installed)
        {
            Log($"{l.Name} isn't installed (looked for: {string.Join(", ", l.ExeList)}).\nInstall it, then use Languages > Re-scan.\n");
            return;
        }
        var file = EnsureFile(t); if (file == null) return;
        string cmd;
        if (l.Name.Equals("C#", StringComparison.OrdinalIgnoreCase) &&
            Path.GetExtension(file).Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            // A plain .cs file is not directly executable by the .NET SDK.
            // Wrap it in a temporary SDK project so F5 works without dotnet-script.
            var projectDir = Path.Combine(Path.GetTempPath(), "CreationStudio2026", "CSharp", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(projectDir);
            File.WriteAllText(Path.Combine(projectDir, "Program.cs"), t.Editor.Text, Utf8);
            File.WriteAllText(Path.Combine(projectDir, "CreationStudio.Run.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>", Utf8);
            cmd = $"dotnet run --project \"{Path.Combine(projectDir, "CreationStudio.Run.csproj")}\" --configuration Release --no-launch-profile";
        }
        else
        {
            cmd = Expand(l, file);
        }
        Log($"> {cmd}\n\n");
        RunBtn.IsEnabled = false; StopBtn.IsEnabled = true;
        StatusText.Text = "Running...";
        var sw = Stopwatch.StartNew();
        try
        {
            var workingDirectory = Path.GetDirectoryName(file);
            if (string.IsNullOrWhiteSpace(workingDirectory))
                workingDirectory = Environment.CurrentDirectory;

            var code = await _runner.RunAsync(cmd, workingDirectory, Log);
            Log($"\\n[Exited with code {code} in {sw.ElapsedMilliseconds} ms]\\n");
        }
        catch (Exception ex)
        {
            Log("Error: " + ex.Message + "\\n");
        }
        finally
        {
            RunBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
            StatusText.Text = "Ready";
        }
    }

    void Stop_Click(object s, RoutedEventArgs e) { _runner.Stop(); Log("\n[Stopped]\n"); }

    async void Input_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        var command = Input.Text.Trim();
        if (command.Length == 0) return;
        Input.Clear();
        Log($"terminal › {command}\n");

        // If F5 is currently running a program, pass text to its standard input.
        if (_runner.IsRunning)
        {
            _runner.SendInput(command);
            return;
        }

        // Terminal commands use a remembered working directory, just like a normal shell.
        if (command.Equals("cls", StringComparison.OrdinalIgnoreCase) || command.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            Output.Clear();
            return;
        }
        if (command.Equals("cd", StringComparison.OrdinalIgnoreCase))
        {
            Log(_terminalWorkingDirectory + Environment.NewLine);
            return;
        }
        if (command.StartsWith("cd ", StringComparison.OrdinalIgnoreCase) || command.StartsWith("chdir ", StringComparison.OrdinalIgnoreCase))
        {
            var target = command[(command.IndexOf(' ') + 1)..].Trim().Trim('"');
            if (target.Length == 0) target = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            try
            {
                var next = Path.GetFullPath(Path.IsPathRooted(target) ? target : Path.Combine(_terminalWorkingDirectory, target));
                if (Directory.Exists(next)) { _terminalWorkingDirectory = next; Log(_terminalWorkingDirectory + Environment.NewLine); }
                else Log("The system cannot find the path specified." + Environment.NewLine);
            }
            catch (Exception ex) { Log("cd: " + ex.Message + Environment.NewLine); }
            return;
        }
        try
        {
            StatusText.Text = "Terminal command running...";
            var exitCode = await _runner.RunAsync(command, _terminalWorkingDirectory, Log);
            Log($"\n[Terminal command exited with code {exitCode}]\n");
        }
        catch (Exception ex)
        {
            Log($"Terminal error: {ex.Message}\n");
        }
        finally
        {
            StatusText.Text = "Ready";
        }
    }

    void Window_PreviewKeyDown(object s, KeyEventArgs e)
    {
        var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        if (e.Key == Key.F5 && shift) { Stop_Click(s, e); e.Handled = true; }
        else if (e.Key == Key.F5) { Run_Click(s, e); e.Handled = true; }
        else if (ctrl && e.Key == Key.S) { Save_Click(s, e); e.Handled = true; }
        else if (ctrl && e.Key == Key.N) { New_Click(s, e); e.Handled = true; }
        else if (ctrl && e.Key == Key.O) { Open_Click(s, e); e.Handled = true; }
    }
}
