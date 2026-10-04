using Prism.Mvvm;
using System.Globalization;
using ExceptionHelper = Cybrex.Core.Exceptions.ExceptionHelper;

namespace PrimitiveTranslator.Data.Models
{
    /// <summary>
    ///     Параметр.
    /// </summary>
    public sealed class Parameter : BindableBase
    {
        /// <summary>
        ///     Ключ.
        /// </summary>
        public string Key
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Значение.
        /// </summary>
        public string Value
        {
            get => field;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        ///     Инициализирует новый экземпляр параметра.
        /// </summary>
        /// <param name="key">
        ///     Ключ.
        /// </param>
        /// <param name="value">
        ///     Значение.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="key"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="value"/></c> равен <c>null</c>.
        /// </exception>
        public Parameter(string key, string value)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(key, nameof(key));
            ExceptionHelper.ThrowIfArgumentIsNull(value, nameof(value));

            // Инициализация
            Key = key;
            Value = value;
        }
        /// <summary>
        ///     Инициализирует новый экземпляр параметра.
        /// </summary>
        /// <param name="key">
        ///     Ключ.
        /// </param>
        /// <param name="value">
        ///     Значение.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="key"/></c> равен <c>null</c>.
        /// </exception>
        public Parameter(string key, double value)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(key, nameof(key));

            // Инициализация
            Key = key;
            Value = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Конфертирует значение в <c><see cref="double"/></c>.
        /// </summary>
        /// <returns>
        ///     Число.
        /// </returns>
        public double ToDouble()
        {
            return double.TryParse(Value, CultureInfo.InvariantCulture, out double value) ? value : double.NaN;
        }
    }
}