using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimitiveTranslator.Converters.Xaml
{
    /// <summary>
    ///     Конвертер <c><see cref="bool"/> <![CDATA[-->]]> <see cref="Visibility"/></c>.
    /// </summary>
    /// <remarks>
    ///     <c>false</c> конвертируется в <c><see cref="Visibility.Collapsed"/></c>.
    /// </remarks>
    public sealed class BoolToVisibilityCollapsedConverter : IValueConverter
    {
        /// <summary>
        ///     Конвертирует <c><paramref name="value"/></c> в представление.
        /// </summary>
        /// <param name="value">
        ///     Значение.
        /// </param>
        /// <param name="targetType">
        ///     Целевой тип.
        /// </param>
        /// <param name="parameter">
        ///     Параметр.
        /// </param>
        /// <param name="culture">
        ///     Культура.
        /// </param>
        /// <returns>
        ///     Отображение в представлении.
        /// </returns>
        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            return value is bool @bool ? (@bool ? Visibility.Visible : Visibility.Collapsed) : Visibility.Collapsed;
        }
        /// <summary>
        ///     Конвертирует <c><paramref name="value"/></c> в модель.
        /// </summary>
        /// <param name="value">
        ///     Значение.
        /// </param>
        /// <param name="targetType">
        ///     Целевой тип.
        /// </param>
        /// <param name="parameter">
        ///     Параметр.
        /// </param>
        /// <param name="culture">
        ///     Культура.
        /// </param>
        /// <returns>
        ///     Отображение в модели.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///     Метод не поддерживается.
        /// </exception>
        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotSupportedException();
        }
    }
}