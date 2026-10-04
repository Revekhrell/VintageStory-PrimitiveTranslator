using ICSharpCode.AvalonEdit;
using PrimitiveTranslator.Editors;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PrimitiveTranslator.Converters.Xaml
{
    /// <summary>
    ///     Конвертер <c><see cref="JsonEditor"/> <![CDATA[-->]]> <see cref="TextEditor"/></c>.
    /// </summary>
    public sealed class JsonEditorToTextEditorConverter : IValueConverter
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
            return (value as JsonEditor)?.GetTextEditor();
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