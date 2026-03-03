using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Text_Caculator_WPF;

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

        bool isChangingTextByCode;

        private List<Label> labels;
        private List<string> prevLines;
        CancellationTokenSource cancelEvaluationSource;

        string defaultTitle;

        public MainWindow()
        {
            InitializeComponent();

            labels = new();
            prevLines = new();
            cancelEvaluationSource = new CancellationTokenSource();

            DataObject.AddPastingHandler(mainTextBox, PlainTextPasting);

            CommandHandling.TextBox = mainTextBox;
            CommandBindings.Add(new(ApplicationCommands.New, CommandHandling.New, CommandHandling.CanNew));
            CommandBindings.Add(new(ApplicationCommands.Open, CommandHandling.Open, CommandHandling.CanOpen));
            CommandBindings.Add(new(ApplicationCommands.Save, CommandHandling.Save, CommandHandling.CanSave));
            CommandBindings.Add(new(ApplicationCommands.SaveAs, CommandHandling.SaveAs, CommandHandling.CanSaveAs));
            CommandBindings.Add(new(ApplicationCommands.Close, CommandHandling.Close, CommandHandling.CanClose));
            defaultTitle = Title;
            CommandHandling.onClearDoc += CommandHandling_onClearDoc;
            CommandHandling.onDirtyChanged += CommandHandling_onDirtyChanged;
        }

        private void CommandHandling_onClearDoc()
        {
            prevLines.Clear();

            cancelEvaluationSource.Cancel();
            if (!cancelEvaluationSource.TryReset())
                cancelEvaluationSource = new CancellationTokenSource();
            DelayEvaluateDocument(cancelEvaluationSource.Token);
        }

        private void CommandHandling_onDirtyChanged(bool newDirty)
        {
            string path = CommandHandling.GetFileName();
            if (string.IsNullOrEmpty(path))
            {
                Title = $"{defaultTitle}{(newDirty ? "*" : "")}";
                return;
            }

            Title = $"{defaultTitle}{(newDirty ? "*" : "")} - {path}";
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isChangingTextByCode) return;
            CommandHandling.SetDirty();
            RepaintLabels();

            cancelEvaluationSource.Cancel();
            if (!cancelEvaluationSource.TryReset())
                cancelEvaluationSource = new CancellationTokenSource();
            DelayEvaluateDocument(cancelEvaluationSource.Token);
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

            isChangingTextByCode = true;

            mainTextBox.BeginChange();
            EvaluteDocument();
            mainTextBox.EndChange();

            isChangingTextByCode = false;
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

                if (!reEvalute && i < prevLines.Count && prevLines[i] == line)
                    continue;

                paraTextRange.ClearAllProperties();
                SymbolConvertor.SetLineNumber(i);
                SymbolConvertor.SetUserVariable(string.Empty, default);

                if (i == prevLines.Count)
                    prevLines.Add(line);
                else
                    prevLines[i] = line;

                Label label = i >= labels.Count ? CreateNewLabel(para.ContentEnd) : labels[i];
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
                        if (SymbolConvertor.IsIdentifierPreserved(varname))
                        {
                            displayError = ErrorMessages.IdentifierPreserved;
                            varNameValid = false;
                        }
                        else
                        {
                            var result = EvaluateAndDisplayResultToLabel(label, start.GetPositionAtOffset(varNameEnd + 1), end);
                            
                            if (result.isSuccessful)
                                SymbolConvertor.SetUserVariable(varname, result.value);
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

            for (int i = list.Count; i < labels.Count || i < prevLines.Count; i++)
            {
                labels[i].Visibility = Visibility.Hidden;
                prevLines[i] = string.Empty;
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
                if (result.isSuccessful)
                    text = $"= {result.value:G15}";
                else
                {
                    TextPointer errorStart = start.GetPositionAtOffset(result.errorStartIndex + 1);
                    TextPointer errorEnd = start.GetPositionAtOffset(result.errorEndIndex + 1);
                    new TextRange(errorStart, errorEnd).ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
                    text = result.errorMessage;
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
            labels.Add(label);
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
            for (int i = 0; i < list.Count && i < labels.Count; i++)
                if (list[i] is Paragraph para)
                    SetLabelPosition(labels[i], para.ContentEnd);
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CommandHandling.NotifySaveChanges())
                e.Cancel = true;
        }
    }
}
