using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimitiveTranslator.Comparers.SQLite;
using PrimitiveTranslator.Converters.SQLite;
using PrimitiveTranslator.Data.Models;
using ExceptionHelper = Cybrex.Core.Exceptions.ExceptionHelper;

namespace PrimitiveTranslator.Data
{
    /// <summary>
    ///     Контекст данных.
    /// </summary>
    public sealed class DataContext : DbContext
    {
        /// <summary>
        ///     Параметры.
        /// </summary>
        public DbSet<Parameter> Parameters { get; set; }
        /// <summary>
        ///     Языки.
        /// </summary>
        public DbSet<Language> Languages { get; set; }
        /// <summary>
        ///     Моды.
        /// </summary>
        public DbSet<Mod> Mods { get; set; }
        /// <summary>
        ///     Ключи.
        /// </summary>
        public DbSet<Key> Keys { get; set; }
        /// <summary>
        ///     Значения.
        /// </summary>
        public DbSet<Value> Values { get; set; }

        /// <summary>
        ///     Инициаилизирует новый экземпляр контекста.
        /// </summary>
        /// <param name="options">
        ///     Настройки.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="options"/></c> равен <c>null</c>.
        /// </exception>
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(options, nameof(options));
        }

        /// <summary>
        ///     Конфигурирует таблицы.
        /// </summary>
        /// <param name="builder">
        ///     Конфигуратор.
        /// </param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Parameter>(ConfigureParametersTable);
            builder.Entity<Language>(ConfigureLanguagesTable);
            builder.Entity<Mod>(ConfigureModsTable);
            builder.Entity<Key>(ConfigureKeysTable);
            builder.Entity<Value>(ConfigureValuesTable);
        }
        /// <summary>
        ///     Конфигурирует таблицу <c><see cref="Parameters"/></c>.
        /// </summary>
        /// <param name="builder">
        ///     Конфигуратор.
        /// </param>
        private static void ConfigureParametersTable(EntityTypeBuilder<Parameter> builder)
        {
            // Таблица
            builder.ToTable("Parameters");

            // Ключ
            builder.HasKey(x => x.Key);
            builder.Property(x => x.Key).ValueGeneratedNever();

            // Значение
            builder.Property(x => x.Value).IsRequired();
        }
        /// <summary>
        ///     Конфигурирует таблицу <c><see cref="Languages"/></c>.
        /// </summary>
        /// <param name="builder">
        ///     Конфигуратор.
        /// </param>
        private static void ConfigureLanguagesTable(EntityTypeBuilder<Language> builder)
        {
            // Таблица
            builder.ToTable("Languages");

            // Идентификатор
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            // Название
            builder.Property(x => x.Name).IsRequired();
            builder.HasIndex(x => x.Name).IsUnique();
        }
        /// <summary>
        ///     Конфигурирует таблицу <c><see cref="Mods"/></c>.
        /// </summary>
        /// <param name="builder">
        ///     Конфигуратор.
        /// </param>
        private static void ConfigureModsTable(EntityTypeBuilder<Mod> builder)
        {
            // Таблица
            builder.ToTable("Mods");

            // Идентификатор
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            // Уникальный идентификатор
            builder.Property(x => x.UID).IsRequired();
            builder.HasIndex(x => x.UID).IsUnique();

            // Время регистрации
            builder.Property(x => x.Timestamp).IsRequired();
            builder.Property(x => x.Timestamp).HasConversion<DateTimeConverter>();

            // Язык
            builder.HasOne(x => x.Language).WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.SetNull);

            // Языки
            builder.HasMany(x => x.Languages).WithMany().UsingEntity(x => x.ToTable("ModLanguagesReferences"));

            // Ключи
            builder.HasMany(x => x.Keys).WithOne(x => x.Mod).HasForeignKey(x => x.ModId).OnDelete(DeleteBehavior.Cascade);

            // Версия
            builder.Property(x => x.RowVersion).IsRequired();
            builder.Property(x => x.RowVersion).IsConcurrencyToken().Metadata.SetValueComparer(ByteArrayComparer.Instance);
        }
        /// <summary>
        ///     Конфигурирует таблицу <c><see cref="Keys"/></c>.
        /// </summary>
        /// <param name="builder">
        ///     Конфигуратор.
        /// </param>
        private static void ConfigureKeysTable(EntityTypeBuilder<Key> builder)
        {
            // Таблица
            builder.ToTable("Keys");

            // Идентификатор
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            // Текст ключа
            builder.Property(x => x.Text).IsRequired();

            // Время регистрации
            builder.Property(x => x.Timestamp).IsRequired();
            builder.Property(x => x.Timestamp).HasConversion<DateTimeConverter>();

            // Мод
            builder.Property(x => x.ModId).IsRequired();
            builder.HasOne(x => x.Mod).WithMany(x => x.Keys).HasForeignKey(x => x.ModId).OnDelete(DeleteBehavior.Cascade);

            // Значения
            builder.HasMany(x => x.Values).WithOne(x => x.Key).HasForeignKey(x => x.KeyId).OnDelete(DeleteBehavior.Cascade);

            // Версия
            builder.Property(x => x.RowVersion).IsRequired();
            builder.Property(x => x.RowVersion).IsConcurrencyToken().Metadata.SetValueComparer(ByteArrayComparer.Instance);

            // Составные индексы
            builder.HasIndex(x => new { x.ModId, x.Text }).IsUnique();
        }
        /// <summary>
        ///     Конфигурирует таблицу <c><see cref="Values"/></c>.
        /// </summary>
        /// <param name="builder">
        ///     Конфигуратор.
        /// </param>
        private static void ConfigureValuesTable(EntityTypeBuilder<Value> builder)
        {
            // Таблица
            builder.ToTable("Values");

            // Идентификатор
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            // Текст значения
            builder.Property(x => x.Text).IsRequired();

            // Время регистрации
            builder.Property(x => x.Timestamp).IsRequired();
            builder.Property(x => x.Timestamp).HasConversion<DateTimeConverter>();

            // Язык
            builder.Property(x => x.LanguageId).IsRequired();
            builder.HasOne(x => x.Language).WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict);

            // Ключ
            builder.Property(x => x.KeyId).IsRequired();
            builder.HasOne(x => x.Key).WithMany(x => x.Values).HasForeignKey(x => x.KeyId).OnDelete(DeleteBehavior.Cascade);

            // Версия
            builder.Property(x => x.RowVersion).IsRequired();
            builder.Property(x => x.RowVersion).IsConcurrencyToken().Metadata.SetValueComparer(ByteArrayComparer.Instance);

            // Уникальный ключ
            builder.HasIndex(x => new { x.KeyId, x.LanguageId }).IsUnique();
        }
    }
}