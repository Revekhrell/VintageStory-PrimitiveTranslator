using Prism.Mvvm;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ExceptionHelper = Cybrex.Core.Exceptions.ExceptionHelper;

namespace PrimitiveTranslator.Data.Models
{
    /// <summary>
    ///     Язык.
    /// </summary>
    public sealed class Language : BindableBase, IEquatable<Language>, IComparable<Language>
    {
        /// <summary>
        ///     Хеш-код.
        /// </summary>
        private readonly int HashCode;

        /// <summary>
        ///     Идентификатор.
        /// </summary>
        public int Id
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Название.
        /// </summary>
        public string Name
        {
            get => field;
            private set => SetProperty(ref field, value);
        }

        /// <summary>
        ///     Инициализирует новый экземпляр языка.
        /// </summary>
        /// <param name="name">
        ///     Название.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="name"/></c> равен <c>null</c>.
        /// </exception>
        public Language(string name)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(name, nameof(name));

            // Инициализация
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            Encoding.ASCII.TryGetBytes(name, buffer, out _);
            HashCode = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            Name = name.ToLower();
        }
        /// <summary>
        ///     Инициализирует новый экземпляр языка.
        /// </summary>
        /// <param name="id">
        ///     Идентификатор.
        /// </param>
        /// <param name="name">
        ///     Название.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="name"/></c> равен <c>null</c>.
        /// </exception>
        public Language(int id, string name) : this(name)
        {
            Id = id;
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
        public int CompareTo(Language? other)
        {
            return Comparer<string>.Default.Compare(Name, other?.Name);
        }

        /// <summary>
        ///     Получает хеш-код объекта.
        /// </summary>
        /// <returns>
        ///     Хеш-код.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCode;
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
            return obj is Language language && Equals(language);
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
        public bool Equals([NotNullWhen(true)] Language? other)
        {
            if (other is null)
                return false;

            return EqualityComparer<string>.Default.Equals(Name, other.Name);
        }

        /// <summary>
        ///     Получает строковое представление объекта.
        /// </summary>
        /// <returns>
        ///     Строковое представление.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }
}