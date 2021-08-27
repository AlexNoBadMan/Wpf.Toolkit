using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Wpf.Toolkit
{
    public class HighlightTextBlock : TextBlock
    {
        public static readonly DependencyProperty HighlightedTextProperty = DependencyProperty.Register("HighlightedText", typeof(string), typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(string.Empty, OnTextChanged));
        
        public static readonly DependencyProperty HighlightedTextColorProperty = DependencyProperty.Register("HighlightedTextColor", typeof(SolidColorBrush), typeof(HighlightTextBlock), 
            new FrameworkPropertyMetadata(Brushes.LightBlue));
        
        public string HighlightedText
        {
            get { return (string)GetValue(HighlightedTextProperty); }
            set { SetValue(HighlightedTextProperty, value); }
        }
        
        public SolidColorBrush HighlightedTextColor
        {
            get { return (SolidColorBrush)GetValue(HighlightedTextColorProperty); }
            set { SetValue(HighlightedTextColorProperty, value); }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HighlightTextBlock textBlock)
                textBlock.Dispatcher.BeginInvoke(new Action(() =>  
                    SetTextBlockTextAndHighlightTerm(textBlock, (string)textBlock.GetValue(TextProperty), textBlock.HighlightedText, textBlock.HighlightedTextColor)), 
                    System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private static void SetTextBlockTextAndHighlightTerm(TextBlock textBlock, string text, string highlightedText, SolidColorBrush highlightedTextColor)
        {
            if (string.IsNullOrWhiteSpace(text) || !textBlock.IsVisible)
                return;

            var isDefaultText = textBlock.Inlines.Count < 2;
            var isEmptyHighlightedText = string.IsNullOrWhiteSpace(highlightedText);
            if (isDefaultText && isEmptyHighlightedText)
                return;

            if (isEmptyHighlightedText || !(text.IndexOf(highlightedText, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                if (!isDefaultText)
                {
                    textBlock.Inlines.Clear();
                    AddPartToTextBlock(textBlock, text);
                }
                return;
            }
            textBlock.Inlines.Clear();

            var textParts = Regex.Split(text, $@"({Regex.Escape(highlightedText)})", RegexOptions.IgnoreCase)
                                 .Where(p => p != string.Empty);

            foreach (var textPart in textParts)
            {
                if (textPart.Equals(highlightedText, StringComparison.OrdinalIgnoreCase))
                    AddHighlightedPartToTextBlock(textBlock, textPart, highlightedTextColor);
                else
                    AddPartToTextBlock(textBlock, textPart);
            }
        }

        private static void AddPartToTextBlock(TextBlock textBlock, string part)
        {
            textBlock.Inlines.Add(new Run { Text = part });
        }

        private static void AddHighlightedPartToTextBlock(TextBlock textBlock, string part, SolidColorBrush highlightedTextColor)
        {
            textBlock.Inlines.Add(new Run { Text = part, Background = highlightedTextColor });
        }
    }
}
