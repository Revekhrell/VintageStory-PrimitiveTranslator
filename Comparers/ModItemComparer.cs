using PrimitiveTranslator.Data.Models;
using System.Collections.Generic;

namespace PrimitiveTranslator.Comparers
{
    /// <summary>
    ///     Компаратор <c><see cref="Mod"/></c> для списка модов.
    /// </summary>
    public sealed class ModItemComparer : IComparer<Mod>
    {
        /// <summary>
        ///     Общий экземпляр.
        /// </summary>
        public static ModItemComparer Instance
        {
            get => field ??= new ModItemComparer();
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
        ///     Резкльтат сравнения.
        /// </returns>
        public int Compare(Mod? x, Mod? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null && y is not null)
                return -1;
            if (x is not null && y is null)
                return 1;

            int result = -Comparer<bool>.Default.Compare(x!.HasKeysChanges, y!.HasKeysChanges);
            return result == 0 ? Comparer<Mod>.Default.Compare(x, y) : result;
        }
    }
}