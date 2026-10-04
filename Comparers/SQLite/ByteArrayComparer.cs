using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace PrimitiveTranslator.Comparers.SQLite
{
    /// <summary>
    ///     Компаратор <c><see cref="byte"/>[]</c>.
    /// </summary>
    public sealed class ByteArrayComparer : ValueComparer<byte[]>
    {
        /// <summary>
        ///     Общий экземпляр.
        /// </summary>
        public static ByteArrayComparer Instance
        {
            get => field ??= new ByteArrayComparer();
        }

        /// <summary>
        ///     Инициализирует новый экземпляр компаратора.
        /// </summary>
        public ByteArrayComparer() : base((byte[]? x, byte[]? y) => Equals(x, y), (byte[] obj) => GetHashCode(obj), (byte[] obj) => GetSnapshot(obj))
        {
        }

        /// <summary>
        ///     Получает полную копию объекта.
        /// </summary>
        /// <param name="obj">
        ///     Объект.
        /// </param>
        /// <returns>
        ///     Копия.
        /// </returns>
        private static byte[] GetSnapshot(byte[] obj)
        {
            byte[] clone = new byte[obj.Length];
            Array.Copy(obj, 0, clone, 0, obj.Length);
            return clone;
        }
        /// <summary>
        ///     Получает хеш-код объекта.
        /// </summary>
        /// <param name="obj">
        ///     Объект.
        /// </param>
        /// <returns>
        ///     Хеш-код.
        /// </returns>
        private static new int GetHashCode(byte[] obj)
        {
            Span<int> hash = stackalloc int[1];
            Span<byte> buffer = MemoryMarshal.Cast<int, byte>(hash);
            Crc32.Hash(obj, buffer);
            return hash[0];
        }
        /// <summary>
        ///     Сравнивает два объекта.
        /// </summary>
        /// <param name="x">
        ///     Объект 1.
        /// </param>
        /// <param name="y">
        ///     Объект 2.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если объекты равны, иначе – <c>false</c>.
        /// </returns>
        private static new bool Equals(byte[]? x, byte[]? y)
        {
            if (object.ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;

            return x.SequenceEqual(y);
        }
    }
}