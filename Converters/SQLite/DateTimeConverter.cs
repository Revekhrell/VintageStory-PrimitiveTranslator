using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace PrimitiveTranslator.Converters.SQLite
{
    /// <summary>
    ///     Конвертер <c><see cref="DateTime"/> <![CDATA[<-->]]> <see cref="long"/></c>.
    /// </summary>
    public sealed class DateTimeConverter : ValueConverter<DateTime, long>
    {
        /// <summary>
        ///     Инициализирует новый экземпляр конвертера.
        /// </summary>
        public DateTimeConverter() : base((DateTime timestamp) => Convert(timestamp), (long ticks) => ConvertBack(ticks))
        {
        }

        /// <summary>
        ///     Конвертирует <c><see cref="DateTime"/></c> в <c><see cref="long"/></c>.
        /// </summary>
        /// <param name="timestamp">
        ///     Временная метка.
        /// </param>
        /// <returns>
        ///     Число.
        /// </returns>
        private static long Convert(DateTime timestamp)
        {
            return timestamp.Ticks;
        }
        /// <summary>
        ///     Конвертирует <c><see cref="long"/></c> в <c><see cref="DateTime"/></c>.
        /// </summary>
        /// <param name="ticks">
        ///     Число.
        /// </param>
        /// <returns>
        ///     Временная метка.
        /// </returns>
        private static DateTime ConvertBack(long ticks)
        {
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                return DateTime.MinValue;

            return new DateTime(ticks);
        }
    }
}