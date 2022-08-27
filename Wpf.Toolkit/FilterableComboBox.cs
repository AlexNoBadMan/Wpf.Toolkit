using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Wpf.Toolkit
{
    public class FilterableComboBox : WatermarkedComboBox
    {
        private bool _isLoaded;
        private TextBox _editableTextBox;
        private int _caretIndex;
        private bool _isNeedProcessSelection;
        private DispatcherTimer? _deferFilterEvaluationTimer;

        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(nameof(SearchTextProperty), typeof(string), typeof(FilterableComboBox), new PropertyMetadata(new PropertyChangedCallback(FilterPropertyChangedCallback)));
        public string SearchText //можно использовать для подсвечивания
        {
            get => (string)GetValue(SearchTextProperty);
        }

        public static readonly DependencyProperty IsAddExternalContainerModeProperty = DependencyProperty.Register(nameof(IsAddExternalContainerMode), typeof(bool), typeof(FilterableComboBox), new UIPropertyMetadata(false, null));
        public bool IsAddExternalContainerMode
        {
            get => (bool)GetValue(IsAddExternalContainerModeProperty);
            set => SetValue(IsAddExternalContainerModeProperty, value);
        }


        public static readonly DependencyProperty AllowFreeTextProperty = DependencyProperty.Register(nameof(AllowFreeText), typeof(bool), typeof(FilterableComboBox), new UIPropertyMetadata(false, null));
        public bool AllowFreeText
        {
            get => (bool)GetValue(AllowFreeTextProperty);
            set => SetValue(AllowFreeTextProperty, value);
        }


        public static readonly DependencyProperty AutoSelectItemProperty = DependencyProperty.Register(nameof(AutoSelectItem), typeof(bool), typeof(FilterableComboBox), new UIPropertyMetadata(false, null));
        public bool AutoSelectItem
        {
            get => (bool)GetValue(AutoSelectItemProperty);
            set => SetValue(AutoSelectItemProperty, value);
        }


        public static readonly DependencyProperty FilterDelayProperty = DependencyProperty.Register(nameof(FilterDelay), typeof(int), typeof(FilterableComboBox), new UIPropertyMetadata(0, null));
        public int FilterDelay
        {
            get => (int)GetValue(FilterDelayProperty);
            set => SetValue(FilterDelayProperty, value);
        }


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
            if (IsAddExternalContainerMode)
            {
                AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(ComboBoxItem_MouseLeftButtonUp), true);
                DropDownOpened += new EventHandler(OnDropDownOpened);
                DropDownClosed += new EventHandler(OnAddExternalContainerModeDropDownClosed);
            }
            else if (AllowFreeText)
            {
                DropDownOpened += new EventHandler(OnAllowFreeTextDropDownOpened);
            }
            else
            {
                //GotFocus += new RoutedEventHandler(OnGotKeyboardFocus);
                DropDownOpened += new EventHandler(OnDropDownOpened);
                LostKeyboardFocus += new KeyboardFocusChangedEventHandler(OnLostKeyboardFocus);
            }
            var binding = new Binding()
            {
                Mode = BindingMode.OneWay,
                Delay = FilterDelay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
                Path = new PropertyPath("Text", Array.Empty<object>())
            };
            SetBinding(SearchTextProperty, binding);
            _isLoaded = true;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _editableTextBox = GetTemplateChild("PART_EditableTextBox") as TextBox;

            _editableTextBox.SelectionChanged += TextBox_SelectionChanged;
        }

        private void OnAddExternalContainerModeDropDownClosed(object sender, EventArgs e)
        {
            SelectedIndex = -1;
        }

        private void ComboBoxItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SelectedIndex == -1)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                GetBindingExpression(SelectedItemProperty).UpdateSource();
                SelectedIndex = -1;
            }), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isNeedProcessSelection) //При открытии popup происходит не отключаемое выделение всего текста, сбрасываем выделение и ставим каретку в прежнее положение
            {
                _isNeedProcessSelection = !_isNeedProcessSelection;
                _editableTextBox.CaretIndex = _caretIndex == _editableTextBox.Text.Length ? _caretIndex : _caretIndex + 1;
            }
            else
            {
                _caretIndex = _editableTextBox.CaretIndex; //запоминаем положение каретки
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!IsDropDownOpen && (e.Key == Key.Down || e.Key == Key.Up))
            {
                e.Handled = true;
                IsDropDownOpen = true;
            }
            else if (IsDropDownOpen && e.Key == Key.F4) //Если не обработать то при нажатии F4 будет задан SelectedItem который был выбран в последний раз
            {
                e.Handled = true;
                IsDropDownOpen = false;
            }
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (AllowFreeText)
                {
                    SelectedIndex = GetNextIndex(SelectedIndex, Items.Count, e.Key);//Может так удобнее будет?
                    _editableTextBox.CaretIndex = _editableTextBox.Text.Length;
                }
                else if (Items.Count > 0)
                {
                    e.Handled = true;//TextBox.SelectAll() -Не работает без указания Handled = true
                    SelectedIndex = GetNextIndex(SelectedIndex, Items.Count, e.Key);//Странный эффект если SelectedIndex == -1 то нажатие Key.Down прокручивает в конец списка
                }
                else
                {
                    base.OnPreviewKeyDown(e);
                }
            }
            else if (IsAddExternalContainerMode && e.Key == Key.Enter)
            {
                GetBindingExpression(SelectedItemProperty).UpdateSource();
                SelectedIndex = -1;
            }
            else
            {
                base.OnPreviewKeyDown(e);
            }
        }

        Dispatcher dispatcher;
        protected virtual void OnSearchTextPropertyChanged(string oldValue, string newValue)
        {
            //System.Diagnostics.Debug.WriteLine("2. Tch");
            //if (_deferFilterEvaluationTimer is null)
            //{
            //    _deferFilterEvaluationTimer = new DispatcherTimer(DispatcherPriority.Input, Dispatcher.CurrentDispatcher);
            //    _deferFilterEvaluationTimer.Interval = TimeSpan.FromMilliseconds(FilterDelay);
            //    _deferFilterEvaluationTimer.Tick += (_, _) => EvaluateFilter();
            //}

            //if (_deferFilterEvaluationTimer.IsEnabled)
            //    _deferFilterEvaluationTimer.Stop();

            if (!_isLoaded || oldValue == newValue || !IsKeyboardFocusWithin)
                return;

            var text = Text ?? string.Empty;
            if (text == string.Empty)
            {
                Items.Filter = null;
                //В режиме IsAddExternalContainerMode, при событии DropDownClosed происходит SelectedIndex = -1, 
                //если попытаться при этом сделать IsDropDownOpen = true, будет ошибка открытия списка из события DropDownClosed
                if (!IsAddExternalContainerMode)
                {
                    SelectedIndex = -1;
                    if (!IsDropDownOpen)
                        IsDropDownOpen = true;
                }
                return;
            }
            EvaluateFilter();
            //Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => { Items.Filter = Contains; }));

            //System.Diagnostics.Debug.WriteLine("3. Перед старт EvaluateFilter");
            //_deferFilterEvaluationTimer.Start();
        }

        private void EvaluateFilter()
        {
            _deferFilterEvaluationTimer?.Stop();

            var text = Text;
            //System.Diagnostics.Debug.WriteLine($"4. Старт EvaluateFilter {text}");
            if (SelectedIndex != -1 && GetText(SelectedItem) == text)
                return;

            if (IsDropDownOpen)
            {
                //System.Diagnostics.Debug.WriteLine($"5. перед Filter {text}");
                //Dispatcher.CurrentDispatcher.Invoke(new Action(() => { Items.Filter = Contains; }));
                //var uiDispatcher = System.Windows.Application.Current.Dispatcher;
                //uiDispatcher.Invoke(new Action(() => Items.Filter = Contains));
                Items.Filter = Contains;
                //System.Diagnostics.Debug.WriteLine($"6. после Filter {text}");
            }
            else
            {
                _isNeedProcessSelection = true;
                IsDropDownOpen = true;
            }

            if (AutoSelectItem && Items.Count > 1)
            {
                foreach (var item in Items)
                {// в методе Contains при !AllowFreeText в списке всегда будет отображаться текущий SelectedItem даже если он не подходит по условию поиска
                    if (GetText(item).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        SelectedItem = item;
                        break;
                    }
                }
            }
            //System.Diagnostics.Debug.WriteLine($"7. Конец EvaluateFilter {text}");
        }

        private static void FilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"1. Main {e.OldValue} - {e.NewValue}");
            ((FilterableComboBox)d).OnSearchTextPropertyChanged(e.OldValue as string, e.NewValue as string);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CheckSelectedItemText();
        }

        //protected override void OnGotFocus(RoutedEventArgs e)
        //{
        //    System.Diagnostics.Debug.WriteLine($"before override OnGotFocus {SelectedItem}");
        //    base.OnGotFocus(e);
        //    System.Diagnostics.Debug.WriteLine($"after override OnGotFocus {SelectedItem}");
        //}

        //protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        //{
        //    System.Diagnostics.Debug.WriteLine($"before override OnGotFocus {SelectedItem}");
        //    base.OnIsKeyboardFocusWithinChanged(e);
        //    System.Diagnostics.Debug.WriteLine($"after override OnGotFocus {SelectedItem}");
        //}
        //protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        //{

        //    System.Diagnostics.Debug.WriteLine($"before override OnGotKeyboardFocus {SelectedItem}");
        //    base.OnGotKeyboardFocus(e);
        //    System.Diagnostics.Debug.WriteLine($"after override OnGotKeyboardFocus {SelectedItem}");

        //}

        //private void OnGotKeyboardFocus(object sender, RoutedEventArgs e)
        //{
        //    //CheckSelectedItemText();
        //    System.Diagnostics.Debug.WriteLine($"OnGotKeyboardFocus {SelectedItem}");
        //}

        private void CheckSelectedItemText()
        {
            var selectedItem = SelectedItem;
            if (selectedItem == null)
            {
                if (!AllowFreeText)
                {
                    if (AutoSelectItem && Items.Count == 1)       //Обрабатываем ситуацию когда после потери фокуса не был выбран элемент но согласно фильтру остался один,
                        SelectedItem = Items[0];//не имеет смысла когда исходная коллекция пуста или состоит из одного элемента
                    else
                        SetCurrentValue(TextProperty, string.Empty);
                }
                return;
            }

            var text = GetText(selectedItem);
            if (!(Text != text))
                return;

            SetCurrentValue(TextProperty, text);
        }

        private void OnAllowFreeTextDropDownOpened(object sender, EventArgs e)
        {
            if (Items.Filter != null)
                Items.Filter = null;
        }

        private void OnDropDownOpened(object sender, EventArgs e)
        {
            var text = _editableTextBox.Text;
            Items.Filter = SelectedItem != null && GetText(SelectedItem) == text ? null : Contains;
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
            var text = Text;//чтобы не потерять SelectedItem при !AllowFreeText в списке всегда будет отображаться даже если он не подходит по условию поиска
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
                    result = (string)(item.GetType().GetProperty(text)?.GetValue(item, null));
                }
            }
            return result;
        }
    }
}
