using System;
using System.Collections.Generic;

namespace KonsolEditor.Editor
{
    // ---------------- EditorState ----------------
    public sealed class EditorState
    {
        // Metin yönetimi
        public TextBuffer Buffer { get; } = new TextBuffer();

        // Cursor
        public int CursorRow { get; set; } = 0;
        public int CursorCol { get; set; } = 0;

        // Görünüm
        public int TopLine { get; set; } = 0;

        // Dosya ve değişiklik durumu
        public bool IsDirty { get; set; } = false;
        public string? CurrentFilePath { get; set; } = null;

        // Arama
        public string? SearchTerm { get; set; } = null;
        public List<(int row, int col)> SearchResults { get; set; } = new();
        public int CurrentSearchIndex { get; set; } = -1;

        // ---------------- Seçim ----------------
        public (int Row, int Col)? SelectionStart { get; set; } = null;
        public (int Row, int Col)? SelectionEnd { get; set; } = null;

        public bool HasSelection => SelectionStart.HasValue && SelectionEnd.HasValue;

        public string GetSelectedText()
        {
            if (!HasSelection) return "";

            var (startRow, startCol) = SelectionStart!.Value;
            var (endRow, endCol) = SelectionEnd!.Value;

            // tersse swap
            if (startRow > endRow || (startRow == endRow && startCol > endCol))
            {
                (startRow, endRow) = (endRow, startRow);
                (startCol, endCol) = (endCol, startCol);
            }

            if (startRow == endRow)
                return Buffer.GetLine(startRow).Substring(startCol, endCol - startCol);

            var lines = new List<string>();
            lines.Add(Buffer.GetLine(startRow)[startCol..]);
            for (int r = startRow + 1; r < endRow; r++)
                lines.Add(Buffer.GetLine(r));
            lines.Add(Buffer.GetLine(endRow)[..endCol]);

            return string.Join("\n", lines);
        }

        // ---------------- Seçim Metodları ----------------
        public void StartSelection()
        {
            SelectionStart = (CursorRow, CursorCol);
            SelectionEnd = (CursorRow, CursorCol);
        }

        public void UpdateSelection()
        {
            if (SelectionStart.HasValue)
            {
                SelectionEnd = (CursorRow, CursorCol);
            }
        }

        public void ClearSelection()
        {
            SelectionStart = null;
            SelectionEnd = null;
        }

        // ---------------- Metodlar ----------------
        public void NewEmptyDocument()
        {
            Buffer.SetText("");
            CursorRow = 0;
            CursorCol = 0;
            CurrentFilePath = null;
            IsDirty = false;
            ClearSelection();
            SearchTerm = null;
            SearchResults.Clear();
            CurrentSearchIndex = -1;
        }

        public EditorStateSnapshot GetSnapshot()
        {
            return new EditorStateSnapshot
            {
                Lines = Buffer.GetLinesCopy(),
                CursorRow = this.CursorRow,
                CursorCol = this.CursorCol,
                SelectionStart = this.SelectionStart,
                SelectionEnd = this.SelectionEnd
            };
        }
    }

    // ---------------- Undo Snapshot ----------------
    public class EditorStateSnapshot
    {
        public List<string> Lines = new List<string>();
        public int CursorRow;
        public int CursorCol;

        public (int Row, int Col)? SelectionStart { get; set; } = null;
        public (int Row, int Col)? SelectionEnd { get; set; } = null;
    }
}