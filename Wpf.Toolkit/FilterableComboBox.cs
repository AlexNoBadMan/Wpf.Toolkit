using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Wpf.Toolkit
{
    public class FilterableComboBox : WatermarkedComboBox
    {
        private bool isLoaded;
        private TextBox _editableTextBox;
        private int _caretIndex;
        private bool _isNeedProcessSelection;
        private static readonly DependencyProperty FilterProperty = DependencyProperty.Register(nameof(FilterProperty), typeof(string), typeof(FilterableComboBox), new PropertyMetadata(new PropertyChangedCallback(FilterPropertyChangedCallback)));

        public bool AllowFreeText { get => (bool)GetValue(AllowFreeTextProperty); set => SetValue(AllowFreeTextProperty, value); }

        public static readonly DependencyProperty AllowFreeTextProperty = DependencyProperty.Register("AllowFreeText", typeof(bool), typeof(FilterableComboBox), new UIPropertyMetadata(false, null));

        static FilterableComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FilterableComboBox), new FrameworkPropertyMetadata(typeof(FilterableComboBox)));
        }

        public FilterableComboBox()
        {
            IsEditable = true;
            StaysOpenOnEdit = true;
            IsTextSearchEnabled = false;
            Loaded += FilterableComboBox_Loaded;
        }

        private void FilterableComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (!AllowFreeText)
            {
                GotFocus += new RoutedEventHandler(OnGotKeyboardFocus);
                DropDownOpened += new EventHandler(OnDropDownOpened);
                //DropDownClosed += new EventHandler(OnDropDownClosed);
                LostKeyboardFocus += new KeyboardFocusChangedEventHandler(OnLostKeyboardFocus);
            }
            else
            {
                DropDownOpened += new EventHandler(OnAllowFreeTextDropDownOpened);
            }
            var binding = new Binding()
            {
                Mode = BindingMode.OneWay,
                Delay = 300,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
                Path = new PropertyPath("Text", Array.Empty<object>())
            };
            SetBinding(FilterProperty, binding);
            isLoaded = true;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _editableTextBox = GetTemplateChild("PART_EditableTextBox") as TextBox;

            _editableTextBox.SelectionChanged += TextBox_SelectionChanged;
            SelectionChanged += ComboBox_SelectionChanged;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isNeedProcessSelection)
            {
                e.Handled = true;
                _isNeedProcessSelection = false;
                System.Diagnostics.Debug.WriteLine($"ComboBox Selection changed Handled slen {_editableTextBox.SelectionLength}");
            }
            System.Diagnostics.Debug.WriteLine($"ComboBox Selection changed slen {_editableTextBox.SelectionLength}");
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isNeedProcessSelection)
            {
                e.Handled = true;
                _isNeedProcessSelection = !_isNeedProcessSelection;
                _editableTextBox.CaretIndex = _caretIndex == _editableTextBox.Text.Length ? _caretIndex : _caretIndex + 1;
                System.Diagnostics.Debug.WriteLine($"Selection changed Handled slen {_editableTextBox.SelectionLength}");

            }
            else
            {
                _caretIndex = _editableTextBox.CaretIndex;
            }
            //if (txt.SelectionLength == 0 && txt.CaretIndex != 0)
            //{
            //    _caretIndex = txt.CaretIndex;
            //    System.Diagnostics.Debug.WriteLine($"Selection changed Handled slen {_editableTextBox.SelectionLength}");
            //}

            //if (_isDropDownOpen)
            //{
            //    e.Handled = true;
            //    _isDropDownOpen = false;
            //    System.Diagnostics.Debug.WriteLine($"Selection changed Handled slen {_editableTextBox.SelectionLength}");
            //}
            System.Diagnostics.Debug.WriteLine($"Selection changed slen {_editableTextBox.SelectionLength}");
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!IsDropDownOpen && (e.Key == Key.Down || e.Key == Key.Up))
            {
                e.Handled = true;
            }
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                base.OnPreviewKeyDown(e);
                if (Items.Count <= 0 || SelectedIndex != -1 && !(GetText(SelectedItem) != Text))
                    return;

                SelectedIndex = GetNextIndex(SelectedIndex, Items.Count, e.Key);
            }
            else
            {
                base.OnPreviewKeyDown(e);
            }
        }

        protected virtual void OnSearchTextPropertyChanged(string oldValue, string newValue)
        {
            if (!isLoaded || oldValue == newValue)
                return;

            if (!IsKeyboardFocusWithin)
            {
                CheckSelectedItemText();
                return;
            }

            var text = Text ?? string.Empty;
            if (text == string.Empty)
            {
                SelectedItem = null;
                Items.Filter = null;
                if (!IsDropDownOpen)
                    IsDropDownOpen = true;
                return;
            }

            if (SelectedItem != null && GetText(SelectedItem) == text)
                return;

            if (IsDropDownOpen)
            {
                Items.Filter = new Predicate<object>(Contains);
                return;
            }
            
            _isNeedProcessSelection = true;
            IsDropDownOpen = true;
        }

        private static void FilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FilterableComboBox)d).OnSearchTextPropertyChanged(e.OldValue as string, e.NewValue as string);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CheckSelectedItemText();
        }

        private void OnGotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            CheckSelectedItemText();
        }

        private void CheckSelectedItemText()
        {
            var selectedItem = SelectedItem;
            if (selectedItem == null)
                return;

            var text = GetText(selectedItem);
            if (!(Text != text))
                return;

            SetCurrentValue(TextProperty, text);
        }
        private void OnDropDownClosed(object sender, EventArgs e)
        {
            if (Items.Filter != null)
                Items.Filter = null;
        }

        private void OnAllowFreeTextDropDownOpened(object sender, EventArgs e)
        {
            if (Items.Filter != null)
                Items.Filter = null;
        }

        private void OnDropDownOpened(object sender, EventArgs e)
        {
            var text = _editableTextBox.Text;
            Items.Filter = SelectedItem != null && GetText(SelectedItem) == text ? null : new Predicate<object>(Contains);
        }

        private int GetNextIndex(int selectedIndex, int count, Key key)
        {
            if (count < 1)
            {
                return -1;
            }
            if (selectedIndex == -1)
            {
                return 0;
            }
            if (key == Key.Down)
            {
                if (selectedIndex < count - 1)
                {
                    return ++selectedIndex;
                }
            }
            else if (key == Key.Up && selectedIndex > 0)
            {
                return --selectedIndex;
            }
            return selectedIndex;
        }

        private bool Contains(object item)
        {
            var text = Text;
            return string.IsNullOrEmpty(text) || (!AllowFreeText && item == SelectedItem) || GetText(item).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string GetText(object item)
        {
            var result = string.Empty;
            if (item != null)
            {
                var text = (string)GetValue(DisplayMemberPathProperty);
                if (string.IsNullOrWhiteSpace(text))
                {
                    result = item.ToString();
                }
                else
                {
                    PropertyInfo property = item.GetType().GetProperty(text);
                    result = (string)(property?.GetValue(item, null));
                }
            }
            return result;
        }
    }
}
