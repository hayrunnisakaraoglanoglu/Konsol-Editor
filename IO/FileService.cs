using System.Text;
using KonsolEditor.Editor;

namespace KonsolEditor.IO;

public static class FileService
    {
        public static void LoadFromFile(EditorState state, string path)
        {
            string text = File.ReadAllText(path, Encoding.UTF8);
            state.Buffer.SetText(text);
            state.CursorRow = 0;
            state.CursorCol = 0;
            state.CurrentFilePath = path;
            state.IsDirty = false;
        }

        public static void SaveToFile(EditorState state, string path)
        {
            File.WriteAllText(path, state.Buffer.GetText(), Encoding.UTF8);
            state.CurrentFilePath = path;
            state.IsDirty = false;
        }
    }
