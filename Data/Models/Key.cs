using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Data;
using ExceptionHelper = Cybrex.Core.Exceptions.ExceptionHelper;

namespace PrimitiveTranslator.Data.Models
{
    /// <summary>
    ///     Ключ.
    /// </summary>
    public sealed class Key : BindableBase, IEquatable<Key>, IComparable<Key>
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
        ///     Текст ключа.
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
        ///     Идентификатор мода.
        /// </summary>
        public int ModId
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Мод.
        /// </summary>
        [MemberNotNullWhen(true, nameof(IsLoaded))]
        public Mod? Mod
        {
            get => field;
            private set
            {
                ModId = value?.Id ?? 0;
                SetProperty(ref field, value);
            }
        }
        /// <summary>
        ///     Устанавливает <c><see cref="Mod"/></c> не разрешая обратной зависимости.
        /// </summary>
        /// <param name="mod">
        ///     Мод.
        /// </param>
        internal void InternalSetMod(Mod? mod)
        {
            Mod = mod;
        }

        /// <summary>
        ///     Значения.
        /// </summary>
        public ICollection<Value> Values
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Значения.
        /// </summary>
        public ICollectionView ValuesView
        {
            get => field ??= CollectionViewSource.GetDefaultView(Values);
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
            get => ModId == 0 || Mod is not null;
        }

        /// <summary>
        ///     Инициализирует новый экземпляр ключа.
        /// </summary>
        /// <param name="text">
        ///     Текст ключа.
        /// </param>
        /// <param name="mod">
        ///     Мод.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="text"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="mod"/></c> равен <c>null</c>.
        /// </exception>
        public Key(string text, Mod mod)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(text, nameof(text));
            ExceptionHelper.ThrowIfArgumentIsNull(mod, nameof(mod));

            // Инициализация
            Text = text;
            Mod = mod;
            RowVersion = new byte[sizeof(long)];
            Timestamp = DateTime.UtcNow;
            Values = new List<Value>();
        }
        /// <summary>
        ///     Инициализирует новый экземпляр ключа.
        /// </summary>
        /// <param name="id">
        ///     Идентификатор.
        /// </param>
        /// <param name="text">
        ///     Текст ключа.
        /// </param>
        /// <param name="timestamp">
        ///     Время регистрации.
        /// </param>
        /// <param name="modId">
        ///     Идентификатор мода.
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
        public Key(long id, string text, DateTime timestamp, int modId, byte[] rowVersion)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(text, nameof(text));

            // Инициализация
            Id = id;
            Text = text;
            Timestamp = timestamp;
            ModId = modId;
            RowVersion = rowVersion;
            Values = new List<Value>();
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
        public int CompareTo(Key? other)
        {
            if (other is null)
                return 1;

            int result;
            if (Mod is null || other.Mod is null)
                result = Comparer<int>.Default.Compare(ModId, other.ModId);
            else
                result = Comparer<Mod>.Default.Compare(Mod, other.Mod);
            if (result != 0)
                return result;

            return Comparer<string>.Default.Compare(Text, other.Text);
        }

        /// <summary>
        ///     Получает хеш-код объекта.
        /// </summary>
        /// <returns>
        ///     Хеш-код.
        /// </returns>
        public override int GetHashCode()
        {
            return Text.GetHashCode();
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
            return obj is Key key && Equals(key);
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
        public bool Equals([NotNullWhen(true)] Key? other)
        {
            if (other is null)
                return false;

            if (Mod is null || other.Mod is null)
            {
                if (!EqualityComparer<int>.Default.Equals(ModId, other.ModId))
                    return false;
            }
            else
            {
                if (!EqualityComparer<Mod>.Default.Equals(Mod, other.Mod))
                    return false;
            }

            return EqualityComparer<string>.Default.Equals(Text, other.Text);
        }

        /// <summary>
        ///     Получает строковое представление объекта.
        /// </summary>
        /// <returns>
        ///     Строковое представление.
        /// </returns>
        public override string ToString()
        {
            return Text;
        }
    }
}