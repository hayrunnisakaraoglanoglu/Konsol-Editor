using System.Text;
using KonsolEditor.Editor;
using KonsolEditor.UI;
using KonsolEditor.IO;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
Console.CursorVisible = true;
Console.TreatControlCAsInput = true;

var state = new EditorState();
state.NewEmptyDocument();

var undoStack = new Stack<EditorStateSnapshot>();
string ClipboardBuffer = "";

// Yanıp sönme için zamanlayıcı
var blinkTimer = System.Diagnostics.Stopwatch.StartNew();
bool cursorVisible = true;

// ----------------------------------------------------------------
// Yardımcı Fonksiyonlar
// ----------------------------------------------------------------

string? ReadInputWithEscape(string prompt)
{
    Console.Clear();
    Console.Write(prompt);
    string input = "";
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Escape) return null;
        if (key.Key == ConsoleKey.Enter) break;
        if (key.Key == ConsoleKey.Backspace)
        {
            if (input.Length > 0)
            {
                input = input[..^1];
                Console.Write("\b \b");
            }
            continue;
        }
        if (!char.IsControl(key.KeyChar))
        {
            input += key.KeyChar;
            Console.Write(key.KeyChar);
        }
    }
    return input;
}

void EnsureCursorVisible(EditorState s)
{
    int width = Console.WindowWidth;
    int top = Toolbox.BuildLines(width).Length;
    int viewHeight = Math.Max(1, Console.WindowHeight - top);

    if (s.CursorRow < s.TopLine)
        s.TopLine = s.CursorRow;
    if (s.CursorRow >= s.TopLine + viewHeight)
        s.TopLine = s.CursorRow - viewHeight + 1;

    s.TopLine = Math.Clamp(s.TopLine, 0, Math.Max(0, s.Buffer.LineCount - 1));
}

void SaveToPath(string path)
{
    try
    {
        FileService.SaveToFile(state, path);
    }
    catch (Exception ex)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Kayıt hatası: {ex.Message}");
        Console.ResetColor();
        Console.WriteLine("Devam etmek için bir tuşa basın...");
        Console.ReadKey(intercept: true);
    }
}

void Save()
{
    if (state.CurrentFilePath != null)
    {
        SaveToPath(state.CurrentFilePath);
    }
    else
    {
        var result = FileManagerView.Show(Directory.GetCurrentDirectory(), "DOSYA KAYDET");
        if (!result.Cancelled && result.SelectedPath != null)
            SaveToPath(result.SelectedPath);
    }
}

// ----------------------------------------------------------------
// Kelime Hareket Fonksiyonları (döngü DIŞINDA)
// ----------------------------------------------------------------

int MoveWordRight(EditorState s)
{
    var line = s.Buffer.GetLine(s.CursorRow);
    int col = s.CursorCol;
    while (col < line.Length && !char.IsLetterOrDigit(line[col])) col++;
    while (col < line.Length && char.IsLetterOrDigit(line[col])) col++;
    return col;
}

int MoveWordLeft(EditorState s)
{
    var line = s.Buffer.GetLine(s.CursorRow);
    int col = s.CursorCol;
    col--;
    while (col > 0 && !char.IsLetterOrDigit(line[col])) col--;
    while (col > 0 && char.IsLetterOrDigit(line[col - 1])) col--;
    return col;
}

// ----------------------------------------------------------------
// Ana Döngü
// ----------------------------------------------------------------

