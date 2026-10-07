using Microsoft.EntityFrameworkCore;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using ExceptionHelper = Cybrex.Core.Exceptions.ExceptionHelper;

namespace PrimitiveTranslator.Data.Models
{
    /// <summary>
    ///     Мод.
    /// </summary>
    public sealed class Mod : BindableBase, IEquatable<Mod>, IComparable<Mod>
    {
        /// <summary>
        ///     Идентификатор.
        /// </summary>
        public int Id
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Уникальный идентификатор.
        /// </summary>
        public string UID
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Название.
        /// </summary>
        public string? Name
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Версия.
        /// </summary>
        public string? Version
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Описание.
        /// </summary>
        public string? Description
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Время регистрации.
        /// </summary>
        public DateTime Timestamp
        {
            get => field;
            private set => SetProperty(ref field, value);
        }

        /// <summary>
        ///     Идентификатор языка.
        /// </summary>
        public int? LanguageId
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Язык.
        /// </summary>
        public Language? Language
        {
            get => field;
            set
            {
                LanguageId = value?.Id;
                SetProperty(ref field, value);
            }
        }
        /// <summary>
        ///     Языки.
        /// </summary>
        public ICollection<Language> Languages
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Языки.
        /// </summary>
        public ICollectionView LanguagesView
        {
            get
            {
                field ??= CollectionViewSource.GetDefaultView(Languages);
                LanguagesDispatcher ??= Dispatcher.CurrentDispatcher;
                return field;
            }
        }
        /// <summary>
        ///     Диспатчер потока.
        /// </summary>
        private Dispatcher? LanguagesDispatcher;

        /// <summary>
        ///     Ключи.
        /// </summary>
        public ICollection<Key> Keys
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Ключи.
        /// </summary>
        public ICollectionView KeysView
        {
            get
            {
                field ??= CollectionViewSource.GetDefaultView(Keys);
                KeysDispatcher ??= Dispatcher.CurrentDispatcher;
                return field;
            }
        }
        /// <summary>
        ///     Диспатчер потока.
        /// </summary>
        private Dispatcher? KeysDispatcher;

        /// <summary>
        ///     Версия записи.
        /// </summary>
        public byte[] RowVersion
        {
            get => field;
            private set => SetProperty(ref field, value);
        }

        /// <summary>
        ///     Флаг наличия изменений в ключах.
        /// </summary>
        public bool HasKeysChanges
        {
            get => field;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        ///     Инициализирует новый экземпляр мода.
        /// </summary>
        private Mod()
        {
            // Инициалиация
            UID = string.Empty;
            RowVersion = new byte[sizeof(long)];
            Languages = new List<Language>();
            Keys = new List<Key>();
        }
        /// <summary>
        ///     Инициализирует новый экземпляр мода.
        /// </summary>
        /// <param name="uid">
        ///     Уникальный идентификатор.
        /// </param>
        /// <param name="name">
        ///     Название.
        /// </param>
        /// <param name="version">
        ///     Версия.
        /// </param>
        /// <param name="description">
        ///     Описание.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="uid"/></c> равен <c>null</c>.
        /// </exception>
        public Mod(string uid, string? name = null, string? version = null, string? description = null) : this()
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(uid, nameof(uid));

            // Инициалиация
            UID = uid;
            Name = name;
            Version = version;
            Description = description;
            Timestamp = DateTime.UtcNow;
        }
        /// <summary>
        ///     Инициализирует новый экземпляр мода.
        /// </summary>
        /// <param name="id">
        ///     Идентификатор.
        /// </param>
        /// <param name="uid">
        ///     Уникальный идентификатор.
        /// </param>
        /// <param name="name">
        ///     Название.
        /// </param>
        /// <param name="version">
        ///     Версия.
        /// </param>
        /// <param name="description">
        ///     Описание.
        /// </param>
        /// <param name="timestamp">
        ///     Время регистрации.
        /// </param>
        /// <param name="languageId">
        ///     Идентификатор языка.
        /// </param>
        /// <param name="rowVersion">
        ///     Версия записи.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="uid"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="rowVersion"/></c> равен <c>null</c>.
        /// </exception>
        public Mod(int id, string uid, string? name, string? version, string? description, DateTime timestamp, int? languageId, byte[] rowVersion) : this(uid, name, version, description)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(rowVersion, nameof(rowVersion));

            // Инициализация
            Id = id;
            Timestamp = timestamp;
            LanguageId = languageId;
            RowVersion = rowVersion;
        }

        /// <summary>
        ///     Обновляет модель из json-свойств.
        /// </summary>
        /// <param name="mod">
        ///     Мод.
        /// </param>
        public void UpdateFromJsonModel(Mod mod)
        {
            if (!Equals(mod))
                return;

            Name = mod.Name;
            Version = mod.Version;
            Description = mod.Description;
        }
        /// <summary>
        ///     Обновляет модель из базы данных.
        /// </summary>
        /// <param name="context">
        ///     Контекст данных.
        /// </param>
        /// <returns>
        ///     Задача.
        /// </returns>
        public async Task UpdateFromDataBase(DataContext context)
        {
            Mod? cached = await context.Mods.Include(x => x.Language).Include(x => x.Languages).FirstOrDefaultAsync(x => x.Id == Id);
            if (cached is null)
                return;

            Name = cached.Name;
            Version = cached.Version;
            Description = cached.Description;
            Language = cached.Language;

            Languages.Clear();
            ((List<Language>)Languages).AddRange(cached.Languages);
            LanguagesDispatcher?.Invoke(LanguagesView.Refresh);
        }
        /// <summary>
        ///     Обновляет модель в базе данных.
        /// </summary>
        /// <param name="context">
        ///     Контекст данных.
        /// </param>
        /// <returns>
        ///     Задача.
        /// </returns>
        public async Task UpdateToDataBase(DataContext context)
        {
            Mod? cached = await context.Mods.Include(x => x.Language).Include(x => x.Languages).FirstOrDefaultAsync(x => x.Id == Id);
            if (cached is null)
                return;

            cached.Name = Name;
            cached.Version = Version;
            cached.Description = Description;

            Language? cachedLanguage = await context.Languages.FindAsync(LanguageId);
            cached.Language = cachedLanguage;

            cached.Languages.Clear();
            foreach (Language language in Languages)
            {
                cachedLanguage = await context.Languages.FindAsync(LanguageId);
                if (cachedLanguage is not null)
                    cached.Languages.Add(cachedLanguage);
            }
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
        public int CompareTo(Mod? other)
        {
            if (other is null)
                return 1;

            return Comparer<string>.Default.Compare(UID, other.UID);
        }

        /// <summary>
        ///     Получает хеш-код объекта.
        /// </summary>
        /// <returns>
        ///     Хеш-код.
        /// </returns>
        public override int GetHashCode()
        {
            return UID.GetHashCode();
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
            return obj is Mod mod && Equals(mod);
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
        public bool Equals([NotNullWhen(true)] Mod? other)
        {
            if (other is null)
                return false;

            return EqualityComparer<string>.Default.Equals(UID, other.UID);
        }

        /// <summary>
        ///     Получает строковое представление объекта.
        /// </summary>
        /// <returns>
        ///     Строковое представление.
        /// </returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(Name ?? UID);
            if (Version is not null)
                builder.Append($" v{Version}");

            return builder.ToString();
        }
    }
}