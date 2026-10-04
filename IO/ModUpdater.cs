using Microsoft.EntityFrameworkCore;
using PrimitiveTranslator.Data;
using PrimitiveTranslator.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimitiveTranslator.IO
{
    /// <summary>
    ///     Вспомогательные методы обновления модов.
    /// </summary>
    public static class ModUpdater
    {
        /// <summary>
        ///     Обновляет базу данных на основе прочитанного набора модов.
        /// </summary>
        /// <remarks>
        ///     Набор <c><paramref name="mods"/></c> не должен иметь модов с одинаковым <c><see cref="Mod.UID"/></c>, для
        ///     чего необходимо лишь не переопределять <c><see cref="Comparer{T}"/></c> для набора.
        /// </remarks>
        /// <param name="context">
        ///     Контекст данных.
        /// </param>
        /// <param name="mods">
        ///     Набор модов.
        /// </param>
        /// <param name="callback">
        ///     Функция обратного вызова при обработке мода.
        /// </param>
        /// <returns>
        ///     Задача.
        /// </returns>
        public static async Task UpdateDataBase(DataContext context, ISet<Mod> mods, Action<Mod>? callback = null)
        {
            // Проверка аргументов
            if (context is null || mods is null || mods.Count == 0)
                return;

            // Языки
            SortedSet<Language> cachedLanguages = new SortedSet<Language>(await context.Languages.ToArrayAsync(), Comparer<Language>.Default);

            // Обновление модов
            foreach (Mod mod in mods)
            {
                // Поиск мода
                Mod? cachedMod = await context.Mods.Include(x => x.Keys).ThenInclude(x => x.Values).ThenInclude(x => x.Language)
                                                   .Include(x => x.Languages).AsSplitQuery().FirstOrDefaultAsync(x => x.UID == mod.UID);

                // Добавление/обновление
                if (cachedMod is null)
                {
                    cachedMod = new Mod(mod.Id, mod.UID, mod.Name, mod.Version, mod.Description, mod.Timestamp, mod.LanguageId, mod.RowVersion);
                    await context.Mods.AddAsync(cachedMod);
                }
                else if (mod.Name is not null || mod.Version is not null || mod.Description is not null)
                {
                    cachedMod.UpdateFromJsonModel(mod);
                }

                // Составление начальных состояний
                SortedSet<Key> keys = new SortedSet<Key>(mod.Keys, Comparer<Key>.Default);
                SortedSet<Key> cachedKeys = new SortedSet<Key>(cachedMod.Keys, Comparer<Key>.Default);

                // Обновление ключей
                foreach (Key key in mod.Keys)
                {
                    // Добавление
                    if (!cachedKeys.TryGetValue(key, out Key? cachedKey))
                    {
                        cachedKey = new Key(key.Id, key.Text, key.Timestamp, key.ModId, key.RowVersion);
                        cachedKey.InternalSetMod(cachedMod);
                        await context.Keys.AddAsync(cachedKey);
                    }

                    // Начальное состояние
                    SortedSet<Value> cachedValues = new SortedSet<Value>(cachedKey.Values, Comparer<Value>.Default);

                    // Обновление значений
                    foreach (Value value in key.Values)
                    {
                        // Некорректное значение
                        if (value.Language is null)
                            continue;

                        // Добавление
                        if (!cachedValues.TryGetValue(value, out Value? cachedValue))
                        {
                            // Язык
                            if (!cachedLanguages.TryGetValue(value.Language, out Language? cachedLanguage))
                            {
                                cachedLanguage = new Language(value.Language.Id, value.Language.Name);
                                await context.Languages.AddAsync(cachedLanguage);
                                cachedLanguages.Add(cachedLanguage);
                            }

                            // Экземпляр
                            cachedValue = new Value(value.Id, value.Text, value.Timestamp, value.LanguageId, value.KeyId, value.RowVersion);
                            cachedValue.InternalSetLanguage(cachedLanguage);
                            cachedValue.InternalSetKey(cachedKey);
                            await context.Values.AddAsync(cachedValue);
                        }

                        // Обновление значения
                        cachedValue.UpdateFromJsonModel(value);
                    }
                }

                // Удаление устаревших ключей
                cachedKeys.ExceptWith(keys);
                context.Keys.RemoveRange(cachedKeys);

                // Обновление ключей
                SortedSet<Language> languages = new SortedSet<Language>(Comparer<Language>.Default);
                foreach (Key key in cachedMod.Keys)
                    foreach (Value value in key.Values)
                        if (value.Language is not null)
                            languages.Add(value.Language);
                cachedMod.Languages.Clear();
                foreach (Language language in languages)
                    cachedMod.Languages.Add(language);

                // Оповещение
                callback?.Invoke(cachedMod);
            }
        }
        /// <summary>
        ///     Обновляет набор модов для интерфейса на основе обновлённой базы.
        /// </summary>
        /// <param name="context">
        ///     Контекст данных.
        /// </param>
        /// <param name="mods">
        ///     Набор модов.
        /// </param>
        /// <returns>
        ///     Задача.
        /// </returns>
        public static async Task UpdateUI(DataContext context, IList<Mod> mods)
        {
            // Проверка аргументов
            if (context is null || mods is null)
                return;

            // Языки и моды
            SortedSet<Language> cachedLanguages = new SortedSet<Language>(await context.Languages.ToArrayAsync(), Comparer<Language>.Default);
            SortedSet<Mod> cachedMods = new SortedSet<Mod>(await context.Mods.Include(x => x.Language).Include(x => x.Languages).AsSplitQuery().ToArrayAsync(), Comparer<Mod>.Default);

            // Обноволение
            int index = 0;
            while (index < mods.Count)
            {
                // Поиск и обновление мода
                Mod mod = mods[index];
                if (cachedMods.TryGetValue(mod, out Mod? cached))
                {
                    // Обновление языка
                    if (!EqualityComparer<Language>.Default.Equals(mod.Language, cached.Language))
                    {
                        if (mod.Language is not null)
                        {
                            // Поиск, обновление, добавление
                            if (!cachedLanguages.TryGetValue(mod.Language, out Language? cachedLanguage))
                            {
                                cachedLanguage = new Language(mod.Language.Id, mod.Language.Name);
                                await context.Languages.AddAsync(cachedLanguage);
                                cachedLanguages.Add(cachedLanguage);
                            }
                            cached.Language = cachedLanguage;
                        }
                        else
                        {
                            cached.Language = null;
                        }
                    }

                    // Обновление мода
                    await mod.UpdateFromDataBase(context);
                    index++;
                }
                else
                {
                    mods.RemoveAt(index);
                }
            }

            // Добавление недостающих модов
            cachedMods.ExceptWith(mods);
            foreach (Mod mod in cachedMods)
                mods.Add(mod);
            if (mods is List<Mod> sorted)
                sorted.Sort(Comparer<Mod>.Default);
        }
    }
}