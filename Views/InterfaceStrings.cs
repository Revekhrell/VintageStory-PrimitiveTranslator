using Cybrex.Core.Localization;
using System.Runtime.CompilerServices;

namespace PrimitiveTranslator.Views
{
    /// <summary>
    ///     Строки интерфейса.
    /// </summary>
    public sealed class InterfaceStrings
    {
        /// <summary>
        ///     Заголовок главного окна.
        /// </summary>
        public string MainWindowTitle
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Заголовок панели настроек.
        /// </summary>
        public string SettingsHeader
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Надпись текущего языка.
        /// </summary>
        public string LanguageLabel
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Надпись папки модов.
        /// </summary>
        public string ModsDirectoryLabel
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Надпись папки экспорта.
        /// </summary>
        public string ExportDirectoryLabel
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Подсказка кнопки выбора папки.
        /// </summary>
        public string SelectDirectoryTooltip
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Надпись языков экспорта.
        /// </summary>
        public string ExportLanguagesLabel
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Плдсказка кнопки перечитывания модов.
        /// </summary>
        public string ReloadModsTooltip
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Подсказка кнопки экспорта мода.
        /// </summary>
        public string ExportTooltip
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Подсказка кнопки автоматического перевода.
        /// </summary>
        public string TranslateTooltip
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Подсказка кнопки проверки ключей.
        /// </summary>
        public string CheckKeysTooltip
        {
            get => field ??= GetString();
        }
        /// <summary>
        ///     Подсказка кнопки удаления модов.
        /// </summary>
        public string DeleteTooltip
        {
            get => field ??= GetString();
        }

        /// <summary>
        ///     Получает локализованную строку.
        /// </summary>
        /// <param name="property">
        ///     Имя свойства.
        /// </param>
        /// <returns>
        ///     Локализованная строка.
        /// </returns>
        private static string GetString([CallerMemberName] string? property = null)
        {
            return Localizer.Get($"{nameof(InterfaceStrings)}:{property ?? string.Empty}");
        }
    }
}