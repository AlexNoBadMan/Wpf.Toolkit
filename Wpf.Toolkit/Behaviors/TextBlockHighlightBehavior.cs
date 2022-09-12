using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Wpf.Toolkit.Behaviors
{
  public class TextBlockHighlightBehavior
  {
    public static readonly DependencyProperty TextProperty =
      DependencyProperty.RegisterAttached("Text", typeof(string), typeof(TextBlockHighlightBehavior), new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty HighlightedTextProperty =
      DependencyProperty.RegisterAttached("HighlightedText", typeof(string), typeof(TextBlockHighlightBehavior), new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty ForegroundProperty =
      DependencyProperty.RegisterAttached("Foreground", typeof(SolidColorBrush), typeof(TextBlockHighlightBehavior), new FrameworkPropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty BackgroundProperty =
      DependencyProperty.RegisterAttached("Background", typeof(SolidColorBrush), typeof(TextBlockHighlightBehavior), new FrameworkPropertyMetadata(Brushes.LightBlue));

    public static string GetText(TextBlock textBlock) => (string)textBlock.GetValue(TextProperty);
    public static void SetText(TextBlock textBlock, string value) => textBlock.SetValue(TextProperty, value);

    public static string GetHighlightedText(TextBlock textBlock) => (string)textBlock.GetValue(HighlightedTextProperty);
    public static void SetHighlightedText(TextBlock textBlock, string value) => textBlock.SetValue(HighlightedTextProperty, value);

    public static SolidColorBrush GetForeground(TextBlock textBlock) => (SolidColorBrush)textBlock.GetValue(ForegroundProperty);
    public static void SetForeground(TextBlock textBlock, SolidColorBrush value) => textBlock.SetValue(ForegroundProperty, value);

    public static SolidColorBrush GetBackground(TextBlock textBlock) => (SolidColorBrush)textBlock.GetValue(BackgroundProperty);
    public static void SetBackground(TextBlock textBlock, SolidColorBrush value) => textBlock.SetValue(BackgroundProperty, value);


    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not TextBlock textBlock)
        return;

      SetTextBlockHighlightedText(textBlock);
    }

    private static void SetTextBlockHighlightedText(TextBlock textBlock)
    {
      var text = GetText(textBlock);
      if (string.IsNullOrEmpty(text) || !textBlock.IsVisible)
        return;

      var highlightedText = (string)textBlock.GetValue(HighlightedTextProperty);
      if (string.IsNullOrEmpty(highlightedText) || text.IndexOf(highlightedText, StringComparison.OrdinalIgnoreCase) == -1)
      {
        textBlock.Text = text;
        return;
      }

      textBlock.Inlines.Clear();
      var foreground = GetForeground(textBlock);
      var background = GetBackground(textBlock);
      var foundIndex = text.IndexOf(highlightedText, StringComparison.OrdinalIgnoreCase);

      for (var i = 0; i < text.Length;)
      {
        if (foundIndex == -1)
        {
          AddPart(textBlock, text.Substring(i, text.Length - i));
          break;
        }

        if (foundIndex > i)
        {
          AddPart(textBlock, text.Substring(i, foundIndex - i));
          i = foundIndex;
        }
        else
        {
          AddHighlightedPart(textBlock, text.Substring(foundIndex, highlightedText.Length), background, foreground);
          foundIndex = text.IndexOf(highlightedText, foundIndex + highlightedText.Length, StringComparison.OrdinalIgnoreCase);
          i += highlightedText.Length;
        }
      }
    }

    private static void AddPart(TextBlock textBlock, string part)
    {
      textBlock.Inlines.Add(new Run { Text = part });
    }

    private static void AddHighlightedPart(TextBlock textBlock, string part, SolidColorBrush highlightedTextBackground, SolidColorBrush highlightedTextForeground)
    {
      textBlock.Inlines.Add(new Run { Text = part, Background = highlightedTextBackground, Foreground = highlightedTextForeground });
    }
  }
}
