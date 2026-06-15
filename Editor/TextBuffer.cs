using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace KonsolEditor.Editor;

public sealed class TextBuffer
{
    private readonly List<StringBuilder> _lines = new() { new StringBuilder() };

    public int LineCount => _lines.Count;

    public string GetLine(int row) => _lines[row].ToString();

    // 🔥 EKLENDİ → SetLine (REPLACE için gerekli)
    public void SetLine(int row, string newLine)
    {
        if (row < 0 || row >= _lines.Count) return;

        _lines[row].Clear();
        _lines[row].Append(newLine);
    }

    // 🔥 BONUS → ReplaceAll (istersen direkt bunu da kullanabilirsin)
    public int ReplaceAll(string oldWord, string newWord, bool ignoreCase = false)
    {
        int count = 0;

        for (int i = 0; i < _lines.Count; i++)
        {
            string original = _lines[i].ToString();

            string replaced;

            if (ignoreCase)
            {
                replaced = System.Text.RegularExpressions.Regex.Replace(
                    original,
                    System.Text.RegularExpressions.Regex.Escape(oldWord),
                    newWord,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }
            else
            {
                replaced = original.Replace(oldWord, newWord);
            }

            if (original != replaced)
                count++;

            _lines[i].Clear();
            _lines[i].Append(replaced);
        }

        return count; // kaç satır değişti
    }

    public void InsertChar(int row, int col, char ch)
    {
        EnsureRow(row);
        col = Math.Clamp(col, 0, _lines[row].Length);
        _lines[row].Insert(col, ch);
    }

    public void DeleteChar(int row, int col)
    {
        if (row < 0 || row >= _lines.Count) return;
        if (col < 0 || col >= _lines[row].Length) return;
        _lines[row].Remove(col, 1);
    }

    public void InsertNewLine(int row, int col)
    {
        EnsureRow(row);
        col = Math.Clamp(col, 0, _lines[row].Length);
        var current = _lines[row].ToString();
        var left = current.Substring(0, col);
        var right = current.Substring(col);
        _lines[row].Clear();
        _lines[row].Append(left);
        _lines.Insert(row + 1, new StringBuilder(right));
    }

    public void MergeLineWithNext(int row)
    {
        if (row < 0 || row >= _lines.Count - 1) return;
        _lines[row].Append(_lines[row + 1]);
        _lines.RemoveAt(row + 1);
    }

    private void EnsureRow(int row)
    {
        while (_lines.Count <= row)
            _lines.Add(new StringBuilder());
    }

    public string GetText() => string.Join("\n", _lines.Select(l => l.ToString()));

    public void SetText(string text)
    {
        _lines.Clear();
        var parts = text.Replace("\r\n", "\n").Split('\n');
        foreach (var p in parts)
            _lines.Add(new StringBuilder(p));
        if (_lines.Count == 0)
            _lines.Add(new StringBuilder());
    }

    public List<string> GetLinesCopy() => _lines.Select(l => l.ToString()).ToList();

    public void DeletePreviousWord(ref int row, ref int col)
    {
        if (row < 0 || row >= _lines.Count) return;

        if (col == 0)
        {
            if (row == 0) return;
            MergeLineWithNext(row - 1);
            col = _lines[row - 1].Length;
            row--;
            return;
        }

        var line = _lines[row].ToString();
        int start = col - 1;

        while (start >= 0 && char.IsWhiteSpace(line[start])) start--;
        while (start >= 0 && !char.IsWhiteSpace(line[start])) start--;

        int deleteCount = col - start - 1;
        _lines[row].Remove(start + 1, deleteCount);
        col = start + 1;
    }
}