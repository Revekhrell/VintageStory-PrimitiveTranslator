using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;

namespace PrimitiveTranslator.Editors.Highlightning
{
    /// <summary>
    ///     Окрашиватель ключей.
    /// </summary>
    public sealed class KeyHighlightTransformer : DocumentColorizingTransformer
    {
        /// <summary>
        ///     Цвет подсветки нового ключа.
        /// </summary>
        public static readonly Brush NewKeyColor;
        /// <summary>
        ///     Цвет посдветки модифицированного ключа.
        /// </summary>
        public static readonly Brush ModifiedKeyColor;

        /// <summary>
        ///     Диспатчер потока.
        /// </summary>
        private readonly Dispatcher Dispatcher;
        /// <summary>
        ///     Диапазоны.
        /// </summary>
        private readonly SortedSet<HighlightRange> Ranges;

        /// <summary>
        ///     Инициализатор.
        /// </summary>
        static KeyHighlightTransformer()
        {
            NewKeyColor = new SolidColorBrush(Color.FromRgb(80, 165, 74));
            ModifiedKeyColor = new SolidColorBrush(Color.FromRgb(222, 85, 10));
        }
        /// <summary>
        ///     Инициализирует новый экземпляр окрашивателя.
        /// </summary>
        public KeyHighlightTransformer()
        {
            Dispatcher = Dispatcher.CurrentDispatcher;
            Ranges = new SortedSet<HighlightRange>(Comparer<HighlightRange>.Default);
        }

        /// <summary>
        ///     Регистрирует новый диапазон.
        /// </summary>
        /// <param name="range">
        ///     Диапазон.
        /// </param>
        public void RegisterRange(HighlightRange range)
        {
            if (object.ReferenceEquals(Dispatcher, Dispatcher.CurrentDispatcher))
                Ranges.Add(range);
            else
                Dispatcher.Invoke(() => Ranges.Add(range));
        }
        /// <summary>
        ///     Очищает настройки диапазонов.
        /// </summary>
        public void ClearRanges()
        {
            if (object.ReferenceEquals(Dispatcher, Dispatcher.CurrentDispatcher))
                Ranges.Clear();
            else
                Dispatcher.Invoke(Ranges.Clear);
        }

        /// <summary>
        ///     Окрашивает часть строки по зарегистрированным диапазонам.
        /// </summary>
        /// <param name="line">
        ///     Строка.
        /// </param>
        protected override void ColorizeLine(DocumentLine line)
        {
            // Проверка диапазонов
            if (Ranges.Count == 0)
                return;

            HighlightRange first = Ranges.First();
            HighlightRange last = Ranges.Last();
            int endOffset = last.Offset + last.Length;
            if (line.EndOffset < first.Offset || line.Offset > endOffset)
                return;

            // Поиск диапазона
            foreach (HighlightRange range in Ranges)
            {
                //  Проверка включения в строку
                endOffset = range.Offset + range.Length;
                if (range.Offset < line.Offset || endOffset > line.EndOffset)
                {
                    // Проверка перехода
                    if (range.Offset > line.EndOffset)
                        return;

                    continue;
                }

                // Окрашивание
                ChangeLinePart(range.Offset, endOffset, (VisualLineElement element) => element.TextRunProperties.SetForegroundBrush(range.Color));
            }
        }
    }
}