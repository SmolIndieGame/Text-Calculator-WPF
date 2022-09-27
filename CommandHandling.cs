using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Text_Calculator_WPF
{
    public static class CommandHandling
    {
        public static event Action? onClearDoc;
        public static event Action<bool>? onDirtyChanged;

        public static RichTextBox? TextBox;
        static string currentPath;
        static bool dirty;

        static CommandHandling()
        {
            currentPath = string.Empty;
            dirty = false;
        }

        static bool isDirty
        {
            set
            {
                dirty = value;
                onDirtyChanged?.Invoke(dirty);
            }
        }
        public static void SetDirty() => isDirty = true;

        public static string GetFileName()
        {
            if (string.IsNullOrEmpty(currentPath))
                return string.Empty;

            return currentPath;
        }

        /// <returns>False if user selected cancel.</returns>
        public static bool NotifySaveChanges()
        {
            if (!dirty) return true;

            string messageBoxText = "Do you want to save changes?";
            string caption = "Text Calculator";

            MessageBoxResult result = MessageBox.Show(
                messageBoxText,
                caption,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Yes);

            return result switch
            {
                MessageBoxResult.Yes => GuardSave(),
                MessageBoxResult.No => true,
                _ => false,
            };
        }
        

        public static void New(object target, ExecutedRoutedEventArgs e)
        {
            if (TextBox is null) return;
            if (!NotifySaveChanges()) return;

            TextBox.Document.Blocks.Clear();
            onClearDoc?.Invoke();
            currentPath = string.Empty;
            isDirty = false;
        }
        public static void CanNew(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        public static void Open(object target, ExecutedRoutedEventArgs e)
        {
            if (TextBox is null) return;
            if (!NotifySaveChanges()) return;

            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".txtc";
            fileDialog.Filter = "Text calculator docs (.txtc)|*.txtc";

            bool? result = fileDialog.ShowDialog();
            if (!result.HasValue || !result.Value)
                return;

            try
            {
                string textData = File.ReadAllText(fileDialog.FileName, Encoding.Unicode);
                TextBox.Document.Blocks.Clear();
                onClearDoc?.Invoke();
                TextBox.AppendText(textData);
                currentPath = fileDialog.FileName;
                isDirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Text Calculator - Read File Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        public static void CanOpen(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        public static void Save(object target, ExecutedRoutedEventArgs e) => GuardSave();
        public static void CanSave(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        public static void SaveAs(object target, ExecutedRoutedEventArgs e) => SaveAs();
        public static void CanSaveAs(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        public static void Close(object target, ExecutedRoutedEventArgs e)
        {
            if (!NotifySaveChanges()) return;

            isDirty = false;
            Application.Current.Shutdown();
        }
        public static void CanClose(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        static bool GuardSave()
        {
            if (string.IsNullOrEmpty(currentPath))
                return SaveAs();
            return Save();
        }

        static bool SaveAs()
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.DefaultExt = ".txtc";
            fileDialog.Filter = "Text calculator docs (.txtc)|*.txtc";

            bool? result = fileDialog.ShowDialog();
            if (!result.HasValue || !result.Value)
                return false;

            currentPath = fileDialog.FileName;
            return Save();
        }

        static bool Save()
        {
            if (TextBox is null) return false;

            try
            {
                var lines = TextBox.Document.Blocks.Select(x => new TextRange(x.ContentStart, x.ContentEnd).Text);
                File.WriteAllLines(currentPath, lines, Encoding.Unicode);
                isDirty = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Text Calculator - Save File Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            return false;
        }
    }
}
