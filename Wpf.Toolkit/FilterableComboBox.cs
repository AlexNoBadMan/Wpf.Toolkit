using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Wpf.Toolkit
{
    public class FilterableComboBox : ComboBox
    {
        private static readonly DependencyProperty FilterProperty = DependencyProperty.Register(nameof(FilterProperty),
            typeof(string),
            typeof(FilterableComboBox),
            new PropertyMetadata(new PropertyChangedCallback(FilterableComboBox.FilterPropertyChangedCallback)));
        private TextBox _EditableTextBox;

        public FilterableComboBox()
        {
            IsEditable = true;
            StaysOpenOnEdit = true;
            IsTextSearchEnabled = false;
            DropDownOpened += new EventHandler(OnFilterableComboBoxDropDownOpened);
            DropDownClosed += new EventHandler(OnDropDownClosed);
            LostKeyboardFocus += new KeyboardFocusChangedEventHandler(OnLostKeyboardFocus);
            GotFocus += new RoutedEventHandler(OnGotKeyboardFocus);
            Binding binding = new Binding()
            {
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
                Path = new PropertyPath("Text", Array.Empty<object>())
            };
            SetBinding(FilterableComboBox.FilterProperty, (BindingBase)binding);
        }

        private string Filter
        {
            get => (string)GetValue(FilterableComboBox.FilterProperty);
            set => SetValue(FilterableComboBox.FilterProperty, (object)value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _EditableTextBox = GetTemplateChild("PART_EditableTextBox") as TextBox;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!IsDropDownOpen && (e.Key == Key.Down || e.Key == Key.Up))
                e.Handled = true;
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                base.OnPreviewKeyDown(e);
                if (Items.Count <= 0 || SelectedIndex != -1 && !(GetText(SelectedItem) != Text))
                    return;
                SelectedIndex = GetNextIndex(SelectedIndex, Items.Count, e.Key);
            }
            else
                base.OnPreviewKeyDown(e);
        }

        protected virtual void OnSearchTextPropertyChanged(string oldValue, string newValue)
        {
            var text = Text ?? string.Empty;
            if (SelectedItem != null && GetText(SelectedItem) == text)
                return;

            if (IsDropDownOpen)
                Items.Filter = new Predicate<object>(Contains);

            if (IsDropDownOpen || string.IsNullOrEmpty(text))
                return;

            IsDropDownOpen = true;
        }

        private static void FilterPropertyChangedCallback(
          DependencyObject d,
          DependencyPropertyChangedEventArgs e)
        {
            ((FilterableComboBox)d).OnSearchTextPropertyChanged(e.OldValue as string, e.NewValue as string);
        }

        private void OnDropDownClosed(object sender, EventArgs e)
        {
            _EditableTextBox.CaretIndex = 0;
            _EditableTextBox.SelectionLength = 0;
            Items.Filter = null;
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Binding binding = new Binding()
            {
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
                Path = new PropertyPath(string.Format("{0}.{1}", (object)"SelectedValue", GetValue(TextSearch.TextPathProperty)), Array.Empty<object>())
            };
            SetBinding(ComboBox.TextProperty, (BindingBase)binding);
        }

        private void OnGotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding((DependencyObject)this, ComboBox.TextProperty);
            var selectedItem = SelectedItem;
            if (selectedItem == null)
                return;

            var text = GetText(selectedItem);
            if (!(Text != text))
                return;

            SetCurrentValue(ComboBox.TextProperty, (object)text);
        }

        private void OnFilterableComboBoxDropDownOpened(object sender, EventArgs e)
        {
            if (_EditableTextBox == null)
                return;
            _EditableTextBox.SelectionStart = _EditableTextBox.Text.Length;
            string text = _EditableTextBox.Text;
            if (SelectedItem != null && GetText(SelectedItem) == text)
                Items.Filter = null;
            else
                Items.Filter = new Predicate<object>(Contains);
        }

        private int GetNextIndex(int selectedIndex, int count, Key key)
        {
            if (count < 1)
                return -1;

            if (selectedIndex == -1)
                return 0;

            return key switch
            {
                Key.Down when selectedIndex < count - 1 => ++selectedIndex,
                Key.Up when selectedIndex > 0 => --selectedIndex,
                _ => selectedIndex,
            };
        }

        private bool Contains(object item)
        {
            var text = Text;
            return string.IsNullOrEmpty(text) || item == SelectedItem || GetText(item).ToUpper().Contains(text.ToUpper());
        }

        private string GetText(object item)
        {
            var result = string.Empty;
            if (item != null)
            {
                string text = (string)GetValue(TextSearch.TextPathProperty);
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
