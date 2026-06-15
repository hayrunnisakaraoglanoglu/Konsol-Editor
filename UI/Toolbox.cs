using System;
using System.Collections.Generic;

namespace KonsolEditor.UI;

public static class Toolbox
{
    private static readonly (string command, string shortcut)[] Items =
    {
        ("Dosya Aç", "Ctrl+O"),
        ("Dosya Kaydet", "Ctrl+S"),
        ("Farklı Kaydet", "Ctrl+D"),
        ("Arama", "Ctrl+F"),
        ("Eşleşme Gez", "Ctrl+G → ←"),
        ("Değiştir", "Ctrl+R"),
        ("Kopyala", "Ctrl+C"),
        ("Yapıştır", "Ctrl+V"),
        ("Kes", "Ctrl+X"),
        ("Geri Al", "Ctrl+Z"),
        ("Karakter Seç", "Shift+Yön"),
        ("Kelime Seç", "Ctrl+Shift+Yön"),
        ("Çıkış", "ESC")
    };

    // ------------------- BuildLines -------------------
    public static string[] BuildLines(int width)
    {
        var lines = new List<string>();
        string currentLine = "";

        foreach (var (cmd, shortcut) in Items)
        {
            string block = $"{cmd} ({shortcut})";

            if (currentLine.Length + block.Length + 1 > width)
            {
                lines.Add(currentLine.PadRight(width));
                currentLine = block;
            }
            else
            {
                if (currentLine.Length > 0) currentLine += " ";
                currentLine += block;
            }
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine.PadRight(width));

        return lines.ToArray();
    }

    // ------------------- Render -------------------
    public static void Render(int width)
    {
        Console.BackgroundColor = ConsoleColor.DarkBlue;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Clear();

        var lines = BuildLines(width);

        foreach (var line in lines)
            Console.WriteLine(line);

        // Alt çizgi
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('─', width));

        // Editör alanı
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
    }
}