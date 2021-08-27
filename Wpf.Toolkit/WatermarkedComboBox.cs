using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Wpf.Toolkit
{
    public class WatermarkedComboBox : ComboBox
    {
        public string WatermarkText
        {
            get { return (string)GetValue(WatermarkTextProperty); }
            set { SetValue(WatermarkTextProperty, value); }
        }

        public static readonly DependencyProperty WatermarkTextProperty =
            DependencyProperty.Register("WatermarkText", typeof(string), typeof(WatermarkedComboBox), new UIPropertyMetadata(null));
        
        public Brush WatermarkForeground
        {
            get { return (Brush)GetValue(WatermarkForegroundProperty); }
            set { SetValue(WatermarkForegroundProperty, value); }
        }

        public static readonly DependencyProperty WatermarkForegroundProperty =
            DependencyProperty.Register("WatermarkForeground", typeof(Brush), typeof(WatermarkedComboBox), new UIPropertyMetadata(Brushes.Gray, null));

        static WatermarkedComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WatermarkedComboBox), new FrameworkPropertyMetadata(typeof(WatermarkedComboBox)));
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Down && !IsDropDownOpen)
            {
                IsDropDownOpen = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }
    }
}