while (true)
{
    //Renderer.Render(state);
    //var key = Console.ReadKey(intercept: true);

    // Yanıp sönen imleç + klavye girişi (500ms blink)
ConsoleKeyInfo key;
while (true)
{
    // Her 500ms'de bir imleç görünürlüğünü değiştir
    if (blinkTimer.ElapsedMilliseconds >= 500)
    {
        cursorVisible = !cursorVisible;
        blinkTimer.Restart();
        Renderer.Render(state, cursorVisible);   // cursorVisible parametresi eklendi
    }

    if (Console.KeyAvailable)
    {
        key = Console.ReadKey(intercept: true);
        cursorVisible = true;       // Tuşa basınca imleç hemen görünür olsun
        blinkTimer.Restart();
        break;
    }

    System.Threading.Thread.Sleep(50);  // CPU'yu yorma
}

    // CTRL + O → Dosya Aç
    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.O)
    {
        if (state.IsDirty)
        {
            Console.Clear();
            Console.Write("Kaydedilmemiş değişiklikler var. Devam edilsin mi? (e/h): ");
            var confirm = Console.ReadKey(intercept: true);
            Console.WriteLine();
            if (confirm.KeyChar != 'e' && confirm.KeyChar != 'E')
                continue;
        }
        var result = FileManagerView.Show(Directory.GetCurrentDirectory(), "DOSYA AÇ");
        if (!result.Cancelled && result.SelectedPath != null)
        {
            try
            {
                FileService.LoadFromFile(state, result.SelectedPath);
                state.TopLine = 0;
                state.SearchResults.Clear();
                state.SearchTerm = null;
                undoStack.Clear();
            }
            catch (Exception ex)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Dosya açma hatası: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("Devam etmek için bir tuşa basın...");
                Console.ReadKey(intercept: true);
            }
        }
        continue;
    }

    // CTRL + D → Farklı Kaydet
    if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.D)
    {
        var result = FileManagerView.Show(Directory.GetCurrentDirectory(), "DOSYA FARKLI KAYDET");
        if (!result.Cancelled && result.SelectedPath != null)
            SaveToPath(result.SelectedPath);
        continue;
    }

    // CTRL + S → Kaydet
    if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.S)
    {
        Save();
        continue;
    }

    // OK TUŞLARI (seçimi temizle)
    if (key.Key == ConsoleKey.RightArrow && key.Modifiers == ConsoleModifiers.None)
    {
        state.ClearSelection();
        var line = state.Buffer.GetLine(state.CursorRow);
        if (state.CursorCol < line.Length)
            state.CursorCol++;
        else if (state.CursorRow < state.Buffer.LineCount - 1)
        {
            state.CursorRow++;
            state.CursorCol = 0;
        }
        EnsureCursorVisible(state);
        continue;
    }

    if (key.Key == ConsoleKey.LeftArrow && key.Modifiers == ConsoleModifiers.None)
    {
        state.ClearSelection();
        if (state.CursorCol > 0)
            state.CursorCol--;
        else if (state.CursorRow > 0)
        {
            state.CursorRow--;
            state.CursorCol = state.Buffer.GetLine(state.CursorRow).Length;
        }
        EnsureCursorVisible(state);
        continue;
    }

    if (key.Key == ConsoleKey.DownArrow && key.Modifiers == ConsoleModifiers.None)
    {
        state.ClearSelection();
        state.CursorRow = Math.Min(state.CursorRow + 1, state.Buffer.LineCount - 1);
        state.CursorCol = Math.Min(state.CursorCol, state.Buffer.GetLine(state.CursorRow).Length);
        EnsureCursorVisible(state);
        continue;
    }

    if (key.Key == ConsoleKey.UpArrow && key.Modifiers == ConsoleModifiers.None)
    {
        state.ClearSelection();
        state.CursorRow = Math.Max(state.CursorRow - 1, 0);
        state.CursorCol = Math.Min(state.CursorCol, state.Buffer.GetLine(state.CursorRow).Length);
        EnsureCursorVisible(state);
        continue;
    }

    // CTRL + C → Kopyala
    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.C)
    {
        if (state.HasSelection)
            ClipboardBuffer = state.GetSelectedText();
        continue;
    }

    // CTRL + X → Kes
    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.X)
    {
        if (state.HasSelection)
        {
            undoStack.Push(state.GetSnapshot());
            ClipboardBuffer = state.GetSelectedText();

            var (startRow, startCol) = state.SelectionStart!.Value;
            var (endRow, endCol) = state.SelectionEnd!.Value;

            if (startRow > endRow || (startRow == endRow && startCol > endCol))
            {
                (startRow, endRow) = (endRow, startRow);
                (startCol, endCol) = (endCol, startCol);
            }

            if (startRow == endRow)
            {
                var line = state.Buffer.GetLine(startRow);
                state.Buffer.SetLine(startRow, line.Remove(startCol, endCol - startCol));
            }
            else
            {
                var firstLine = state.Buffer.GetLine(startRow);
                state.Buffer.SetLine(startRow, firstLine[..startCol]);
                for (int r = startRow + 1; r < endRow; r++)
                    state.Buffer.SetLine(r, "");
                var lastLine = state.Buffer.GetLine(endRow);
                state.Buffer.SetLine(endRow, lastLine[endCol..]);
                for (int r = startRow + 1; r <= endRow; r++)
                    state.Buffer.MergeLineWithNext(startRow);
            }

            state.CursorRow = startRow;
            state.CursorCol = startCol;
            state.SelectionStart = null;
            state.SelectionEnd = null;
            state.IsDirty = true;
        }
        continue;
    }

    // CTRL + V → Yapıştır
    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.V)
    {
    if (!string.IsNullOrEmpty(ClipboardBuffer))
    {
        undoStack.Push(state.GetSnapshot());
        var lines = ClipboardBuffer.Replace("\r\n", "\n").Split('\n');

        // İlk satırı mevcut satıra ekle
        var currentLine = state.Buffer.GetLine(state.CursorRow);
        var before = currentLine[..state.CursorCol];
        var after  = currentLine[state.CursorCol..];

        if (lines.Length == 1)
        {
            // Tek satır: doğrudan ekle, satır atlamadan
            state.Buffer.SetLine(state.CursorRow, before + lines[0] + after);
            state.CursorCol = before.Length + lines[0].Length;
        }
        else
        {
            // Çok satır: ilk parçayı mevcut satıra yaz
            state.Buffer.SetLine(state.CursorRow, before + lines[0]);

            // Ortadaki satırları ekle
            for (int i = 1; i < lines.Length - 1; i++)
            {
                state.Buffer.InsertNewLine(state.CursorRow + i - 1,
                    state.Buffer.GetLine(state.CursorRow + i - 1).Length);
                state.Buffer.SetLine(state.CursorRow + i, lines[i]);
            }

            // Son satırı ekle, after'ı sonuna yapıştır
            int lastRow = state.CursorRow + lines.Length - 1;
            state.Buffer.InsertNewLine(lastRow - 1,
                state.Buffer.GetLine(lastRow - 1).Length);
            state.Buffer.SetLine(lastRow, lines[^1] + after);

            state.CursorRow = lastRow;
            state.CursorCol = lines[^1].Length;
        }

        state.IsDirty = true;
    }
    continue;
    }

    // CTRL + Z → Geri Al
    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.Z)
    {
        if (undoStack.Count > 0)
        {
            var snap = undoStack.Pop();
            state.Buffer.SetText(string.Join("\n", snap.Lines));
            state.CursorRow = snap.CursorRow;
            state.CursorCol = snap.CursorCol;
            state.SelectionStart = snap.SelectionStart;
            state.SelectionEnd = snap.SelectionEnd;
        }
        continue;
    }

    // CTRL + BACKSPACE → Kelime Sil
    if (key.Key == ConsoleKey.Backspace && key.Modifiers == ConsoleModifiers.Control)
    {
        if (state.CursorCol == 0 && state.CursorRow > 0)
        {
            undoStack.Push(state.GetSnapshot());
            int prevRow = state.CursorRow - 1;
            int prevLen = state.Buffer.GetLine(prevRow).Length;
            state.Buffer.MergeLineWithNext(prevRow);
            state.CursorRow = prevRow;
            state.CursorCol = prevLen;
            state.IsDirty = true;
        }
        else if (state.CursorCol > 0)
        {
            undoStack.Push(state.GetSnapshot());
            var row = state.CursorRow;
            var col = state.CursorCol;
            state.Buffer.DeletePreviousWord(ref row, ref col);
            state.CursorRow = row;
            state.CursorCol = col;
            state.IsDirty = true;
        }
        continue;
    }

    // NORMAL BACKSPACE → Karakter Sil
    if (key.Key == ConsoleKey.Backspace)
    {
        if (state.CursorCol > 0)
        {
            undoStack.Push(state.GetSnapshot());
            state.Buffer.DeleteChar(state.CursorRow, state.CursorCol - 1);
            state.CursorCol--;
            state.IsDirty = true;
        }
        else if (state.CursorRow > 0)
        {
            undoStack.Push(state.GetSnapshot());
            int prevRow = state.CursorRow - 1;
            int prevLen = state.Buffer.GetLine(prevRow).Length;
            state.Buffer.MergeLineWithNext(prevRow);
            state.CursorRow = prevRow;
            state.CursorCol = prevLen;
            state.IsDirty = true;
        }
        continue;
    }

    // ENTER → Yeni Satır
    if (key.Key == ConsoleKey.Enter)
    {
        undoStack.Push(state.GetSnapshot());
        state.Buffer.InsertNewLine(state.CursorRow, state.CursorCol);
        state.CursorRow++;
        state.CursorCol = 0;
        state.IsDirty = true;
        continue;
    }

    // ESC → Çıkış
    if (key.Key == ConsoleKey.Escape)
    {
        if (state.IsDirty)
        {
            Console.Clear();
            Console.Write("Kaydedilmemiş değişiklikler var. Kaydetmek istiyor musunuz? (e/h): ");
            var answer = Console.ReadKey(intercept: true);
            Console.WriteLine();
            if (answer.KeyChar == 'e' || answer.KeyChar == 'E')
                Save();
        }
        break;
    }

    // CTRL + F → Arama
    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.F)
    {
        Console.Clear();
        Console.Write("Aranacak kelime: ");
        var input = Console.ReadLine();
        state.SearchResults.Clear();
        state.CurrentSearchIndex = -1;

        if (!string.IsNullOrWhiteSpace(input))
        {
            state.SearchTerm = input;
            for (int i = 0; i < state.Buffer.LineCount; i++)
            {
                var line = state.Buffer.GetLine(i);
                int index = 0;
                while (true)
                {
                    int found = line.IndexOf(input, index, StringComparison.OrdinalIgnoreCase);
                    if (found == -1) break;
                    state.SearchResults.Add((i, found));
                    index = found + input.Length;
                }
            }
            if (state.SearchResults.Count > 0)
            {
                state.CurrentSearchIndex = 0;
                var (r, c) = state.SearchResults[0];
                state.CursorRow = r;
                state.CursorCol = c;
            }
        }
        else
        {
            state.SearchTerm = null;
        }
        continue;
    }

    // CTRL + G → Eşleşmeler arası gezinme modu
    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.G)
    {
    if (state.SearchResults.Count == 0) continue;

        // Moda gir — ESC ile çıkılana kadar yön tuşlarını dinle
        while (true)
        {
        // Mevcut eşleşmeyi göster
        var (row, col) = state.SearchResults[state.CurrentSearchIndex];
        state.CursorRow = row;
        state.CursorCol = col;
        EnsureCursorVisible(state);
        Renderer.Render(state, true);

        // Mod bilgisi göster
        int statusRow = Console.WindowHeight - 1;
        Console.SetCursorPosition(0, statusRow);
        Console.BackgroundColor = ConsoleColor.DarkCyan;
        Console.ForegroundColor = ConsoleColor.White;
        string statusMsg = $" Eşleşme [{state.CurrentSearchIndex + 1}/{state.SearchResults.Count}]  ← → ile gezin  |  ESC çıkış ";
        Console.Write(statusMsg.PadRight(Console.WindowWidth));
        Console.ResetColor();

        var navKey = Console.ReadKey(intercept: true);

        if (navKey.Key == ConsoleKey.Escape)
            break;

        if (navKey.Key == ConsoleKey.RightArrow)
        {
            state.CurrentSearchIndex =
                (state.CurrentSearchIndex + 1) % state.SearchResults.Count;
        }
        else if (navKey.Key == ConsoleKey.LeftArrow)
        {
            state.CurrentSearchIndex =
                (state.CurrentSearchIndex - 1 + state.SearchResults.Count) % state.SearchResults.Count;
        }
        // Diğer tuşlar görmezden gelinir, mod içinde kalınır
        }
        continue;
    }

    // CTRL + R → Replace
    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.R)
    {
        string? oldWord = ReadInputWithEscape("Değiştirilecek kelime: ");
        if (string.IsNullOrWhiteSpace(oldWord)) continue;
        string? newWord = ReadInputWithEscape("Yeni kelime: ");
        if (newWord == null) continue;

        undoStack.Push(state.GetSnapshot());
        for (int i = 0; i < state.Buffer.LineCount; i++)
        {
            var line = state.Buffer.GetLine(i);
            state.Buffer.SetLine(i, line.Replace(oldWord, newWord));
        }
        state.IsDirty = true;
        state.SearchResults.Clear();
        state.SearchTerm = null;
        state.CurrentSearchIndex = -1;
        state.CursorCol = Math.Min(state.CursorCol, state.Buffer.GetLine(state.CursorRow).Length);
        continue;
    }

    // SHIFT + CTRL + Yön → Kelime Bazlı Seçim (ÖNCE kontrol edilmeli)
    if (key.Modifiers.HasFlag(ConsoleModifiers.Shift)
        && key.Modifiers.HasFlag(ConsoleModifiers.Control)
        && (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.RightArrow))
    {
        if (!state.HasSelection)
            state.StartSelection();

        if (key.Key == ConsoleKey.RightArrow)
        {
            state.CursorCol = MoveWordRight(state);
            if (state.CursorCol >= state.Buffer.GetLine(state.CursorRow).Length
                && state.CursorRow < state.Buffer.LineCount - 1)
            {
                state.CursorRow++;
                state.CursorCol = 0;
            }
        }
        else
        {
            state.CursorCol = MoveWordLeft(state);
        }

        state.UpdateSelection();
        EnsureCursorVisible(state);
        continue;
    }

    // SHIFT + Yön → Tek Karakter / Satır Seçimi (SONRA kontrol edilmeli)
    if (key.Modifiers.HasFlag(ConsoleModifiers.Shift)
        && !key.Modifiers.HasFlag(ConsoleModifiers.Control)
        && (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.RightArrow
            || key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow))
    {
        if (!state.HasSelection)
            state.StartSelection();

        if (key.Key == ConsoleKey.RightArrow)
        {
            var line = state.Buffer.GetLine(state.CursorRow);
            if (state.CursorCol < line.Length) state.CursorCol++;
            else if (state.CursorRow < state.Buffer.LineCount - 1) { state.CursorRow++; state.CursorCol = 0; }
        }
        else if (key.Key == ConsoleKey.LeftArrow)
        {
            if (state.CursorCol > 0) state.CursorCol--;
            else if (state.CursorRow > 0) { state.CursorRow--; state.CursorCol = state.Buffer.GetLine(state.CursorRow).Length; }
        }
        else if (key.Key == ConsoleKey.DownArrow)
        {
            state.CursorRow = Math.Min(state.CursorRow + 1, state.Buffer.LineCount - 1);
            state.CursorCol = Math.Min(state.CursorCol, state.Buffer.GetLine(state.CursorRow).Length);
        }
        else if (key.Key == ConsoleKey.UpArrow)
        {
            state.CursorRow = Math.Max(state.CursorRow - 1, 0);
            state.CursorCol = Math.Min(state.CursorCol, state.Buffer.GetLine(state.CursorRow).Length);
        }

        state.UpdateSelection();
        EnsureCursorVisible(state);
        continue;
    }

    // NORMAL YAZMA
    if (!char.IsControl(key.KeyChar))
    {
        undoStack.Push(state.GetSnapshot());
        state.Buffer.InsertChar(state.CursorRow, state.CursorCol, key.KeyChar);
        state.CursorCol++;
        state.IsDirty = true;
        continue;
    }
}