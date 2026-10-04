using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ExceptionHelper = Cybrex.Core.Exceptions.ExceptionHelper;

namespace PrimitiveTranslator.Data.Models
{
    /// <summary>
    ///     Значение.
    /// </summary>
    public sealed class Value : BindableBase, IEquatable<Value>, IComparable<Value>
    {
        /// <summary>
        ///     Идентификатор.
        /// </summary>
        public long Id
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Текст значения.
        /// </summary>
        public string Text
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
        public int LanguageId
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Язык.
        /// </summary>
        [MemberNotNullWhen(true, nameof(IsLoaded))]
        public Language? Language
        {
            get => field;
            private set
            {
                LanguageId = value?.Id ?? 0;
                SetProperty(ref field, value);
            }
        }
        /// <summary>
        ///     Устанавливает <c><see cref="Language"/></c>.
        /// </summary>
        /// <param name="language">
        ///     Язык.
        /// </param>
        internal void InternalSetLanguage(Language? language)
        {
            Language = language;
        }

        /// <summary>
        ///     Идентификатор ключа.
        /// </summary>
        public long KeyId
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Ключ.
        /// </summary>
        [MemberNotNullWhen(true, nameof(IsLoaded))]
        public Key? Key
        {
            get => field;
            private set
            {
                KeyId = value?.Id ?? 0;
                SetProperty(ref field, value);
            }
        }
        /// <summary>
        ///     Устанавливает <c><see cref="Key"/></c> не разрешая обратной зависимости.
        /// </summary>
        /// <param name="key">
        ///     Язык.
        /// </param>
        internal void InternalSetKey(Key? key)
        {
            Key = key;
        }

        /// <summary>
        ///     Версия записи.
        /// </summary>
        public byte[] RowVersion
        {
            get => field;
            private set => SetProperty(ref field, value);
        }

        /// <summary>
        ///     Флаг загруженного объекта.
        /// </summary>
        public bool IsLoaded
        {
            get => (LanguageId == 0 || Language is not null) && (KeyId == 0 || Key is not null);
        }

        /// <summary>
        ///     Инициализирует новый экземпляр значения.
        /// </summary>
        /// <param name="text">
        ///     Текст значения.
        /// </param>
        /// <param name="language">
        ///     Язык.
        /// </param>
        /// <param name="key">
        ///     Ключ.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="text"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="language"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="key"/></c> равен <c>null</c>.
        /// </exception>
        public Value(string text, Language language, Key key)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(text, nameof(text));
            ExceptionHelper.ThrowIfArgumentIsNull(language, nameof(language));
            ExceptionHelper.ThrowIfArgumentIsNull(key, nameof(key));

            // Инициализация
            Text = text;
            Language = language;
            Key = key;
            RowVersion = new byte[sizeof(long)];
            Timestamp = DateTime.UtcNow;
        }
        /// <summary>
        ///     Инициализирует новый экземпляр значения.
        /// </summary>
        /// <param name="id">
        ///     Идентификатор.
        /// </param>
        /// <param name="text">
        ///     Текст значения.
        /// </param>
        /// <param name="timestamp">
        ///     Время регистрации.
        /// </param>
        /// <param name="languageId">
        ///     Идентификатор языка.
        /// </param>
        /// <param name="keyId">
        ///     Идентификатор ключа.
        /// </param>
        /// <param name="rowVersion">
        ///     Версия записи.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="text"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="rowVersion"/></c> равен <c>null</c>.
        /// </exception>
        public Value(long id, string text, DateTime timestamp, int languageId, long keyId, byte[] rowVersion)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(text, nameof(text));
            ExceptionHelper.ThrowIfArgumentIsNull(rowVersion, nameof(rowVersion));

            // Инициализация
            Id = id;
            Text = text;
            Timestamp = timestamp;
            LanguageId = languageId;
            KeyId = keyId;
            RowVersion = rowVersion;
        }

        /// <summary>
        ///     Обновляет модель из json-свойств.
        /// </summary>
        /// <param name="value">
        ///     Значение.
        /// </param>
        public void UpdateFromJsonModel(Value value)
        {
            if (!Equals(value))
                return;
            if (value.Timestamp < Timestamp)
                return;
            if (EqualityComparer<string>.Default.Equals(Text, value.Text))
                return;

            Text = value.Text;
            Timestamp = value.Timestamp;
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
            Value? cached = await context.Values.FindAsync(Id);
            if (cached is null)
                return;

            Text = cached.Text;
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
            Value? cached = await context.Values.FindAsync(Id);
            if (cached is null)
                return;

            cached.Text = Text;
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
        public int CompareTo(Value? other)
        {
            if (other is null)
                return 1;

            int result;
            if (Language is null || other.Language is null)
                result = Comparer<int>.Default.Compare(LanguageId, other.LanguageId);
            else
                result = Comparer<Language>.Default.Compare(Language, other.Language);
            if (result != 0)
                return result;

            if (Key is null || other.Key is null)
                result = Comparer<long>.Default.Compare(KeyId, other.KeyId);
            else
                result = Comparer<Key>.Default.Compare(Key, other.Key);

            return result;
        }

        /// <summary>
        ///     Получает хеш-код объекта.
        /// </summary>
        /// <returns>
        ///     Хеш-код.
        /// </returns>
        public override int GetHashCode()
        {
            if (Language is null || Key is null)
                return RuntimeHelpers.GetHashCode(this);
            else
                return Language.GetHashCode() ^ Key.GetHashCode();
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
            return obj is Value value && Equals(value);
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
        public bool Equals([NotNullWhen(true)] Value? other)
        {
            if (other is null)
                return false;

            if (Language is null || other.Language is null)
            {
                if (!EqualityComparer<int>.Default.Equals(LanguageId, other.LanguageId))
                    return false;
            }
            else
            {
                if (!EqualityComparer<Language>.Default.Equals(Language, other.Language))
                    return false;
            }

            if (Key is null || other.Key is null)
            {
                if (!EqualityComparer<long>.Default.Equals(KeyId, other.KeyId))
                    return false;
            }
            else
            {
                if (!EqualityComparer<Key>.Default.Equals(Key, other.Key))
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Получает строковое представление объекта.
        /// </summary>
        /// <returns>
        ///     Строковое представление.
        /// </returns>
        public override string ToString()
        {
            return $"{Language}: \"{Text}\"";
        }
    }
}