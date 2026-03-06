using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Text_Calculator_WPF
{
    public static class CommandHandling
    {
        public static event Action? OnClearDoc;
        public static event Action<bool>? OnDirtyChanged;

        public static RichTextBox? TextBox;
        static string _currentPath;
        static bool _dirty;

        static CommandHandling()
        {
            _currentPath = string.Empty;
            _dirty = false;
        }

        static bool IsDirty
        {
            set
            {
                _dirty = value;
                OnDirtyChanged?.Invoke(_dirty);
            }
        }
        public static void SetDirty() => IsDirty = true;

        public static string GetFileName()
        {
            if (string.IsNullOrEmpty(_currentPath))
                return string.Empty;

            return _currentPath;
        }

        /// <returns>False if user selected cancel.</returns>
        public static bool NotifySaveChanges()
        {
            if (!_dirty) return true;

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
            OnClearDoc?.Invoke();
            _currentPath = string.Empty;
            IsDirty = false;
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
                OnClearDoc?.Invoke();
                TextBox.AppendText(textData);
                _currentPath = fileDialog.FileName;
                IsDirty = false;
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

            IsDirty = false;
            Application.Current.Shutdown();
        }
        public static void CanClose(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        static bool GuardSave()
        {
            if (string.IsNullOrEmpty(_currentPath))
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

            _currentPath = fileDialog.FileName;
            return Save();
        }

        static bool Save()
        {
            if (TextBox is null) return false;

            try
            {
                var lines = TextBox.Document.Blocks.Select(x => new TextRange(x.ContentStart, x.ContentEnd).Text);
                File.WriteAllLines(_currentPath, lines, Encoding.Unicode);
                IsDirty = false;
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
