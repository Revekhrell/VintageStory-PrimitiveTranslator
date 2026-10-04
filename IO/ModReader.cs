using Newtonsoft.Json;
using PrimitiveTranslator.Converters.Json;
using PrimitiveTranslator.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrimitiveTranslator.IO
{
    /// <summary>
    ///     Вспомогательные методы чтения модов.
    /// </summary>
    public static class ModReader
    {
        /// <summary>
        ///     Ищет все zip-архивы в папке <c><paramref name="directory"/></c>.
        /// </summary>
        /// <param name="directory">
        ///     Папка с архивами.
        /// </param>
        /// <returns>
        ///     Список путей к архивам.
        /// </returns>
        public static IReadOnlyList<string> GetArchives(string directory)
        {
            if (!Directory.Exists(directory))
                return Array.Empty<string>();

            string[] files = Directory.GetFiles(directory);
            return files.Where(x => x.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        /// <summary>
        ///     Считывает архивы модов.
        /// </summary>
        /// <param name="pathes">
        ///     Пути к архивам.
        /// </param>
        /// <param name="translatorId">
        ///     Идентификатор мода переводчика.
        /// </param>
        /// <param name="callback">
        ///     Функция обратного вызова для обработки одного элемента.
        /// </param>
        /// <returns>
        ///     Набор модов.
        /// </returns>
        public static async Task<ISet<Mod>> ReadMods(IReadOnlyList<string> pathes, string? translatorId, Action<string>? callback = null)
        {
            // Проверка аргументов
            SortedSet<Mod> mods = new SortedSet<Mod>(Comparer<Mod>.Default);
            if (pathes is null || pathes.Count == 0)
                return mods;

            // Чтение архивов
            foreach (string path in pathes)
            {
                // Инициализация
                FileStream? stream = null;
                ZipArchive? archive = null;

                // Проверка пути
                if (!File.Exists(path))
                    goto Complete;

                // Открытие потока
                try { stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read); }
                catch { goto Complete; }

                // Открытие архива
                try { archive = new ZipArchive(stream, ZipArchiveMode.Read); }
                catch { goto Complete; }

                // Чтение архива
                await ReadMod(archive, mods, translatorId);
                
            // Завершение
            Complete:
                if (archive is not null)
                    await archive.DisposeAsync();
                if (stream is not null)
                    await stream.DisposeAsync();

                callback?.Invoke(path);
            }
            return mods;
        }
        /// <summary>
        ///     Считывает архив мода.
        /// </summary>
        /// <param name="archive">
        ///     Архив.
        /// </param>
        /// <param name="mods">
        ///     Набор модов.
        /// </param>
        /// <param name="translatorId">
        ///     Идентификатор мода переводчика.
        /// </param>
        /// <returns>
        ///     Задача.
        /// </returns>
        private static async Task ReadMod(ZipArchive archive, SortedSet<Mod> mods, string? translatorId)
        {
            // Проверка аргументов
            if (archive is null || mods is null)
                return;

            // Чтение информации
            if (!await ReadInfo(archive, mods, translatorId))
                return;

            // Чтение словарей
            await ReadDictionaries(archive, mods);
        }
        /// <summary>
        ///     Считывает файл информации мода.
        /// </summary>
        /// <param name="archive">
        ///     Арихив.
        /// </param>
        /// <param name="mods">
        ///     Набор модов.
        /// </param>
        /// <param name="translatorId">
        ///     Идентификатор мода переводчика.
        /// </param>
        /// <returns>
        ///     <c>false</c>, если мод нужно проигнорировать.
        /// </returns>
        private static async Task<bool> ReadInfo(ZipArchive archive, SortedSet<Mod> mods, string? translatorId)
        {
            // Проверка аргументов
            if (archive is null || mods is null)
                return false;

            // Поиск файла информации
            ZipArchiveEntry? info = archive.Entries.FirstOrDefault(x => x.FullName.Equals("modinfo.json", StringComparison.OrdinalIgnoreCase));
            if (info is null)
                return false;

            // Чтение файла информации
            string json;
            using (Stream stream = await info.OpenAsync())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    json = await reader.ReadToEndAsync();

            // Десериализация
            Mod? mod = null;
            try { mod = JsonConvert.DeserializeObject<Mod>(json, ModConverter.Instance); }
            catch { return false; }
            if (mod is null || (translatorId is not null && translatorId.Equals(mod.UID, StringComparison.OrdinalIgnoreCase)))
                return false;

            // Добавление
            if (!mods.TryGetValue(mod, out Mod? cached))
            {
                mods.Add(mod);
            }
            else
            {
                cached.UpdateFromJsonModel(mod);
            }
            return true;
        }
        /// <summary>
        ///     Считывает словари мода.
        /// </summary>
        /// <param name="archive">
        ///     Архив.
        /// </param>
        /// <param name="mods">
        ///     Список модов.
        /// </param>
        /// <returns>
        ///     Задача.
        /// </returns>
        private static async Task ReadDictionaries(ZipArchive archive, SortedSet<Mod> mods)
        {
            // Проверка аргументов
            if (archive is null || mods is null)
                return;

            // Чтение словарей
            IReadOnlyList<ZipArchiveEntry> entries = archive.Entries.Where(x => Regex.IsMatch(x.FullName, @"(?i)^assets/[^/]+/lang/[^/]+\.json")).ToArray();
            foreach (ZipArchiveEntry entry in entries)
            {
                // Определение языка и мода
                string[] parts = entry.FullName.Split('/');
                Language language = new Language(Path.GetFileNameWithoutExtension(parts[3]));
                Mod mod = new Mod(parts[1], null, null, null);
                if (!mods.TryGetValue(mod, out Mod? cached))
                {
                    mods.Add(mod);
                }
                else
                {
                    mod = cached;
                }

                // Чтение файла словаря
                string json;
                using (Stream stream = await entry.OpenAsync())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        json = await reader.ReadToEndAsync();

                // Десериализация и чтение словаря
                Dictionary<string, string>? dictionary;
                try { dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json); }
                catch { continue; }
                if (dictionary is not null)
                    ReadDictionary(dictionary, mod, language);
            }
        }
        /// <summary>
        ///     Считывает словарь.
        /// </summary>
        /// <param name="dictionary">
        ///     Словарь.
        /// </param>
        /// <param name="mod">
        ///     Мод.
        /// </param>
        /// <param name="language">
        ///     Язык.
        /// </param>
        private static void ReadDictionary(IReadOnlyDictionary<string, string> dictionary, Mod mod, Language language)
        {
            // Сопоставление ключей и значений
            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                // Проверка пары
                if (pair.Key is null || pair.Value is null)
                    continue;

                // Поиск и добавление ключа
                Key key = new Key(pair.Key, mod);
                {
                    Key? cached = mod.Keys.FirstOrDefault(x => x.Equals(key));
                    if (cached is null)
                    {
                        mod.Keys.Add(key);
                    }
                    else
                    {
                        key = cached;
                    }
                }

                // Поиск и обновление значения
                Value value = new Value(pair.Value, language, key);
                {
                    Value? cached = key.Values.FirstOrDefault(x => x.Equals(value));
                    if (cached is null)
                    {
                        key.Values.Add(value);
                    }
                    else
                    {
                        cached.UpdateFromJsonModel(value);
                        value = cached;
                    }
                }
            }
        }
    }
}