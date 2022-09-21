using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Text_Caculator_WPF;

namespace Text_Calculator_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int evaluationDelay = 200;

        bool isChangingTextByCode;

        private List<Label> labels;
        private List<string> prevLines;
        CancellationTokenSource cancelEvaluationSource;

        public MainWindow()
        {
            InitializeComponent();

            labels = new();
            prevLines = new();
            cancelEvaluationSource = new CancellationTokenSource();
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isChangingTextByCode) return;
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
            System.Collections.IList list = mainTextBox.Document.Blocks;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is Paragraph para)
                {
                    TextPointer start = para.ContentStart;
                    TextPointer end = para.ContentEnd;

                    TextRange paraTextRange = new TextRange(start, end);
                    string line = paraTextRange.Text;

                    if (i < prevLines.Count && prevLines[i] == line)
                        continue;

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
                        paraTextRange.ClearAllProperties();
                        paraTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Green);
                        label.Visibility = Visibility.Hidden;
                        continue;
                    }

                    label.Visibility = Visibility.Visible;
                    paraTextRange.ClearAllProperties();

                    string text;
                    try
                    {
                        var result = Evaluator.Evaluate(line);
                        if (result.isSuccessful)
                            text = $"= {result.value:G15}";
                        else
                        {
                            TextPointer errorStart = para.ContentStart.GetPositionAtOffset(result.errorStartIndex + 1);
                            TextPointer errorEnd = para.ContentStart.GetPositionAtOffset(result.errorEndIndex + 1);
                            new TextRange(errorStart, errorEnd).ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
                            text = result.errorMessage;
                        }
                    }
                    catch (Exception ex)
                    {
                        text = ex.Message + "\n" + ex.StackTrace;
                    }

                    label.Content = text;
                }
            }

            for (int i = list.Count; i < labels.Count || i < prevLines.Count; i++)
            {
                labels[i].Visibility = Visibility.Hidden;
                prevLines[i] = string.Empty;
            }
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

        private void RepaintLabels()
        {
            System.Collections.IList list = mainTextBox.Document.Blocks;
            for (int i = 0; i < list.Count && i < labels.Count; i++)
                if (list[i] is Paragraph para)
                    SetLabelPosition(labels[i], para.ContentEnd);
        }
    }
}
