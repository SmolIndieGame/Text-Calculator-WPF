using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Text_Calculator_WPF
{
    // TODO: make constant and constant implicit multiplication have higher order,
    //       include some of the unaryOp like "%" and "degree", ("!" should have a higher order),
    //       but care for special case when there are "^" operators around.
    // TODO: finish those unary operations

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int evaluationDelay = 333;

        bool _isChangingTextByCode;

        private List<Label> _labels;
        private List<string> _prevLines;
        CancellationTokenSource _cancelEvaluationSource;

        string _defaultTitle;

        public MainWindow()
        {
            InitializeComponent();

            _labels = new();
            _prevLines = new();
            _cancelEvaluationSource = new CancellationTokenSource();

            DataObject.AddPastingHandler(mainTextBox, PlainTextPasting);

            CommandHandling.TextBox = mainTextBox;
            CommandBindings.Add(new(ApplicationCommands.New, CommandHandling.New, CommandHandling.CanNew));
            CommandBindings.Add(new(ApplicationCommands.Open, CommandHandling.Open, CommandHandling.CanOpen));
            CommandBindings.Add(new(ApplicationCommands.Save, CommandHandling.Save, CommandHandling.CanSave));
            CommandBindings.Add(new(ApplicationCommands.SaveAs, CommandHandling.SaveAs, CommandHandling.CanSaveAs));
            CommandBindings.Add(new(ApplicationCommands.Close, CommandHandling.Close, CommandHandling.CanClose));
            _defaultTitle = Title;
            CommandHandling.onClearDoc += CommandHandling_onClearDoc;
            CommandHandling.onDirtyChanged += CommandHandling_onDirtyChanged;
        }

        private void CommandHandling_onClearDoc()
        {
            _prevLines.Clear();

            _cancelEvaluationSource.Cancel();
            if (!_cancelEvaluationSource.TryReset())
                _cancelEvaluationSource = new CancellationTokenSource();
            DelayEvaluateDocument(_cancelEvaluationSource.Token);
        }

        private void CommandHandling_onDirtyChanged(bool newDirty)
        {
            string path = CommandHandling.GetFileName();
            if (string.IsNullOrEmpty(path))
            {
                Title = $"{_defaultTitle}{(newDirty ? "*" : "")}";
                return;
            }

            Title = $"{_defaultTitle}{(newDirty ? "*" : "")} - {path}";
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isChangingTextByCode) return;
            CommandHandling.SetDirty();
            RepaintLabels();

            _cancelEvaluationSource.Cancel();
            if (!_cancelEvaluationSource.TryReset())
                _cancelEvaluationSource = new CancellationTokenSource();
            DelayEvaluateDocument(_cancelEvaluationSource.Token);
        }

        private async void DelayEvaluateDocument(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(evaluationDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _isChangingTextByCode = true;

            mainTextBox.BeginChange();
            EvaluteDocument();
            mainTextBox.EndChange();

            _isChangingTextByCode = false;
        }

        private void EvaluteDocument()
        {
            bool reEvalute = false;

            System.Collections.IList list = mainTextBox.Document.Blocks;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not Paragraph para)
                    continue;

                TextPointer start = para.ContentStart;
                TextPointer end = para.ContentEnd;

                TextRange paraTextRange = new TextRange(start, end);
                string line = paraTextRange.Text;

                if (!reEvalute && i < _prevLines.Count && _prevLines[i] == line)
                    continue;

                paraTextRange.ClearAllProperties();
                SymbolConverter.SetLineNumber(i);
                SymbolConverter.SetUserVariable(string.Empty, default);

                if (i == _prevLines.Count)
                    _prevLines.Add(line);
                else
                    _prevLines[i] = line;

                Label label = i >= _labels.Count ? CreateNewLabel(para.ContentEnd) : _labels[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    label.Visibility = Visibility.Hidden;
                    continue;
                }

                if (line.StartsWith("//"))
                {
                    paraTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Green);
                    label.Visibility = Visibility.Hidden;
                    continue;
                }

                label.Visibility = Visibility.Visible;
                if (line.StartsWith("Dim ") || line.StartsWith("dim "))
                {
                    int varNameStart = 4;
                    while (varNameStart < line.Length && line[varNameStart] == ' ')
                        varNameStart++;
                    bool varNameValid = true;
                    int varNameEnd = varNameStart;
                    while (varNameEnd < line.Length && line[varNameEnd] != ':')
                    {
                        if (line[varNameEnd] is not ('_' or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z')))
                            varNameValid = false;
                        varNameEnd++;
                    }

                    string displayError = ErrorMessages.InvalidIdentifier;
                    if (varNameEnd == varNameStart)
                        varNameValid = false;
                    if (varNameEnd == line.Length)
                    {
                        displayError = "Missing ':'";
                        varNameValid = false;
                    }

                    if (varNameValid)
                    {
                        var varname = line[varNameStart..varNameEnd];
                        if (SymbolConverter.IsIdentifierPreserved(varname))
                        {
                            displayError = ErrorMessages.IdentifierPreserved;
                            varNameValid = false;
                        }
                        else
                        {
                            var result = EvaluateAndDisplayResultToLabel(label, start.GetPositionAtOffset(varNameEnd + 1), end);
                            
                            if (result.IsSuccessful)
                                SymbolConverter.SetUserVariable(varname, result.Value);
                        }
                    }

                    if (!varNameValid)
                    {
                        TextPointer errorStart = start.GetPositionAtOffset(varNameStart);
                        TextPointer errorEnd = start.GetPositionAtOffset(varNameEnd + 1);
                        new TextRange(errorStart, errorEnd ?? errorStart).ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
                        label.Content = displayError;
                    }

                    new TextRange(start, start.GetPositionAtOffset(4)).ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue);
                    reEvalute = true;
                    continue;
                }

                EvaluateAndDisplayResultToLabel(label, start, end);
            }

            for (int i = list.Count; i < _labels.Count || i < _prevLines.Count; i++)
            {
                _labels[i].Visibility = Visibility.Hidden;
                _prevLines[i] = string.Empty;
            }
        }

        private static EvaluateResult EvaluateAndDisplayResultToLabel(Label label, TextPointer start, TextPointer end)
        {
            TextRange textRange = new TextRange(start.GetPositionAtOffset(1), end);

            string text;
            EvaluateResult result;
            try
            {
                result = Evaluator.Evaluate(textRange.Text);
                if (result.IsSuccessful)
                    text = $"= {result.Value:G15}";
                else
                {
                    TextPointer errorStart = start.GetPositionAtOffset(result.ErrorStartIndex + 1);
                    TextPointer errorEnd = start.GetPositionAtOffset(result.ErrorEndIndex + 1);
                    new TextRange(errorStart, errorEnd).ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
                    text = result.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                text = ex.Message + "\n" + ex.StackTrace;
                result = new(ex.Message, 0, 0);
            }

            label.Content = text;
            return result;
        }

        private Label CreateNewLabel(TextPointer pointer)
        {
            Label label = new Label();
            label.Content = "= HELLO!!";
            label.Foreground = Brushes.CadetBlue;
            label.FontFamily = new FontFamily("Cambria");
            label.FontSize = 14;
            _labels.Add(label);
            labelCanvas.Children.Add(label);
            SetLabelPosition(label, pointer);
            return label;
        }

        private void SetLabelPosition(Label label, TextPointer pointer)
        {
            Rect rect = pointer.GetCharacterRect(LogicalDirection.Forward);
            label.SetValue(Canvas.LeftProperty, rect.Left);
            label.SetValue(Canvas.TopProperty, rect.Top - 3.5);
        }

        private void mainTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RepaintLabels();
        }
        private void mainTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            RepaintLabels();
        }
        private static void PlainTextPasting(object sender, DataObjectPastingEventArgs e)
        {
            e.DataObject = new DataObject(DataFormats.Text, e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty);
        }

        private void RepaintLabels()
        {
            System.Collections.IList list = mainTextBox.Document.Blocks;
            for (int i = 0; i < list.Count && i < _labels.Count; i++)
                if (list[i] is Paragraph para)
                    SetLabelPosition(_labels[i], para.ContentEnd);
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CommandHandling.NotifySaveChanges())
                e.Cancel = true;
        }
    }
}
