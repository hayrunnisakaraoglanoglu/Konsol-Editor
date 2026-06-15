namespace KonsolEditor.UI;

public sealed class FileManagerResult
{
    public string? SelectedPath { get; init; }
    public bool Cancelled { get; init; }
}

public static class FileManagerView
{
    public static FileManagerResult Show(string startDirectory, string title)
{
    var currentDir = startDirectory;
    int selectedIndex = 0;
    int scrollOffset = 0;          // ← YENİ
    var fileNameInput = "";

    while (true)
    {
        var entries = GetEntries(currentDir);
        selectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, entries.Count - 1));

        // Scroll güncelle — seçili öğe her zaman görünür kalsın
        int maxLines = Math.Max(1, Console.WindowHeight - 8);
        if (selectedIndex < scrollOffset)
            scrollOffset = selectedIndex;
        if (selectedIndex >= scrollOffset + maxLines)
            scrollOffset = selectedIndex - maxLines + 1;

        Render(title, currentDir, entries, selectedIndex, fileNameInput, scrollOffset);  // ← scrollOffset eklendi

        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Escape)
            return new FileManagerResult { Cancelled = true };

        if (key.Key == ConsoleKey.UpArrow)
        {
            selectedIndex = selectedIndex <= 0 ? entries.Count - 1 : selectedIndex - 1;
        }
        else if (key.Key == ConsoleKey.DownArrow)
        {
            selectedIndex = selectedIndex >= entries.Count - 1 ? 0 : selectedIndex + 1;
        }
        else if (key.Key == ConsoleKey.RightArrow)
        {
            if (entries.Count > 0)
            {
                var chosen = entries[selectedIndex];
                if (chosen.IsDirectory)
                {
                    currentDir = chosen.FullPath;
                    selectedIndex = 0;
                    scrollOffset = 0;    // ← Klasör değişince sıfırla
                }
                else
                {
                    fileNameInput = Path.GetFileName(chosen.FullPath);
                }
            }
        }
        else if (key.Key == ConsoleKey.LeftArrow)
        {
            var parent = Directory.GetParent(currentDir);
            if (parent != null) currentDir = parent.FullName;
            selectedIndex = 0;
            scrollOffset = 0;            // ← Üst dizine çıkınca sıfırla
        }
        else if (key.Key == ConsoleKey.Enter)
        {
            if (!string.IsNullOrWhiteSpace(fileNameInput))
                return new FileManagerResult { SelectedPath = Path.Combine(currentDir, fileNameInput), Cancelled = false };
            if (entries.Count > 0)
            {
                var chosen = entries[selectedIndex];
                if (chosen.IsDirectory)
                {
                    currentDir = chosen.FullPath;
                    selectedIndex = 0;
                    scrollOffset = 0;    // ← Klasör değişince sıfırla
                }
                else
                {
                    return new FileManagerResult { SelectedPath = chosen.FullPath, Cancelled = false };
                }
            }
        }
        else if (key.Key == ConsoleKey.Backspace)
        {
            if (fileNameInput.Length > 0)
                fileNameInput = fileNameInput[..^1];
        }
        else if (!char.IsControl(key.KeyChar))
            fileNameInput += key.KeyChar;
    }
}

    private static List<Entry> GetEntries(string dir)
    {
        var list = new List<Entry>();
        try
        {
            foreach (var d in Directory.GetDirectories(dir).OrderBy(x => x))
                list.Add(new Entry(Path.GetFileName(d)!, d, true));
            foreach (var f in Directory.GetFiles(dir).OrderBy(x => x))
                list.Add(new Entry(Path.GetFileName(f)!, f, false));
        }
        catch { }
        return list;
    }

    private static void Render(string title, string dir, List<Entry> entries,
    int selectedIndex, string fileNameInput, int scrollOffset)    // ← YENİ PARAMETRE
{
    Console.Clear();
    Console.WriteLine(title);
    Console.WriteLine($"Dizin: {dir}");
    Console.WriteLine("↑/↓ seç | → içeri gir | ← üst dizin | Enter onay | ESC iptal");
    Console.WriteLine(new string('-', Math.Min(Console.WindowWidth, 80)));

    int maxLines = Math.Max(1, Console.WindowHeight - 8);

    // scrollOffset'ten başla, maxLines kadar göster
    for (int i = scrollOffset; i < Math.Min(entries.Count, scrollOffset + maxLines); i++)
    {
        var e = entries[i];
        bool sel = i == selectedIndex;
        if (sel)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.Black;
        }
        Console.WriteLine(e.IsDirectory ? $"[D] {e.Name}" : $"    {e.Name}");
        if (sel) Console.ResetColor();
    }
     Console.WriteLine();
    Console.WriteLine($"[{selectedIndex + 1}/{entries.Count}]");   // ← Konum göstergesi
    Console.Write("Dosya adı: ");
    Console.Write(fileNameInput);
}

    private sealed record Entry(string Name, string FullPath, bool IsDirectory);
}