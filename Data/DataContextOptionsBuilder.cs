using Dorssel.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PrimitiveTranslator.Data
{
    /// <summary>
    ///     Построитель настроек контекста данных.
    /// </summary>
    internal static class DataContextOptionsBuilder
    {
        /// <summary>
        ///     Строка подключения.
        /// </summary>
        private const string ConnectionString = "Data Source=Data.bin";
        /// <summary>
        ///     Настройки.
        /// </summary>
        private static DbContextOptions<DataContext>? Options;

        /// <summary>
        ///     Создаёт настройки контекста.
        /// </summary>
        /// <returns>
        ///     Настройки контекста данных.
        /// </returns>
        internal static DbContextOptions<DataContext> Create()
        {
            if (Options is not null)
                return Options;

            DbContextOptionsBuilder<DataContext> builder = new DbContextOptionsBuilder<DataContext>();
            builder.UseSqlite(ConnectionString);
            builder.UseSqliteTimestamp();
            Options = builder.Options;
            return Options;
        }
    }
}