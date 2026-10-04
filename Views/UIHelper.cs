using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;

namespace PrimitiveTranslator.Views
{
    /// <summary>
    ///     Набор вспомогательных методов и данных.
    /// </summary>
    internal static class UIHelper
    {
        /// <summary>
        ///     Стандартный размер шрифта.
        /// </summary>
        internal static readonly double FontSizeDefault = 14;

        /// <summary>
        ///     Стандартное семейство шрифтов.
        /// </summary>
        internal static FontFamily FontFamilyDefault = new FontFamily("Consolas");

        /// <summary>
        ///     Стандартный фон.
        /// </summary>
        internal static readonly Brush BackgroundDefault = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        /// <summary>
        ///     Фон текстового поля.
        /// </summary>
        internal static readonly Brush BackgroundTextField = new SolidColorBrush(Color.FromRgb(40, 40, 40));
        /// <summary>
        ///     Фон выделенного элемента.
        /// </summary>
        internal static readonly Brush BackgroundSelected = new SolidColorBrush(Color.FromRgb(64, 64, 64));
        /// <summary>
        ///     Фон отключённого элемента.
        /// </summary>
        internal static readonly Brush BackgroundDisabled = new SolidColorBrush(Color.FromRgb(64, 64, 64));

        /// <summary>
        ///     Стандартный цвет текста.
        /// </summary>
        internal static readonly Brush ForegroundDefault = new SolidColorBrush(Color.FromRgb(230, 230, 230));

        /// <summary>
        ///     Стандартный цвет каретки.
        /// </summary>
        internal static readonly Brush CarretColorDefault = new SolidColorBrush(Color.FromRgb(220, 220, 220));

        /// <summary>
        ///     Обходит древо визуальных элементов и ищет первое вхождение элемента с типом <c><typeparamref name="T"/></c>.
        /// </summary>
        /// <typeparam name="T">
        ///     Тип искомого элемента.
        /// </typeparam>
        /// <param name="parent">
        ///     Родительский элемент.
        /// </param>
        /// <returns>
        ///     Дочерний элемент или <c>null</c>.
        /// </returns>
        internal static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            // Проверка аргументов
            if (parent is null)
                return null;
            
            // Обход древа
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); ++i)
            {
                // Получение дочернего элемента
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                    return typed;

                // Поиск внутри дочернего элемента
                T? result = FindVisualChild<T>(child);
                if (result is not null)
                    return result;
            }

            return null;
        }
        /// <summary>
        ///     Обходит древо визуальных элементов и ищет все элементы с типом <c><typeparamref name="T"/></c>.
        /// </summary>
        /// <typeparam name="T">
        ///     Тип искомого элемента.
        /// </typeparam>
        /// <param name="parent">
        ///     Родительский элемент.
        /// </param>
        /// <param name="childs">
        ///     Список дочерних элементов.
        /// </param>
        internal static void FindVisualChilds<T>(DependencyObject? parent, [NotNull] ref List<T>? childs) where T : DependencyObject
        {
            // Инициализация
            childs ??= new List<T>();

            // Проверка аргументов
            if (parent is null)
                return;

            // Обход древа
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                // Получение дочернего элемента
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                    childs.Add(typed);

                // Поиск внутри дочернего элемента
                FindVisualChilds(child, ref childs);
            }
        }
    }
}