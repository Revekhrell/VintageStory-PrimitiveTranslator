using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PrimitiveTranslator.Editors
{
    /// <summary>
    ///     Сигнатура редактора.
    /// </summary>
    public sealed class EditorSignature : IEquatable<EditorSignature>
    {
        /// <summary>
        ///     Тип редактора.
        /// </summary>
        public EditorType Type { get; set; }
        /// <summary>
        ///     Уникальный идентификатор мода.
        /// </summary>
        public string? ModId { get; set; }
        /// <summary>
        ///     Язык, с которого происходит перевод.
        /// </summary>
        public string? OriginalLanuage { get; set; }
        /// <summary>
        ///     Язык, на который происходит перевод.
        /// </summary>
        public string? TranslationLanguage { get; set; }

        /// <summary>
        ///     Получает хеш-код объекта.
        /// </summary>
        /// <returns>
        ///     Хеш-код.
        /// </returns>
        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
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
            return obj is EditorSignature signature && Equals(signature);
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
        public bool Equals([NotNullWhen(true)] EditorSignature? other)
        {
            if (other is null)
                return false;

            return Type == other.Type && ModId == other.ModId && OriginalLanuage == other.OriginalLanuage && TranslationLanguage == other.TranslationLanguage;
        }
    }
}