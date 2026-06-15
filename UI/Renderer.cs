using KonsolEditor.Editor;

namespace KonsolEditor.UI;
public static class Renderer
{
    private const int LineNumberWidth = 5; // "  1 | " gibi

    public static void Render(EditorState state, bool showCursor=true)
    {
        int width = Console.WindowWidth;
        var toolLines = Toolbox.BuildLines(width);
        int topOffset = toolLines.Length;
        int viewHeight = Math.Max(1, Console.WindowHeight - topOffset);
        int contentWidth = Math.Max(1, width - LineNumberWidth);

        Console.CursorVisible = false;
        Console.SetCursorPosition(0, 0);

        // Toolbar
        foreach (var tl in toolLines)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            string padded = tl.PadRight(width);
            Console.Write(padded[..Math.Min(padded.Length, width)]);
        }

        // Normalize selection order
        (int sRow, int sCol)? selStart = null;
        (int eRow, int eCol)? selEnd = null;
        if (state.SelectionStart.HasValue && state.SelectionEnd.HasValue)
        {
            var (r1, c1) = state.SelectionStart.Value;
            var (r2, c2) = state.SelectionEnd.Value;
            if (r1 < r2 || (r1 == r2 && c1 <= c2))
            { selStart = (r1, c1); selEnd = (r2, c2); }
            else
            { selStart = (r2, c2); selEnd = (r1, c1); }
        }

        // Editor lines
        for (int vi = 0; vi < viewHeight; vi++)
        {
            int lineIndex = state.TopLine + vi;
            Console.SetCursorPosition(0, topOffset + vi);

            if (lineIndex >= state.Buffer.LineCount)
            {
                // Boş satır numarası alanı
                Console.BackgroundColor = ConsoleColor.DarkGray;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("".PadRight(LineNumberWidth));
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("".PadRight(contentWidth));
                continue;
            }

            // Satır numarası
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.White;
            string lineNum = (lineIndex + 1).ToString().PadLeft(3) + " |";
            Console.Write(lineNum);

            var line = state.Buffer.GetLine(lineIndex);

            // Search highlight
            List<(int start, int end)> highlights = new();
            if (!string.IsNullOrEmpty(state.SearchTerm))
            {
                int idx = 0;
                while (true)
                {
                    int found = line.IndexOf(state.SearchTerm, idx, StringComparison.OrdinalIgnoreCase);
                    if (found == -1) break;
                    highlights.Add((found, found + state.SearchTerm.Length));
                    idx = found + state.SearchTerm.Length;
                }
            }

            // Render char by char
            int col = 0;
            while (col <= line.Length && col < contentWidth)
            {
                bool isSelected = false;
                if (selStart.HasValue && selEnd.HasValue)
                {
                    var (sr, sc) = selStart.Value;
                    var (er, ec) = selEnd.Value;
                    if (lineIndex > sr && lineIndex < er)
                        isSelected = true;
                    else if (lineIndex == sr && lineIndex == er)
                        isSelected = col >= sc && col < ec;
                    else if (lineIndex == sr)
                        isSelected = col >= sc;
                    else if (lineIndex == er)
                        isSelected = col < ec;
                }

                bool isSearchHit = highlights.Any(h => col >= h.start && col < h.end);
                bool isCurrentHit = false;
                if (state.CurrentSearchIndex >= 0 && state.CurrentSearchIndex < state.SearchResults.Count)
                {
                    var (hr, hc) = state.SearchResults[state.CurrentSearchIndex];
                    if (!string.IsNullOrEmpty(state.SearchTerm))
                        isCurrentHit = lineIndex == hr && col >= hc && col < hc + state.SearchTerm.Length;
                }

                // Renk ataması
                if (isCurrentHit)
                {
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else if (isSelected)
                {
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else if (isSearchHit)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White;
                }

                if (col < line.Length)
                    Console.Write(line[col]);
                else
                    Console.Write(' ');

                col++;
            }

            // Fill rest of line
            if (col < contentWidth)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("".PadRight(contentWidth - col));
            }
        }

        // Cursor
        int cursorScreenRow = state.CursorRow - state.TopLine + topOffset;
        int cursorScreenCol = state.CursorCol + LineNumberWidth;
        if (cursorScreenRow >= topOffset && cursorScreenRow < Console.WindowHeight
           && cursorScreenCol >= LineNumberWidth && cursorScreenCol < width)
        {
        Console.SetCursorPosition(cursorScreenCol, cursorScreenRow);
        Console.CursorVisible = showCursor;   // ← blinkTimer'a göre kontrol
        }
    }
}