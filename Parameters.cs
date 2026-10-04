namespace PrimitiveTranslator
{
    /// <summary>
    ///     Параметры.
    /// </summary>
    internal static class Parameters
    {
        /// <summary>
        ///     Текущий язык перевода.
        /// </summary>
        internal const string CurrentLanguage = nameof(CurrentLanguage);
        /// <summary>
        ///     Языки экспорта, раделённые символами ',' или ';'.
        /// </summary>
        internal const string ExportLanguages = nameof(ExportLanguages);

        /// <summary>
        ///     Папка модов.
        /// </summary>
        internal const string ModsDirectory = nameof(ModsDirectory);
        /// <summary>
        ///     Папка экспорта.
        /// </summary>
        internal const string ExportDirectory = nameof(ExportDirectory);
        /// <summary>
        ///     Файл информации о моде-переводчике.
        /// </summary>
        internal const string TranslationModInfoJson = nameof(TranslationModInfoJson);
        /// <summary>
        ///     Состояние окна.
        /// </summary>
        internal const string WindowState = nameof(WindowState);

        /// <summary>
        ///     Ширина левой панели.
        /// </summary>
        internal const string LeftPanelWidth = nameof(LeftPanelWidth);
        /// <summary>
        ///     Список открытых вкладок.
        /// </summary>
        internal const string OpenedTabs = nameof(OpenedTabs);
        /// <summary>
        ///     Выбранная вкладка.
        /// </summary>
        internal const string SelectedTab = nameof(SelectedTab);
    }
}