using Dragablz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using PrimitiveTranslator.Views;

namespace PrimitiveTranslator.Extensions
{
    /// <summary>
    ///     Методы расширения <c><see cref="TabablzControl"/></c>.
    /// </summary>
    public static class TabablzControlExtensions
    {
        /// <summary>
        ///     Словарь подписанных на событие элементов.
        /// </summary>
        private static readonly Dictionary<TabablzControl, List<DragablzItem>> RegisteredItems;

        /// <summary>
        ///     Стиль элемента прокрутки.
        /// </summary>
        private static Style? ScrollVieverStyle;
        /// <summary>
        ///     Стиль полосы прокрутки.
        /// </summary>
        private static Style? ScrollBarStyle;

        /// <summary>
        ///     Инициализатор.
        /// </summary>
        static TabablzControlExtensions()
        {
            RegisteredItems = new Dictionary<TabablzControl, List<DragablzItem>>();
        }

        /// <summary>
        ///     Получает элементы управления вкладок.
        /// </summary>
        /// <param name="control">
        ///     Элемент управления вкладками.
        /// </param>
        /// <returns>
        ///     Элементы управления вкладок.
        /// </returns>
        public static IEnumerable<DragablzItem> GetTabItemControls(this TabablzControl control)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo? itemsControlField = control?.GetType().GetField("_dragablzItemsControl", flags);
            DragablzItemsControl? itemsControl = itemsControlField?.GetValue(control) as DragablzItemsControl;
            MethodInfo? itemsMethod = itemsControl?.GetType()?.GetMethod("DragablzItems", flags);
            return itemsMethod?.Invoke(itemsControl, []) as IEnumerable<DragablzItem> ?? Array.Empty<DragablzItem>();
        }
        /// <summary>
        ///     Подписывает все вкладки на событие СКМ.
        /// </summary>
        /// <param name="control">
        ///     Элемент управления вкладками.
        /// </param>
        /// <param name="handler">
        ///     Обработчик события.
        /// </param>
        public static void FollowTabs(this TabablzControl control, MouseButtonEventHandler handler)
        {
            List<DragablzItem> items = control.GetTabItemControls().ToList();
            if (!RegisteredItems.TryGetValue(control, out List<DragablzItem>? registered))
            {
                registered = new List<DragablzItem>();
                RegisteredItems[control] = registered;
            }
            registered.RemoveAll(x => !items.Contains(x));
            foreach (DragablzItem item in items)
            {
                if (registered.Contains(item))
                    continue;

                item.PreviewMouseDown += handler;
                registered.Add(item);
            }
        }
        /// <summary>
        ///     Заменяет стиль заголовков вкладок.
        /// </summary>
        /// <param name="control">
        ///     Элемент управления вкладками.
        /// </param>
        public static void StyleTabHeaders(this TabablzControl control)
        {
            List<Border> borders = new List<Border>(2);
            IEnumerable<DragablzItem> items = control.GetTabItemControls();
            foreach (DragablzItem item in items)
            {
                borders.Clear();
                UIHelper.FindVisualChilds(item, ref borders);
                foreach (Border border in borders)
                    border.Background = item.IsSelected ? UIHelper.BackgroundDefault : UIHelper.BackgroundSelected;
            }
        }
        /// <summary>
        ///     Заменяет стиль элементов прокрутки.
        /// </summary>
        /// <param name="control">
        ///     Элемент управления вкладками.
        /// </param>
        public static void StyleTabViewers(this TabablzControl control)
        {
            DragablzItemsControl? items = UIHelper.FindVisualChild<DragablzItemsControl>(control);
            ScrollViewer? viewer = UIHelper.FindVisualChild<ScrollViewer>(items);
            ScrollVieverStyle ??= Application.Current.TryFindResource("ScrollViewerStyle") as Style;
            if (viewer is not null && ScrollVieverStyle is not null)
            {
                viewer.Style = ScrollVieverStyle;
                viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                viewer.Background = UIHelper.BackgroundDefault;

                ScrollBarStyle ??= Application.Current.TryFindResource("ScrollBarStyle") as Style;
                if (ScrollBarStyle is not null)
                {
                    List<ScrollBar> bars = new List<ScrollBar>(2);
                    UIHelper.FindVisualChilds(viewer, ref bars);
                    foreach (ScrollBar bar in bars)
                        bar.Style = ScrollBarStyle;
                }
            }
        }

        /// <summary>
        ///     Устанавливает режим отображения всем дочерним элементам типа <c><typeparamref name="T"/></c>.
        /// </summary>
        /// <typeparam name="T">
        ///     Тип элемента.
        /// </typeparam>
        /// <param name="control">
        ///     Элемент управления вкладками.
        /// </param>
        /// <param name="visibility">
        ///     Режим отображения.
        /// </param>
        public static void SetVisibility<T>(this TabablzControl control, Visibility visibility) where T : UIElement
        {
            List<T> elements = new List<T>(1);
            UIHelper.FindVisualChilds(control, ref elements);
            foreach (T element in elements)
                element.Visibility = visibility;
        }
    }
}