using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;

namespace PrimitiveTranslator.Editors.Highlightning
{
    /// <summary>
    ///     Диапазон подсветки.
    /// </summary>
    public readonly struct HighlightRange : IEquatable<HighlightRange>, IComparable<HighlightRange>
    {
        /// <summary>
        ///     Смещение.
        /// </summary>
        public readonly int Offset;
        /// <summary>
        ///     Длина.
        /// </summary>
        public readonly int Length;
        /// <summary>
        ///     Цвет текста.
        /// </summary>
        public readonly Brush Color;

        /// <summary>
        ///     Инициализирует новый экземпляр диапазона.
        /// </summary>
        /// <param name="offset">
        ///     Смещение.
        /// </param>
        /// <param name="length">
        ///     Длина.
        /// </param>
        /// <param name="color">
        ///     Цвет текста.
        /// </param>
        public HighlightRange(int offset, int length, Brush color)
        {
            Offset = offset;
            Length = length;
            Color = color;
        }

        /// <summary>
        ///     Сравнивает объект с <c><paramref name="other"/></c>.
        /// </summary>
        /// <param name="other">
        ///     Объект для сравнения.
        /// </param>
        /// <returns>
        ///     Результат сравнения.
        /// </returns>
        public int CompareTo(HighlightRange other)
        {
            int result = Comparer<int>.Default.Compare(Offset, other.Offset);
            return result == 0 ? Comparer<int>.Default.Compare(Length, other.Length) : result;
        }

        /// <summary>
        ///     Получает хеш-код объекта.
        /// </summary>
        /// <returns>
        ///     Хеш-код.
        /// </returns>
        public override int GetHashCode()
        {
            return Offset ^ Length;
        }
        /// <summary>
        ///     Сравнивает объект с <c><paramref name="obj"/></c>.
        /// </summary>
        /// <param name="obj">
        ///     Объект для сравнения.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если объекты равны, иначе – <c>false</c>.
        /// </returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is HighlightRange range && Equals(range);
        }
        /// <summary>
        ///     Сравнивает объект с <c><paramref name="other"/></c>.
        /// </summary>
        /// <param name="other">
        ///     Объект для сравнения.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если объекты равны, иначе – <c>false</c>.
        /// </returns>
        public bool Equals(HighlightRange other)
        {
            return Offset == other.Offset && Length == other.Length;
        }
    }
}