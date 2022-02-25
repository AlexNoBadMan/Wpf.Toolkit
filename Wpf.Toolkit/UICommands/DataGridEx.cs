using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Wpf.Toolkit.UICommands
{
    public static class DataGridEx
    {
        private static readonly RoutedUICommand _moveItemUpCommand = new RoutedUICommand("Переместить вверх", nameof(MoveItemUpCommand), typeof(DataGrid), new InputGestureCollection() { new KeyGesture(Key.Up, ModifierKeys.Alt) });
        private static readonly RoutedUICommand _moveItemDownCommand = new RoutedUICommand("Переместить вниз", nameof(MoveItemDownCommand), typeof(DataGrid), new InputGestureCollection() { new KeyGesture(Key.Down, ModifierKeys.Alt) });

        public static RoutedUICommand MoveItemUpCommand => _moveItemUpCommand;
        public static RoutedUICommand MoveItemDownCommand => _moveItemDownCommand;
        static DataGridEx()
        {
            var typeFromHandle = typeof(DataGrid);
            CommandManager.RegisterClassCommandBinding(typeFromHandle, new CommandBinding(MoveItemUpCommand, OnExecutedMoveItemUp, OnCanExecuteMoveItemUp));
            CommandManager.RegisterClassCommandBinding(typeFromHandle, new CommandBinding(MoveItemDownCommand, OnExecutedMoveItemDown, OnCanExecuteMoveItemDown));
        }

        private static bool OnCanExecuteMoveItem(DataGrid dataGrid)
        {
            return !dataGrid.IsReadOnly && (dataGrid.ItemsSource.IsObservableCollection() || dataGrid.Items.IObservableCollectionInGroup()) && !((IEditableCollectionView)dataGrid.Items).IsEditingItem;
        }

        private static void OnCanExecuteMoveItemUp(object sender, CanExecuteRoutedEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            e.CanExecute = dataGrid.SelectedIndex > 0 && OnCanExecuteMoveItem(dataGrid) && !GetSelectedItems(dataGrid).Any(x => dataGrid.Items.IndexOf(x) == 0);
            e.Handled = true;
        }

        private static void OnExecutedMoveItemUp(object sender, ExecutedRoutedEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            //MoveItem(dataGrid, dataGrid.GetSelectedItemsIndices().OrderBy(x => x).ToArray(), MoveUp);
            var indices = dataGrid.GetSelectedItemsIndices().OrderBy(x => x).ToArray();
            var isGrouped = !dataGrid.ItemsSource.IsObservableCollection();
            var sourceCollection = isGrouped ? (IList)((ICollectionView)dataGrid.Items.SourceCollection).SourceCollection : (IList)dataGrid.ItemsSource;
            var method = sourceCollection.GetType().GetMethod("Move", BindingFlags.Instance | BindingFlags.Public);
            if (isGrouped && dataGrid.Items.Groups.Count > 1)
            {
                var needsRefresh = false;
                for (int i = 0; i < indices.Length; i++)
                {
                    var sourceIndex = indices[i];
                    var targetIndex = sourceIndex - 1;
                    if (TryMoveToAnotherGroup(dataGrid.Items, sourceIndex, targetIndex))
                    {
                        needsRefresh = true;
                        continue;
                    }
                    if (sourceIndex > 0)
                        method.Invoke(sourceCollection, new object[] { sourceIndex, targetIndex });
                }
                if (needsRefresh)
                    ((ICollectionView)dataGrid.Items.SourceCollection).Refresh();
            }
            else
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    var sourceIndex = indices[i];
                    var targetIndex = sourceIndex - 1;
                    if (sourceIndex > 0)
                        method.Invoke(sourceCollection, new object[] { sourceIndex, targetIndex });
                }
            }

            if (dataGrid.IsKeyboardFocusWithin)//Попытка вернуть фокус на ячейку если была сфокусирована
                dataGrid.Dispatcher.BeginInvoke(new Action(() => FocusCell(dataGrid)), DispatcherPriority.Background);
            e.Handled = true;

        }

        private static void OnCanExecuteMoveItemDown(object sender, CanExecuteRoutedEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            e.CanExecute = dataGrid.SelectedIndex != -1 && OnCanExecuteMoveItem(dataGrid) && !GetSelectedItems(dataGrid).Any(x => dataGrid.Items.IndexOf(x) == dataGrid.Items.Count - 1);
            e.Handled = true;
        }

        private static void OnExecutedMoveItemDown(object sender, ExecutedRoutedEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            var indices = dataGrid.GetSelectedItemsIndices().OrderByDescending(x => x).ToArray();
            var isGrouped = !dataGrid.ItemsSource.IsObservableCollection();
            var sourceCollection = isGrouped ? (IList)((ICollectionView)dataGrid.Items.SourceCollection).SourceCollection : (IList)dataGrid.ItemsSource;
            var method = sourceCollection.GetType().GetMethod("Move", BindingFlags.Instance | BindingFlags.Public);
            if (isGrouped && dataGrid.Items.Groups.Count > 1)
            {
                var needsRefresh = false;
                for (int i = 0; i < indices.Length; i++)
                {
                    var sourceIndex = indices[i];
                    var targetIndex = sourceIndex + 1;
                    if (TryMoveToAnotherGroup(dataGrid.Items, sourceIndex, targetIndex))
                    {
                        needsRefresh = true;
                        continue;
                    }
                    if (sourceIndex < sourceCollection.Count - 1)
                        method.Invoke(sourceCollection, new object[] { sourceIndex, targetIndex });
                }
                if (needsRefresh)
                    ((ICollectionView)dataGrid.Items.SourceCollection).Refresh();
            }
            else
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    var sourceIndex = indices[i];
                    var targetIndex = sourceIndex + 1;
                    if (sourceIndex < sourceCollection.Count - 1)
                        method.Invoke(sourceCollection, new object[] { sourceIndex, targetIndex });
                }
            }

            if (dataGrid.IsKeyboardFocusWithin)//Попытка вернуть фокус на ячейку если была сфокусирована
                dataGrid.Dispatcher.BeginInvoke(new Action(() => FocusCell(dataGrid)), DispatcherPriority.Background);
            //MoveItem(dataGrid, dataGrid.GetSelectedItemsIndices().OrderByDescending(x => x).ToArray(), MoveDown);
            e.Handled = true;
        }

        private static void MoveItem(DataGrid dataGrid, int[] indices, Action<IList, MethodInfo, int> move)
        {
            var isGrouped = !dataGrid.ItemsSource.IsObservableCollection();
            var sourceCollection = isGrouped ? (IList)((ICollectionView)dataGrid.Items.SourceCollection).SourceCollection : (IList)dataGrid.ItemsSource;
            var method = sourceCollection.GetType().GetMethod("Move", BindingFlags.Instance | BindingFlags.Public);
            if (isGrouped)
            {

            }
            else
            {
                for (var i = 0; i < indices.Length; i++)
                    move(sourceCollection, method, indices[i]);
            }

            if (dataGrid.IsKeyboardFocusWithin)//Попытка вернуть фокус на ячейку если была сфокусирована
                dataGrid.Dispatcher.BeginInvoke(new Action(() => FocusCell(dataGrid)), DispatcherPriority.Background);
        }

        private static void MoveUp(IList sourceCollection, MethodInfo method, int sourceIndex)
        {
            if (sourceIndex > 0)
                method.Invoke(sourceCollection, new object[] { sourceIndex, sourceIndex - 1 });
        }

        private static void MoveDown(IList sourceCollection, MethodInfo method, int sourceIndex)
        {
            if (sourceIndex < sourceCollection.Count - 1)
                method.Invoke(sourceCollection, new object[] { sourceIndex, sourceIndex + 1 });
        }

        private static bool TryMoveToAnotherGroup(ItemCollection items, int sourceIndex, int targetIndex)
        {
            var sourceItem = items[sourceIndex];
            var targetItem = items[targetIndex];
            var flag = false;
            foreach (var item in items.GroupDescriptions.OfType<PropertyGroupDescription>())
            {
                var sourceItemProperty = sourceItem.GetType().GetProperty(item.PropertyName);
                var targetItemPropertyValue = targetItem.GetType().GetProperty(item.PropertyName).GetValue(targetItem);
                if (!sourceItemProperty.GetValue(sourceItem).Equals(targetItemPropertyValue))
                {
                    sourceItemProperty.SetValue(sourceItem, targetItemPropertyValue);
                    flag = true;
                }
            }
            return flag;
        }

        private static void FocusCell(DataGrid dataGrid)
        {
            var dataGridRow = dataGrid.ItemContainerGenerator.ContainerFromIndex(dataGrid.SelectedIndex) as DataGridRow;
            if (dataGridRow is null)
                return;

            if (dataGrid.CurrentColumn.GetCellContent(dataGridRow)?.Parent is DataGridCell cell)
                cell.Focus();
        }

        private static IEnumerable<int> GetSelectedItemsIndices(this DataGrid dataGrid)
        {
            return dataGrid.SelectedItems.OfType<object>().Select(x => dataGrid.Items.IndexOf(x));
        }

        private static IEnumerable<object> GetSelectedItems(this DataGrid dataGrid)
        {
            return dataGrid.SelectedItems.OfType<object>();
        }

        private static bool IObservableCollectionInGroup(this ItemCollection collection)
        {
            return collection?.SourceCollection is ICollectionView collectionView && collectionView.SourceCollection.IsObservableCollection();
        }

        /// <summary>
        /// Checks if the given collection is a ObservableCollection&lt;&gt;
        /// </summary>
        /// <param name="collection">The collection to test.</param>
        /// <returns>True if the collection is a ObservableCollection&lt;&gt;</returns>
        public static bool IsObservableCollection(this IEnumerable collection)
        {
            return collection != null
                   && collection.GetType().IsGenericType
                   && collection.GetType().GetGenericTypeDefinition() == typeof(ObservableCollection<>);
        }
    }
}