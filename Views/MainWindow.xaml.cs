using Cybrex.Core.Localization;
using Dragablz;
using GTranslate.Results;
using GTranslate.Translators;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using PrimitiveTranslator.Commands;
using PrimitiveTranslator.Converters.Json;
using PrimitiveTranslator.Data;
using PrimitiveTranslator.Data.Models;
using PrimitiveTranslator.Editors;
using PrimitiveTranslator.Extensions;
using PrimitiveTranslator.IO;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Key = PrimitiveTranslator.Data.Models.Key;
using ExceptionHelper = Cybrex.Core.Exceptions.ExceptionHelper;

namespace PrimitiveTranslator.Views
{
    /// <summary>
    ///     Главное окно.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        ///     Максимальный интервал, который считается двойным кликом.
        /// </summary>
        private readonly TimeSpan DoubleClickInterval;

        /// <summary>
        ///     Временная метка последнего нажатия кнопки мыши на элемент мода.
        /// </summary>
        private DateTime ModsListItemClickTimestamp;

        /// <summary>
        ///     Инициализирует новый экземпляр главного окна.
        /// </summary>
        public MainWindow()
        {
            Localizer.Initialize();
            Localizer.RegisterLocales(typeof(MainWindow).Assembly);
            DoubleClickInterval = TimeSpan.FromMilliseconds(500);
            InitializeComponent();
        }

        /// <summary>
        ///     Обработчик события прокрутки вкладок.
        /// </summary>
        /// <param name="sender">
        ///     Объект, вызвавший событие.
        /// </param>
        /// <param name="e">
        ///     Аргументы события.
        /// </param>
        private void TabItemsControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Проверка аргументов
            if (sender is not DragablzItemsControl control || e is null)
                return;

            // Прокрутка
            ScrollViewer? viewer = UIHelper.FindVisualChild<ScrollViewer>(control);
            viewer?.ScrollToHorizontalOffset(viewer.HorizontalOffset - e.Delta / 5);
        }
        /// <summary>
        ///     Обработчик нажатия клавиш.
        /// </summary>
        /// <param name="sender">
        ///     Объект, вызвавший событие.
        /// </param>
        /// <param name="e">
        ///     Аргументы события.
        /// </param>
        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Проверка аргументов
            if (e is null)
                return;

            // Проверка контекста
            if (DataContext is not MainWindowViewModel vm || !vm.IsFreeContext)
                return;

            // Ctrl + S
            if (e.Key == System.Windows.Input.Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                // Ctrl + Shift + S
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    await vm.SaveAllTabsAsync();
                    return;
                }
                await vm.SaveTabAsync(vm.SelectedTab);
                return;
            }
        }
        /// <summary>
        ///     Обработчик события нажатия кнопки мыши на элемент мода.
        /// </summary>
        /// <param name="sender">
        ///     Объект, вызвавший событие.
        /// </param>
        /// <param name="e">
        ///     Аргументы события.
        /// </param>
        private async void ModsListItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Проверка аргументов
            if (sender is not ListBoxItem item || item.Content is not Mod mod)
                return;
            if (e is null || e.ChangedButton != MouseButton.Left)
                return;

            // Проверка контекста
            if (DataContext is not MainWindowViewModel vm || !vm.IsFreeContext)
                return;

            // Выполнение
            DateTime now = DateTime.UtcNow;
            TimeSpan interval = now - ModsListItemClickTimestamp;
            ModsListItemClickTimestamp = now;
            if (interval <= DoubleClickInterval)
                await vm.EditSelectedModAsync(mod);
        }

        /// <summary>
        ///     Обработчик события загрузки окна.
        /// </summary>
        /// <param name="sender">
        ///     Объект, вызвавший событие.
        /// </param>
        /// <param name="e">
        ///     Аргументы события.
        /// </param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверка контекста
            if (DataContext is not MainWindowViewModel vm)
                return;

            // Установка параметров
            vm.TabControl = TabControl;
            vm.ModsControl = ModsListBox;

            // Настройка прокрутки вкладок
            DragablzItemsControl? items = UIHelper.FindVisualChild<DragablzItemsControl>(TabControl);
            items?.PreviewMouseWheel += TabItemsControl_PreviewMouseWheel;

            // Инициализация из базы данных
            vm.InitializeFromDataBase();
        }
        /// <summary>
        ///     Обработчик события начала закрытия окна.
        /// </summary>
        /// <param name="sender">
        ///     Объект, вызвавший событие.
        /// </param>
        /// <param name="e">
        ///     Аргументы события.
        /// </param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Проверка контекста
            if (DataContext is not MainWindowViewModel vm || vm.StartFailed)
                return;

            // Проверка блокировки
            if (!vm.IsFreeContext)
            {
                e.Cancel = true;
                return;
            }

            // Требование сохранения вкладок
            ReadOnlySpan<JsonEditor> editors = vm.TabsView.SourceCollection.OfType<JsonEditor>().ToArray();
            foreach (JsonEditor editor in editors)
            {
                editor.ExecuteDeferredHandlers();
                if (editor.IsModified)
                {
                    MessageBoxResult result = MessageBox.Show(Localizer.Get("Message:Body:Save"), Localizer.Get("Message:Header:Save"), MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Cancel || (result == MessageBoxResult.Yes && !vm.SaveAllTabs()))
                    {
                        e.Cancel = true;
                        return;
                    }
                    break;
                }
            }

            // Сохранение состояния
            vm.SaveState();
            vm.UnlockApplicationContext();
        }

        /// <summary>
        ///     Получает состояние окна.
        /// </summary>
        /// <returns>
        ///     Состояние.
        /// </returns>
        internal MainWindowState GetState()
        {
            MainWindowState state = new MainWindowState();
            state.State = WindowState;
            state.Width = Width;
            state.Height = Height;
            state.LeftOffset = Left;
            state.TopOffset = Top;
            return state;
        }
        /// <summary>
        ///     Устанавливает состояние окна.
        /// </summary>
        /// <param name="state">
        ///     Состояние.
        /// </param>
        internal void SetState(MainWindowState? state)
        {
            // Проверка аругемнтов
            if (state is null)
                return;

            // Применение состояния
            Action action = () =>
            {
                Left = state.LeftOffset;
                Top = state.LeftOffset;
                Width = state.Width;
                Height = state.Height;
                WindowState = state.State;
            };
            Dispatcher.Invoke(action);
        }
    }

    /// <summary>
    ///     Контекст данных главного окна.
    /// </summary>
    public sealed class MainWindowViewModel : BindableBase
    {
        #region Внутренние данные

        /// <summary>
        ///     Диспатчер потока интерфейса.
        /// </summary>
        private readonly Dispatcher CurrentDispatcher;
        /// <summary>
        ///     Строки интерфейса.
        /// </summary>
        public InterfaceStrings Strings
        {
            get => field ??= new InterfaceStrings();
        }

        #endregion
        #region Блокировка контекста приложения

        /// <summary>
        ///     Имя файла блокировки.
        /// </summary>
        private const string LockFile = "Locker";
        /// <summary>
        ///     Поток блокировки контекста приложения.
        /// </summary>
        private Stream? ApplicationLockerStream;
        /// <summary>
        ///     Флаг ошибки запуска.
        /// </summary>
        public bool StartFailed;

        /// <summary>
        ///     Блокирует контекст приложения.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если блокировка выполнена успешно, иначе – <c>false</c>.
        /// </returns>
        private bool LockApplicationContext()
        {
            try { ApplicationLockerStream = new FileStream(LockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch { return false; }
            return true;
        }
        /// <summary>
        ///     Разблокирует контекст приложения.
        /// </summary>
        internal void UnlockApplicationContext()
        {
            ApplicationLockerStream?.Close();
        }

        #endregion
        #region Контекст данных

        /// <summary>
        ///     Флаг свободного контекста.
        /// </summary>
        [MemberNotNullWhen(false, nameof(ActiveDataContext))]
        public bool IsFreeContext
        {
            get => field;
            private set => SetProperty(ref field, value);
        }

        #region Управление активным контекстом

        /// <summary>
        ///     Активный контекст данных.
        /// </summary>
        private DataContext? ActiveDataContext { get; set; }
        /// <summary>
        ///     Создаёт экземпляр контекста данных, если он не занят.
        /// </summary>
        /// <returns>
        ///     Экземпляр контекста данных
        /// </returns>
        private DataContext? CreateDataContext()
        {
            if (!IsFreeContext)
                return null;

            IsFreeContext = false;
            ActiveDataContext = new DataContext(DataContextOptionsBuilder.Create());
            return ActiveDataContext;
        }
        /// <summary>
        ///     Освобождает активный контекст данных.
        /// </summary>
        /// <returns>
        ///     Задача.
        /// </returns>
        private void FreeDataContext()
        {
            if (IsFreeContext)
                return;

            ActiveDataContext.Dispose();
            ActiveDataContext = null;
            IsFreeContext = true;
        }
        /// <summary>
        ///     Освобождает активный контекст данных.
        /// </summary>
        /// <returns>
        ///     Задача.
        /// </returns>
        private async Task FreeDataContextAsync()
        {
            if (IsFreeContext)
                return;

            await Task.Run(ActiveDataContext.DisposeAsync);
            ActiveDataContext = null;
            IsFreeContext = true;
        }

        #endregion
        #region Загрузка начального состояния из контекста данных

        /// <summary>
        ///     Флаг инициализации приложения из контекста данных.
        /// </summary>
        private bool IsInitializedFromDataBase { get; set; }
        /// <summary>
        ///     Инициализирует приложение из контекста данных.
        /// </summary>
        internal void InitializeFromDataBase()
        {
            // Проверка параметров
            if (IsInitializedFromDataBase)
                return;

            // Создание контекста
            IsInitializedFromDataBase = true;
            DataContext context = CreateDataContext()!;

            // Язык переводов
            Parameter? parameter = context.Parameters.Find(Parameters.CurrentLanguage);
            if (parameter is not null)
                Language = parameter.Value;

            // Языки экспорта
            parameter = context.Parameters.Find(Parameters.ExportLanguages);
            if (parameter is not null)
                Languages = parameter.Value;

            // Папка модов
            parameter = context.Parameters.Find(Parameters.ModsDirectory);
            if (parameter is not null)
                ModsDirectory = parameter.Value;

            // Папка экспорта
            parameter = context.Parameters.Find(Parameters.ExportDirectory);
            if (parameter is not null)
                ExportDirectory = parameter.Value;

            // Ширина левой панели
            parameter = context.Parameters.Find(Parameters.LeftPanelWidth);
            if (parameter is not null)
                LeftPanelWidth = new GridLength(parameter.ToDouble(), GridUnitType.Pixel);

            // Список модов
            Mods.AddRange(context.Mods.Include(x => x.Language).Include(x => x.Languages).AsSplitQuery().ToArray());
            Mods.Sort(Comparer<Mod>.Default);
            ModsView.Refresh();

            // Вкладки
            parameter = context.Parameters.Find(Parameters.OpenedTabs);
            if (parameter is not null)
            {
                // Десериализация
                List<EditorSignature>? signatures = null;
                try { signatures = JsonConvert.DeserializeObject<List<EditorSignature>>(parameter.Value); }
                catch { goto CompleteOpenedTabs; }
                if (signatures is null)
                    goto CompleteOpenedTabs;

                // Создание вкладок
                foreach (EditorSignature signature in signatures)
                {
                    // Вкладка информации мода
                    if (signature.Type == EditorType.ModInfo)
                    {
                        AddTab(new ModInfoEditor(context.Parameters.Find(Parameters.TranslationModInfoJson)?.Value));
                        continue;
                    }

                    // Вкладка словаря
                    if (signature.Type == EditorType.Dictionary)
                    {
                        // Невалидное значение
                        if (signature.ModId is null || signature.OriginalLanuage is null || signature.TranslationLanguage is null)
                            continue;

                        // Создание вкладки
                        Mod? mod = Mods.FirstOrDefault(x => x.UID == signature.ModId);
                        if (mod is null)
                            continue;

                        // Сохранение параметров
                        string current = Language;
                        Language? selected = mod.Language;

                        // Открытие вкладки
                        Language = signature.TranslationLanguage;
                        mod.Language = new Language(signature.OriginalLanuage);
                        EditSelectedMod(mod, context);

                        // Восстановление параметров
                        Language = current;
                        mod.Language = selected;
                        continue;
                    }
                }
            }
            CompleteOpenedTabs:

            // Выбранная вкладка
            parameter = context.Parameters.Find(Parameters.SelectedTab);
            if (parameter is not null)
            {
                // Вкладка не выбрана
                if (string.IsNullOrWhiteSpace(parameter.Value))
                    goto CompleteSelectedTab;

                // Десериализация
                EditorSignature? signature = null;
                try { signature = JsonConvert.DeserializeObject<EditorSignature>(parameter.Value); }
                catch { goto CompleteSelectedTab; }
                if (signature is null)
                    goto CompleteSelectedTab;

                // Поиск и открытие
                JsonEditor? editor = Tabs.FirstOrDefault(x => x.GetSignature().Equals(signature));
                if (editor is not null)
                    SelectedTab = editor;
            }
            CompleteSelectedTab:

            // Состояние окна
            parameter = context.Parameters.Find(Parameters.WindowState);
            if (parameter is not null)
            {
                // Состояние не сохранено
                if (string.IsNullOrWhiteSpace(parameter.Value))
                    goto CompleteWindowState;

                // Десериализация
                MainWindowState? state = null;
                try { state = JsonConvert.DeserializeObject<MainWindowState>(parameter.Value); }
                catch { goto CompleteWindowState; }
                if (state is null)
                    goto CompleteWindowState;

                // Применение
                (Application.Current.MainWindow as MainWindow)?.SetState(state);
            }
            CompleteWindowState:

            // Завершение
            FreeDataContext();
        }

        #endregion
        #region Сохранение состояния

        /// <summary>
        ///     Сохраняет состояние приложения.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если сохранение было выполнено, иначе – <c>false</c>.
        /// </returns>
        public bool SaveState()
        {
            // Проверка состояния
            if (!IsFreeContext)
                return false;

            // Создание контекста
            DataContext? context = CreateDataContext();
            if (context is null)
                return false;

            // Язык переводов
            Parameter? parameter = context.Parameters.Find(Parameters.CurrentLanguage);
            if (parameter is null)
            {
                parameter = new Parameter(Parameters.CurrentLanguage, Language);
                context.Parameters.Add(parameter);
            }
            else
            {
                parameter.Value = Language;
            }

            // Языки экспорта
            parameter = context.Parameters.Find(Parameters.ExportLanguages);
            if (parameter is null)
            {
                parameter = new Parameter(Parameters.ExportLanguages, Languages);
                context.Parameters.Add(parameter);
            }
            else
            {
                parameter.Value = Languages;
            }

            // Папка модов
            parameter = context.Parameters.Find(Parameters.ModsDirectory);
            if (parameter is null)
            {
                parameter = new Parameter(Parameters.ModsDirectory, ModsDirectory);
                context.Parameters.Add(parameter);
            }
            else
            {
                parameter.Value = ModsDirectory;
            }

            // Папка экспорта
            parameter = context.Parameters.Find(Parameters.ExportDirectory);
            if (parameter is null)
            {
                parameter = new Parameter(Parameters.ExportDirectory, ExportDirectory);
                context.Parameters.Add(parameter);
            }
            else
            {
                parameter.Value = ExportDirectory;
            }

            // Ширина левой панели
            parameter = context.Parameters.Find(Parameters.LeftPanelWidth);
            if (parameter is null)
            {
                parameter = new Parameter(Parameters.LeftPanelWidth, LeftPanelWidth.Value);
                context.Parameters.Add(parameter);
            }
            else
            {
                parameter.Value = LeftPanelWidth.Value.ToString(CultureInfo.InvariantCulture);
            }

            // Вкладки
            List<JsonEditor> tabs = TabControl?.GetOrderedHeaders()?.Select(x => x.Content)?.OfType<JsonEditor>().ToList() ?? [];
            List<EditorSignature> signatures = tabs.Select(x => x.GetSignature()).ToList();
            string json = JsonConvert.SerializeObject(signatures);
            parameter = context.Parameters.Find(Parameters.OpenedTabs);
            if (parameter is null)
            {
                parameter = new Parameter(Parameters.OpenedTabs, json);
                context.Parameters.Add(parameter);
            }
            else
            {
                parameter.Value = json;
            }

            // Выбранная вкладка
            json = SelectedTab is not null ? JsonConvert.SerializeObject(SelectedTab.GetSignature()) : string.Empty;
            parameter = context.Parameters.Find(Parameters.SelectedTab);
            if (parameter is null)
            {
                parameter = new Parameter(Parameters.SelectedTab, json);
                context.Parameters.Add(parameter);
            }
            else
            {
                parameter.Value = json;
            }

            // Состояние окна
            parameter = context.Parameters.Find(Parameters.WindowState);
            MainWindowState state = (Application.Current.MainWindow as MainWindow)?.GetState() ?? new MainWindowState();
            if (parameter is null)
            {
                parameter = new Parameter(Parameters.WindowState, JsonConvert.SerializeObject(state));
                context.Parameters.Add(parameter);
            }
            else
            {
                parameter.Value = JsonConvert.SerializeObject(state);
            }

            // Выбранные языки модов
            SaveModsLanguages(context);

            // Завершение
            context.SaveChanges();
            FreeDataContext();
            return true;
        }
        /// <summary>
        ///     Сохраняет состояние приложения.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если сохранение было выполнено, иначе – <c>false</c>.
        /// </returns>
        public async Task<bool> SaveStateAsync()
        {
            return await Task.Run(SaveState);
        }

        /// <summary>
        ///     Сохраняет выбранные языки модов.
        /// </summary>
        /// <param name="context">
        ///     Контекст данных.
        /// </param>
        private void SaveModsLanguages(DataContext context)
        {
            SortedSet<Language> cachedLanguages = new SortedSet<Language>(context.Languages.ToArray(), Comparer<Language>.Default);
            SortedSet<Mod> cachedMods = new SortedSet<Mod>(context.Mods.Include(x => x.Language).AsSplitQuery().ToArray(), Comparer<Mod>.Default);
            for (int i = 0; i < Mods.Count; ++i)
            {
                Mod mod = Mods[i];
                if (cachedMods.TryGetValue(mod, out Mod? cachedMod))
                {
                    if (mod.Language is not null)
                    {
                        cachedLanguages.TryGetValue(mod.Language, out Language? cachedLanguage);
                        cachedMod.Language = cachedLanguage;
                    }
                    else
                    {
                        cachedMod.Language = null;
                    }
                }
            }
        }

        #endregion

        #endregion
        #region Языки

        /// <summary>
        ///     Язык перевода.
        /// </summary>
        public string Language
        {
            get => field ??= string.Empty;
            set => SetProperty(ref field, value?.ToLower());
        }
        /// <summary>
        ///     Языки экспорта.
        /// </summary>
        public string Languages
        {
            get => field ??= string.Empty;
            set => SetProperty(ref field, value?.ToLower());
        }

        #endregion
        #region Папка модов

        /// <summary>
        ///     Папка модов.
        /// </summary>
        public string ModsDirectory
        {
            get => field ??= string.Empty;
            set => SetProperty(ref field, value);
        }

        #region Команда

        /// <summary>
        ///     Команда открытия папки модов.
        /// </summary>
        public ICommand OpenModsDirectoryCommand
        {
            get => field ??= new DependentDelegateCommand(OnOpenModsDirectoryCommandExecuted, CanOpenModsDirectoryCommandExecute, this);
        }
        /// <summary>
        ///     Определяет, может ли <c><see cref="OpenModsDirectoryCommand"/></c> выполняться.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если команду можно выполнять, иначе – <c>false</c>.
        /// </returns>
        private bool CanOpenModsDirectoryCommandExecute()
        {
            return true;
        }
        /// <summary>
        ///     Открывает папку с модами.
        /// </summary>
        private async void OnOpenModsDirectoryCommandExecuted()
        {
            // Подготовка диалогового окна
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (Directory.Exists(ModsDirectory))
                dialog.InitialDirectory = ModsDirectory;

            // Выбор папки
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result != CommonFileDialogResult.Ok)
                return;

            // Проверка папки
            if (!Directory.Exists(dialog.FileName))
                return;

            // Чтение модов
            ModsDirectory = dialog.FileName;
            if (ReloadModsCommand.CanExecute(null))
                ReloadModsCommand.Execute(null);
        }

        #endregion

        #endregion
        #region Папка экспорта

        /// <summary>
        ///     Папка экспорта.
        /// </summary>
        public string ExportDirectory
        {
            get => field ??= string.Empty;
            set => SetProperty(ref field, value);
        }

        #region Команда

        /// <summary>
        ///     Команда открытия папки экспорта.
        /// </summary>
        public ICommand OpenExportDirectoryCommand
        {
            get => field ??= new DependentDelegateCommand(OnOpenExportDirectoryCommandExecuted, CanOpenExportDirectoryCommandExecute, this);
        }
        /// <summary>
        ///     Определяет, может ли <c><see cref="OpenExportDirectoryCommand"/></c> выполняться.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если команду можно выполнять, иначе – <c>false</c>.
        /// </returns>
        private bool CanOpenExportDirectoryCommandExecute()
        {
            return true;
        }
        /// <summary>
        ///     Открывает папку экспорта.
        /// </summary>
        private async void OnOpenExportDirectoryCommandExecuted()
        {
            // Подготовка диалогового окна
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (Directory.Exists(ExportDirectory))
                dialog.InitialDirectory = ExportDirectory;

            // Выбор папки
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result != CommonFileDialogResult.Ok)
                return;

            // Проверка папки
            if (!Directory.Exists(dialog.FileName))
                return;

            // Установка значения
            ExportDirectory = dialog.FileName;
        }

        #endregion

        #endregion
        #region Ширина левой панели

        /// <summary>
        ///     Ширина левой панели.
        /// </summary>
        public GridLength LeftPanelWidth
        {
            get => field;
            set => SetProperty(ref field, value);
        }

        #endregion
        #region Список модов

        /// <summary>
        ///     Элемент управления списком модов.
        /// </summary>
        internal ListBox? ModsControl;

        /// <summary>
        ///     Список модов.
        /// </summary>
        private readonly List<Mod> Mods;
        /// <summary>
        ///     Список модов.
        /// </summary>
        public ICollectionView ModsView
        {
            get
            {
                if (field is not null)
                    return field;

                ICollectionView view = CollectionViewSource.GetDefaultView(Mods);
                view.Filter = CanViewMod;
                return field = view;
            }
        }
        /// <summary>
        ///     Индекс выбранного мода.
        /// </summary>
        public int SelectedModIndex
        {
            get => field;
            set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Определяет, нужно ли отображать мод.
        /// </summary>
        /// <param name="obj">
        ///     Мод.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если мод нужно отобразить, иначе – <c>false</c>.
        /// </returns>
        private bool CanViewMod(object? obj)
        {
            return obj is Mod mod && mod.Languages.Count > 0;
        }

        /// <summary>
        ///     Открывает словарь текущего мода.
        /// </summary>
        /// <param name="mod">
        ///     Мод.
        /// </param>
        public void EditSelectedMod(Mod? mod)
        {
            // Мод не выбран
            if (mod is null)
                return;

            // Получение контекста
            DataContext? context = CreateDataContext();
            if (context is null)
                return;

            // Открытие
            EditSelectedMod(mod, context);
            FreeDataContext();
        }
        /// <summary>
        ///     Открывает словарь текущего мода.
        /// </summary>
        /// <param name="mod">
        ///     Мод.
        /// </param>
        /// <param name="context">
        ///     Контекст данных.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="mod"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="context"/></c> равен <c>null</c>.
        /// </exception>
        private void EditSelectedMod(Mod mod, DataContext context)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(mod, nameof(mod));
            ExceptionHelper.ThrowIfArgumentIsNull(context, nameof(context));

            // Язык мода не выбран или тот же
            Language language = new Language(Language);
            if (mod.Language is null || EqualityComparer<Language>.Default.Equals(language, mod.Language))
                return;

            // Поиск вкладки
            JsonEditor? editor = Tabs.FirstOrDefault(x => x is DictionaryEditor de && de.ModId == mod.UID && de.OriginalLanuage.Equals(mod.Language) && de.TranslationLanguage.Equals(language));
            if (editor is not null)
            {
                SelectedTab = editor;
                return;
            }

            // Поиск мода
            Mod? cached = context.Mods.Include(x => x.Keys).ThenInclude(x => x.Values).ThenInclude(x => x.Language)
                                      .Include(x => x.Languages).Include(x => x.Language).AsSplitQuery()
                                      .FirstOrDefault(x => x.UID == mod.UID);
            if (cached is null)
                return;

            // Формирование словарей
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            List<string> modified = new List<string>();
            List<string> @new = new List<string>();
            foreach (Key key in cached.Keys)
            {
                // Поиск оригинального значения
                Value? originalValue = key.Values.FirstOrDefault(x => EqualityComparer<Language>.Default.Equals(x.Language, mod.Language));
                if (originalValue is null)
                    continue;

                // Поиск переведённого значения
                Value? translatedValue = key.Values.FirstOrDefault(x => EqualityComparer<Language>.Default.Equals(x.Language, language));
                if (translatedValue is not null)
                {
                    if (translatedValue.Timestamp < originalValue.Timestamp)
                    {
                        string value = $"{originalValue.Text}{DictionaryEditor.ModifiedValuesSeparator}{translatedValue.Text}";
                        dictionary.Add(key.Text, value);
                        modified.Add(key.Text);
                    }
                    else
                    {
                        dictionary.Add(key.Text, translatedValue.Text);
                    }
                }
                else
                {
                    dictionary.Add(key.Text, originalValue.Text);
                    @new.Add(key.Text);
                }
            }

            // Добавление вкладки
            CurrentDispatcher.Invoke(() => AddTab(new DictionaryEditor(mod.UID, mod.Language, language, dictionary, modified, @new)));
        }
        /// <summary>
        ///     Открывает словарь текущего мода.
        /// </summary>
        /// <param name="mod">
        ///     Мод.
        /// </param>
        /// <returns>
        ///     Задача.
        /// </returns>
        public async Task EditSelectedModAsync(Mod? mod)
        {
            await Task.Run(() => EditSelectedMod(mod));
        }

        #endregion
        #region modinfo.json

        /// <summary>
        ///     Команда открытия файла информации мода-переводчика.
        /// </summary>
        public ICommand EditModInfoCommand
        {
            get => field ??= new DependentDelegateCommand(OnEditModInfoCommandExecuted, CanEditModInfoCommandExecute, this, nameof(IsFreeContext));
        }
        /// <summary>
        ///     Определяет, может ли <c><see cref="EditModInfoCommand"/></c> выполняться.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если команду можно выполнять, иначе – <c>false</c>.
        /// </returns>
        private bool CanEditModInfoCommandExecute()
        {
            return IsFreeContext;
        }
        /// <summary>
        ///     Открывает "modinfo.json" для редактирования.
        /// </summary>
        private async void OnEditModInfoCommandExecuted()
        {
            // Поиск вкладки
            JsonEditor? editor = Tabs.FirstOrDefault(x => x is ModInfoEditor);
            if (editor is not null)
            {
                SelectedTab = editor;
                return;
            }

            // Получение контекста
            DataContext? context = CreateDataContext();
            if (context is null)
                return;

            // Создание вкладки
            Parameter? parameter = await context.Parameters.FindAsync(Parameters.TranslationModInfoJson);
            AddTab(new ModInfoEditor(parameter?.Value));
            await FreeDataContextAsync();
        }

        #endregion
        #region Панель команд

        #region Перезагрузка модов

        /// <summary>
        ///     Команда перезагрузки модов.
        /// </summary>
        public ICommand ReloadModsCommand
        {
            get => field ??= new DependentDelegateCommand(OnReloadModsCommandExecuted, CanReloadModsCommandExecute, this, nameof(ActiveTranslation), nameof(IsFreeContext));
        }
        /// <summary>
        ///     Определяет, может ли <c><see cref="ReloadModsCommand"/></c> выполняться.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если команду можно выполнять, иначе – <c>false</c>.
        /// </returns>
        private bool CanReloadModsCommandExecute()
        {
            return !ActiveTranslation && IsFreeContext;
        }
        /// <summary>
        ///     Считывает моды из папки и обновляет базу с интерфейсом.
        /// </summary>
        private async void OnReloadModsCommandExecuted()
        {
            // Диалоговое окно
            MessageBoxResult result = MessageBox.Show(Localizer.Get("Message:Body:Load"), Localizer.Get("Message:Header:Load"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            // Получение контекста
            DataContext? context = CreateDataContext();
            if (context is null)
                return;

            // Десериализация мода переводчика
            Mod? translator = null;
            Parameter? modinfo = context.Parameters.Find(Parameters.TranslationModInfoJson);
            if (!string.IsNullOrWhiteSpace(modinfo?.Value))
            {
                try { translator = JsonConvert.DeserializeObject<Mod>(modinfo.Value, ModConverter.Instance); }
                catch { }
            }

            // Получение архивов
            IReadOnlyList<string> archives = ModReader.GetArchives(ModsDirectory);

            // Чтение модов
            ModsProgressBarValue = 0;
            ModsProgressBarMaximum = archives.Count;
            ModsProgressBarVisibility = Visibility.Visible;
            ISet<Mod> mods = await Task.Run(() => ModReader.ReadMods(archives, translator?.UID, OnModArchiveProcessed));

            // Синхронизация базы данных
            ModsProgressBarValue = 0;
            ModsProgressBarMaximum = mods.Count;
            await Task.Run(() => ModUpdater.UpdateDataBase(context, mods, OnModSyncProcessed));
            await Task.Run(() => context.SaveChangesAsync());

            // Синхронизация интерфейса
            await Task.Run(() => ModUpdater.UpdateUI(context, Mods));
            ModsProgressBarVisibility = Visibility.Collapsed;
            ModsView.Refresh();

            // Завершение
            await FreeDataContextAsync();
        }

        #endregion
        #region Экспорт мода-переводчика

        /// <summary>
        ///     Команда экспорта мода.
        /// </summary>
        public ICommand ExportCommand
        {
            get => field ??= new DependentDelegateCommand(OnExportCommandExecuted, CanExportCommandExecute, this, nameof(ActiveTranslation), nameof(IsFreeContext));
        }
        /// <summary>
        ///     Определяет, может ли <c><see cref="ExportCommand"/></c> выполняться.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если команду можно выполнять, иначе – <c>false</c>.
        /// </returns>
        private bool CanExportCommandExecute()
        {
            return !ActiveTranslation && IsFreeContext;
        }
        /// <summary>
        ///     Считывает моды из папки и обновляет базу с интерфейсом.
        /// </summary>
        private async void OnExportCommandExecuted()
        {
            // Диалоговое окно
            MessageBoxResult result = MessageBox.Show(Localizer.Get("Message:Body:Export"), Localizer.Get("Message:Header:Export"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            // Экспорт
            await Task.Run(Export);
        }

        /// <summary>
        ///     Экспортирует мод-переводчик.
        /// </summary>
        private void Export()
        {
            // Проверка папки экспорта
            if (!Directory.Exists(ExportDirectory))
                return;

            // Языки экспорта
            ReadOnlySpan<Language> languages = Languages.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                                                        .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => new Language(x))
                                                        .Distinct().ToArray();
            if (languages.Length == 0)
                return;

            // Поиск и чтение иконки
            string modIconPath = Path.Combine(ExportDirectory, "modicon.png");
            FileStream? modIconStream = null;
            if (File.Exists(modIconPath))
            {
                try { modIconStream = new FileStream(modIconPath, FileMode.Open, FileAccess.Read, FileShare.Read); }
                catch { }
            }

            // Получение контекста
            DataContext? context = CreateDataContext();
            if (context is null)
                goto Complete;

            // Десериализация информации
            Mod? translator = null;
            Parameter? modinfo = context.Parameters.Find(Parameters.TranslationModInfoJson);
            if (!string.IsNullOrWhiteSpace(modinfo?.Value))
            {
                try { translator = JsonConvert.DeserializeObject<Mod>(modinfo.Value, ModConverter.Instance); }
                catch { }
            }
            if (translator is null)
                goto Complete;

            // Формирование потока архива
            FileStream? stream = null;
            string archivePath = Path.Combine(ExportDirectory, translator.UID + (translator.Version is null ? string.Empty : $"_{translator.Version}") + ".zip");
            try { stream = new FileStream(archivePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read); }
            catch { goto Complete; }

            // Загрузка модов
            ExportProgressBarValue = 0;
            ExportProgressBarVisibility = Visibility.Visible;
            ReadOnlySpan<Mod> mods = context.Mods.Include(x => x.Keys).ThenInclude(x => x.Values).ThenInclude(x => x.Language)
                                                 .Include(x => x.Languages).Include(x => x.Language).AsSplitQuery().ToArray();

            // Формирование архива
            ExportProgressBarMaximum = mods.Length;
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                // Информация мода
                ZipArchiveEntry entry = archive.CreateEntry("modinfo.json");
                using (Stream entryStream = entry.Open())
                    using (StreamWriter writer = new StreamWriter(entryStream, Encoding.UTF8))
                        writer.Write(modinfo!.Value);

                // Иконка мода
                if (modIconStream is not null)
                {
                    entry = archive.CreateEntry("modicon.png");
                    using (Stream entryStream = entry.Open())
                        modIconStream.CopyTo(entryStream);
                }

                // Формирование словарей
                foreach (Language language in languages)
                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    foreach (Mod mod in mods)
                    {
                        // Заполнение словаря
                        dictionary.Clear();
                        foreach (Key key in mod.Keys)
                        {
                            Value? value = key.Values.FirstOrDefault(x => x.Language!.Equals(language));
                            if (value is not null)
                                dictionary[key.Text] = value.Text;
                        }
                        if (dictionary.Count == 0)
                            goto ModComplete;

                        // Запись
                        entry = archive.CreateEntry($"assets/{mod.UID}/lang/{language}.json");
                        using (Stream entryStream = entry.Open())
                            using (StreamWriter writer = new StreamWriter(entryStream, Encoding.UTF8))
                                writer.Write(JsonConvert.SerializeObject(dictionary, Formatting.Indented));

                    // Завершение
                    ModComplete:
                        ExportProgressStep();
                    }
                }
            }
            ExportProgressBarVisibility = Visibility.Collapsed;

        // Завершение
        Complete:
            modIconStream?.Close();
            FreeDataContext();
        }

        #endregion
        #region Автоматичсекий перевод

        /// <summary>
        ///     Флаг активного процесса перевода.
        /// </summary>
        public bool ActiveTranslation
        {
            get => field;
            private set => SetProperty(ref field, value);
        }

        /// <summary>
        ///     Команда автоматического перевода.
        /// </summary>
        public ICommand TranslateCommand
        {
            get => field ??= new DependentDelegateCommand(OnTranslateCommandExecuted, CanTranslateCommandExecute, this, nameof(ActiveTranslation), nameof(SelectedTab));
        }
        /// <summary>
        ///     Определяет, может ли <c><see cref="TranslateCommand"/></c> выполняться.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если команду можно выполнять, иначе – <c>false</c>.
        /// </returns>
        private bool CanTranslateCommandExecute()
        {
            return !ActiveTranslation && SelectedTab is DictionaryEditor;
        }
        /// <summary>
        ///     Переводит открытый словарь автоматически.
        /// </summary>
        private async void OnTranslateCommandExecuted()
        {
            // Диалоговое окно
            MessageBoxResult result = MessageBox.Show(Localizer.Get("Message:Body:Translate"), Localizer.Get("Message:Header:Translate"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            // Перевод
            await Task.Run(Translate);
        }

        /// <summary>
        ///     Переводит открытый словарь.
        /// </summary>
        /// <returns>
        ///     Задача.
        /// </returns>
        private async Task Translate()
        {
            // Проверка редактора
            if (SelectedTab is not DictionaryEditor editor || editor.KeysToTranslate.Count == 0)
                return;

            // Десериализация
            if (!editor.TryDeserializeJson(out Dictionary<string, string>? dictionary))
                return;

            // Перевод
            try
            {
                // Переводчик
                GTranslate.ILanguage from = GTranslate.Language.GetLanguage(editor.OriginalLanuage.Name);
                GTranslate.ILanguage to = GTranslate.Language.GetLanguage(editor.TranslationLanguage.Name);
                using AggregateTranslator translator = new AggregateTranslator();

                // Перевод
                ActiveTranslation = true;
                ExportProgressBarValue = 0;
                ExportProgressBarMaximum = editor.KeysToTranslate.Count;
                ExportProgressBarVisibility = Visibility.Visible;
                foreach (string key in editor.KeysToTranslate)
                {
                    // Извлечение
                    if (!dictionary.TryGetValue(key, out string? value))
                        goto KeyComplete;

                    // Извлечение непереведённой части
                    int index = value.IndexOf(DictionaryEditor.ModifiedValuesSeparator);
                    if (index >= 0)
                        value = value.Remove(index);

                    // Перевод
                    ITranslationResult result = await translator.TranslateAsync(value, to, from).WaitAsync(TimeSpan.FromSeconds(20));
                    if (!string.IsNullOrWhiteSpace(result.Translation))
                        dictionary[key] = result.Translation;

                // Завершение
                KeyComplete:
                    ExportProgressStep();
                }

                // Применение
                editor.Text = JsonConvert.SerializeObject(dictionary, Formatting.Indented);
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Localizer.Get("Message:Body:Translate:Timeout"), Localizer.Get("Message:Header:Error"), MessageBoxButton.OK);
            }
            catch
            {
                MessageBox.Show(Localizer.Get("Message:Body:Translate:Unknown"), Localizer.Get("Message:Header:Error"), MessageBoxButton.OK);
            }
            finally
            {
                ExportProgressBarVisibility = Visibility.Collapsed;
                ActiveTranslation = false;
            }
        }

        #endregion
        #region Проверка ключей

        /// <summary>
        ///     Команда проверки ключей.
        /// </summary>
        public ICommand ChekKeysCommand
        {
            get => field ??= new DependentDelegateCommand(OnChekKeysCommandExecuted, CanChekKeysCommandExecute, this, nameof(IsFreeContext));
        }
        /// <summary>
        ///     Определяет, может ли <c><see cref="ChekKeysCommand"/></c> выполняться.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если команду можно выполнять, иначе – <c>false</c>.
        /// </returns>
        private bool CanChekKeysCommandExecute()
        {
            return IsFreeContext;
        }
        /// <summary>
        ///     Проверяет моды на новые или модифицированные ключи.
        /// </summary>
        private async void OnChekKeysCommandExecuted()
        {
            // Диалоговое окно
            MessageBoxResult result = MessageBox.Show(Localizer.Get("Message:Body:CheckKeys"), Localizer.Get("Message:Header:Check"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            // Перевод
            await Task.Run(CheckKeys);
        }

        /// <summary>
        ///     Выделяет моды с новыми или модифицированными ключами.
        /// </summary>
        private void CheckKeys()
        {
            // Проверка языка
            Language language = new Language(Language);
            if (string.IsNullOrEmpty(Language))
                return;

            // Получение контекста
            DataContext? context = CreateDataContext();
            if (context is null)
                return;

            // Загрузка данных
            SortedSet<Language> cachedLanguages = new SortedSet<Language>(context.Languages.ToArray(), Comparer<Language>.Default);
            SortedSet<Mod> cachedMods = new SortedSet<Mod>(context.Mods.Include(x => x.Keys).ThenInclude(x => x.Values).ThenInclude(x => x.Language).Include(x => x.Languages).Include(x => x.Language).AsSplitQuery().ToArray(), Comparer<Mod>.Default);

            // Проверка модов
            foreach (Mod mod in Mods)
            {
                // Поиск мода
                if (!cachedMods.TryGetValue(mod, out Mod? cachedMod))
                    continue;

                // Язык не выбран или языки совпадают
                if (mod.Language is null || language.Equals(mod.Language))
                    continue;

                // Обновление языка
                if (!Equals(mod.Language, cachedMod.Language))
                {
                    // Поиск языка
                    if (!cachedLanguages.TryGetValue(mod.Language, out Language? cachedLanguage))
                    {
                        cachedLanguage = new Language(mod.Language.Name);
                        context.Languages.Add(cachedLanguage);
                        cachedLanguages.Add(cachedLanguage);
                    }

                    // Обновление
                    cachedMod.Language = cachedLanguage;
                    if (!cachedMod.Languages.Any(x => x.Equals(cachedLanguage)))
                        cachedMod.Languages.Add(cachedLanguage);
                }

                // Проверка ключей
                bool hasChangedKeys = false;
                foreach (Key key in cachedMod.Keys)
                {
                    // Получение оригинала
                    Value? original = key.Values.FirstOrDefault(x => x.Language?.Equals(cachedMod.Language) == true);
                    if (original is null)
                        continue;

                    // Получение перевода
                    Value? translated = key.Values.FirstOrDefault(x => x.Language?.Equals(language) == true);
                    if (translated is null || original.Timestamp > translated.Timestamp)
                    {
                        hasChangedKeys = true;
                        break;
                    }
                }
                mod.HasKeysChanges = hasChangedKeys;
            }

            // Завершение
            FreeDataContext();
        }

        #endregion
        #region Удаление

        /// <summary>
        ///     Команда удаления модов.
        /// </summary>
        public ICommand DeleteCommand
        {
            get => field ??= new DependentDelegateCommand(OnDeleteCommandExecuted, CanDeleteCommandExecute, this, nameof(ActiveTranslation), nameof(IsFreeContext));
        }
        /// <summary>
        ///     Определяет, может ли <c><see cref="DeleteCommand"/></c> выполняться.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если команду можно выполнять, иначе – <c>false</c>.
        /// </returns>
        private bool CanDeleteCommandExecute()
        {
            return !ActiveTranslation && IsFreeContext;
        }
        /// <summary>
        ///     Удаляет выделенные моды.
        /// </summary>
        private async void OnDeleteCommandExecuted()
        {
            // Проверка параметров
            if (ModsControl is null)
                return;

            // Диалоговое окно
            MessageBoxResult result = MessageBox.Show(Localizer.Get("Message:Body:Delete"), Localizer.Get("Message:Header:Delete"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            // Перевод
            SortedSet<Mod> mods = new SortedSet<Mod>(ModsControl.SelectedItems.OfType<Mod>(), Comparer<Mod>.Default);
            await Task.Run(() => Delete(mods));
            ModsView.Refresh();
        }

        /// <summary>
        ///     Переводит открытый словарь.
        /// </summary>
        /// <param name="mods">
        ///     Моды для удаления.
        /// </param>
        /// <returns>
        ///     Задача.
        /// </returns>
        private async Task Delete(SortedSet<Mod> mods)
        {
            // Проверка аргументов
            if (mods is null || mods.Count == 0)
                return;

            // Получение контекста
            DataContext? context = CreateDataContext();
            if (context is null)
                return;

            // Загрузка модов
            SortedSet<Mod> cachedMods = new SortedSet<Mod>(context.Mods.ToArray(), Comparer<Mod>.Default);

            // Удаление
            foreach (Mod mod in mods)
            {
                Mods.Remove(mod);
                if (cachedMods.TryGetValue(mod, out Mod? cached))
                    context.Mods.Remove(cached);
            }

            // Завершение
            context.SaveChanges();
            FreeDataContext();
        }

        #endregion

        #endregion
        #region Полоса прогресса загрузки модов

        /// <summary>
        ///     Текущее значение прогресса загрузки модов.
        /// </summary>
        public long ModsProgressBarValue
        {
            get => field;
            set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Максимальное значение прогресса загрузки модов.
        /// </summary>
        public long ModsProgressBarMaximum
        {
            get => field;
            set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Отображение прогресса загрузки модов.
        /// </summary>
        public Visibility ModsProgressBarVisibility
        {
            get => field;
            set => SetProperty(ref field, value);
        }
        
        /// <summary>
        ///     Увеличивает на единицу прогресс загрузки модов.
        /// </summary>
        private void ModsProgressStep()
        {
            long next = ModsProgressBarValue + 1;
            if (next <= ModsProgressBarMaximum)
                ModsProgressBarValue = next;
        }
        /// <summary>
        ///     Обрабатывает событие чтения архива мода.
        /// </summary>
        /// <param name="path">
        ///     Путь к архиву мода.
        /// </param>
        private void OnModArchiveProcessed(string path)
        {
            ModsProgressStep();
        }
        /// <summary>
        ///     Обрабатывает событие синхронизации мода с базой данных.
        /// </summary>
        /// <param name="mod">
        ///     Мод.
        /// </param>
        private void OnModSyncProcessed(Mod mod)
        {
            ModsProgressStep();
        }

        #endregion
        #region Полоса прогресса экспорта мода

        /// <summary>
        ///     Текущее значение прогресса экспорта мода.
        /// </summary>
        public long ExportProgressBarValue
        {
            get => field;
            set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Максимальное значение прогресса экспорта мода.
        /// </summary>
        public long ExportProgressBarMaximum
        {
            get => field;
            set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Отображение прогресса экспорта мода.
        /// </summary>
        public Visibility ExportProgressBarVisibility
        {
            get => field;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        ///     Увеличивает на единицу прогресс экспорта мода.
        /// </summary>
        private void ExportProgressStep()
        {
            long next = ExportProgressBarValue + 1;
            if (next <= ExportProgressBarMaximum)
                ExportProgressBarValue = next;
        }

        #endregion
        #region Вкладки

        /// <summary>
        ///     Элемент управления вкладками.
        /// </summary>
        internal TabablzControl? TabControl;
        /// <summary>
        ///     Вкладки.
        /// </summary>
        private readonly List<JsonEditor> Tabs;
        /// <summary>
        ///     Вкладки.
        /// </summary>
        public ICollectionView TabsView
        {
            get => field ??= CollectionViewSource.GetDefaultView(Tabs);
        }
        /// <summary>
        ///     Выделенная вкладка.
        /// </summary>
        public JsonEditor? SelectedTab
        {
            get => field;
            set
            {
                SetProperty(ref field, value);
                CurrentDispatcher.Invoke(() => TabControl?.StyleTabHeaders());
            }
        }

        #region Управление вкладками

        /// <summary>
        ///     Добавляет вкладку.
        /// </summary>
        /// <param name="editor">
        ///     Редактор.
        /// </param>
        public void AddTab(JsonEditor editor)
        {
            RefreshTabsOrder();
            Tabs.Add(editor);
            TabsView.Refresh();
            SelectedTab = editor;
            TabControl?.FollowTabs(OnTabMouseButton);
        }
        /// <summary>
        ///     Удаляет вкладку.
        /// </summary>
        /// <param name="editor">
        ///     Редактор.
        /// </param>
        public void RemoveTab(JsonEditor editor)
        {
            // Сортировка
            RefreshTabsOrder();

            // Определение выделенной вкладки
            JsonEditor? selected = null;
            if (ReferenceEqualityComparer.Instance.Equals(SelectedTab, editor))
            {
                int index = Tabs.IndexOf(editor);
                if (Tabs.Count > 1)
                    selected = Tabs[index + (index < Tabs.Count - 1 ? 1 : -1)];
            }
            else
            {
                selected = SelectedTab;
            }

            // Удаление
            Tabs.Remove(editor);
            editor.Dispose();

            // Обновление
            CurrentDispatcher.Invoke(() =>
            {
                TabsView.Refresh();
                SelectedTab = selected;
                TabControl?.FollowTabs(OnTabMouseButton);
            }, DispatcherPriority.Loaded);
        }

        /// <summary>
        ///     Обновляет порядок вкладок.
        /// </summary>
        private void RefreshTabsOrder()
        {
            // Проверка параметров
            if (TabControl is null)
                return;

            // Сортировка
            Action action = () =>
            {
                IEnumerable<DragablzItem> items = TabControl.GetOrderedHeaders();
                Tabs.Clear();
                Tabs.AddRange(items.Select(x => x.Content as JsonEditor).OfType<JsonEditor>());
            };
            if (object.ReferenceEquals(CurrentDispatcher, Dispatcher.CurrentDispatcher))
                action();
            else
                CurrentDispatcher.Invoke(action);
        }
        /// <summary>
        ///     Обработчик события нажатия на вкладку.
        /// </summary>
        /// <param name="sender">
        ///     Объект, вызвавший событие.
        /// </param>
        /// <param name="e">
        ///     Аргументы события.
        /// </param>
        private async void OnTabMouseButton(object sender, MouseButtonEventArgs e)
        {
            // Проверка аргументов
            if (e?.MiddleButton != MouseButtonState.Pressed || sender is not DragablzItem item)
                return;
            if (TabControl is null || ActiveTranslation)
                return;

            // Получение вкладки
            JsonEditor? editor = Tabs.FirstOrDefault(x => ReferenceEqualityComparer.Instance.Equals(x, item.Content));
            if (editor is null)
                return;

            // Сохранение
            editor.ExecuteDeferredHandlers();
            if (editor.IsModified)
            {
                MessageBoxResult mbResult = MessageBox.Show(Localizer.Get("Message:Body:SaveDictionary", editor.Title), Localizer.Get("Message:Header:Save"), MessageBoxButton.YesNoCancel, MessageBoxImage.Information);
                if (mbResult == MessageBoxResult.Cancel || (mbResult == MessageBoxResult.Yes && !await SaveTabAsync(editor)))
                    return;
            }

            // Удаление
            RemoveTab(editor);
        }

        #endregion
        #region Сохранение

        /// <summary>
        ///     Сохраняет вкладку.
        /// </summary>
        /// <param name="editor">
        ///     Редактор вкладки.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если сохранение было выполнено, иначе – <c>false</c>.
        /// </returns>
        public bool SaveTab(JsonEditor? editor)
        {
            // Определение вкладки
            editor ??= SelectedTab;
            if (editor is null)
                return true;

            // Информация о моде
            if (editor is ModInfoEditor modEditor)
                return SaveModInfoTab(modEditor);

            // Словарь
            if (editor is DictionaryEditor dictionaryEditor)
                return SaveDictionaryTab(dictionaryEditor);

            return false;
        }
        /// <summary>
        ///     Сохраняет вкладку.
        /// </summary>
        /// <param name="editor">
        ///     Редактор вкладки.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если сохранение было выполнено, иначе – <c>false</c>.
        /// </returns>
        public async Task<bool> SaveTabAsync(JsonEditor? editor)
        {
            return await Task.Run(() => SaveTab(editor));
        }

        /// <summary>
        ///     Сохраняет все вкладки.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если все вкладки были успешно сохранены, иначе – <c>false</c>.
        /// </returns>
        public bool SaveAllTabs()
        {
            bool result = true;
            for (int i = 0; i < Tabs.Count; ++i)
                result &= SaveTab(Tabs[i]);

            return result;
        }
        /// <summary>
        ///     Сохраняет все вкладки.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, если все вкладки были успешно сохранены, иначе – <c>false</c>.
        /// </returns>
        public async Task<bool> SaveAllTabsAsync()
        {
            return await Task.Run(SaveAllTabs);
        }

        /// <summary>
        ///     Сохраняет вкладку.
        /// </summary>
        /// <param name="editor">
        ///     Редактор вкладки.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если сохранение было выполнено, иначе – <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="editor"/></c> равен <c>null</c>.
        /// </exception>
        private bool SaveModInfoTab(ModInfoEditor editor)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(editor, nameof(editor));

            // Проверка необходимости сохранения
            editor.ExecuteDeferredHandlers();
            if (!editor.IsModified)
                return true;

            // Десериализация
            if (!editor.TryDeserializeJson(out Mod? _, ModConverter.Instance))
            {
                MessageBox.Show(Localizer.Get("Message:Body:Error:Save:JsonSyntaxTab", editor.Title), Localizer.Get("Message:Header:Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Получение контекста
            DataContext? context = CreateDataContext();
            if (context is null)
                return false;

            // Обновление параметра
            Parameter? parameter = context.Parameters.Find(Parameters.TranslationModInfoJson);
            if (parameter is null)
            {
                parameter = new Parameter(Parameters.TranslationModInfoJson, editor.Text);
                context.Parameters.Add(parameter);
            }
            else
            {
                parameter.Value = editor.Text;
            }

            // Применение изменений
            context.SaveChanges();
            FreeDataContext();
            editor.ApplyChanges();
            return true;
        }
        /// <summary>
        ///     Сохраняет вкладку.
        /// </summary>
        /// <param name="editor">
        ///     Редактор вкладки.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если сохранение было выполнено, иначе – <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="editor"/></c> равен <c>null</c>.
        /// </exception>
        private async Task<bool> SaveModInfoTabAsync(ModInfoEditor editor)
        {
            return await Task.Run(() => SaveModInfoTab(editor));
        }

        /// <summary>
        ///     Сохраняет вкладку.
        /// </summary>
        /// <param name="editor">
        ///     Редактор вкладки.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если сохранение было выполнено, иначе – <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="editor"/></c> равен <c>null</c>.
        /// </exception>
        private bool SaveDictionaryTab(DictionaryEditor editor)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(editor, nameof(editor));

            // Проверка необходимости сохранения
            editor.ExecuteDeferredHandlers();
            if (!editor.IsModified)
                return true;

            // Десериализация
            if (!editor.TryDeserializeJson(out Dictionary<string, string>? dictionary))
            {
                MessageBox.Show(Localizer.Get("Message:Body:Error:Save:JsonSyntaxTab", editor.Title), Localizer.Get("Message:Header:Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Получение контекста
            DataContext? context = CreateDataContext();
            if (context is null)
                return false;

            // Поиск мода
            Mod? mod = context.Mods.Include(x => x.Keys).ThenInclude(x => x.Values).ThenInclude(x => x.Language)
                                   .Include(x => x.Languages).Include(x => x.Language).AsSplitQuery()
                                   .FirstOrDefault(x => x.UID == editor.ModId);
            if (mod is null)
                goto Complete;

            // Поиск языка
            Language? language = context.Languages.FirstOrDefault(x => x.Name == editor.TranslationLanguage.Name);
            if (language is null)
            {
                language = new Language(editor.TranslationLanguage.Name);
                context.Languages.Add(language);
            }
            if (!mod.Languages.Contains(language))
            {
                mod.Languages.Add(language);
                CurrentDispatcher.Invoke(() =>
                {
                    Mod? modUI = Mods.FirstOrDefault(x => x.Equals(mod));
                    if (modUI is not null)
                    {
                        modUI.Languages.Add(language);
                        modUI.LanguagesView.Refresh();
                    }
                });
            }

            // Обновление значений
            foreach (Key key in mod.Keys)
            {
                // Поиск перевода
                if (!dictionary.TryGetValue(key.Text, out string? value))
                    continue;

                // Обновление/добавление
                Value @new = new Value(value, language, key);
                Value? old = key.Values.FirstOrDefault(x => EqualityComparer<Language>.Default.Equals(x.Language, language));
                if (old is null)
                {
                    context.Values.Add(@new);
                }
                else
                {
                    old.UpdateFromJsonModel(@new);
                }
            }

        // Завершение
        Complete:
            context.SaveChanges();
            FreeDataContext();
            editor.ConfigureHighlightning(null, null);
            editor.ApplyChanges();
            return true;
        }
        /// <summary>
        ///     Сохраняет вкладку.
        /// </summary>
        /// <param name="editor">
        ///     Редактор вкладки.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если сохранение было выполнено, иначе – <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="editor"/></c> равен <c>null</c>.
        /// </exception>
        private async Task<bool> SaveDictionaryTabAsync(DictionaryEditor editor)
        {
            return await Task.Run(() => SaveDictionaryTab(editor));
        }

        #endregion

        #endregion

        /// <summary>
        ///     Инициализирует новый экземпляр контекста.
        /// </summary>
        public MainWindowViewModel()
        {
            // Инициализация
            Mods = new List<Mod>();
            Tabs = new List<JsonEditor>();
            CurrentDispatcher = Dispatcher.CurrentDispatcher;
            ModsProgressBarMaximum = 1;
            ExportProgressBarMaximum = 1;
            ModsProgressBarVisibility = Visibility.Collapsed;
            ExportProgressBarVisibility = Visibility.Collapsed;
            LeftPanelWidth = new GridLength(200, GridUnitType.Pixel);

            // Блокировка контекста приложения
            if (!LockApplicationContext())
            {
                StartFailed = true;
                Application.Current.Shutdown(int.MinValue);
            }

            // Создание базы данных
            DataContext context = new DataContext(DataContextOptionsBuilder.Create());
            context.Database.EnsureCreated();
            context.SaveChanges();
            context.Dispose();
            IsFreeContext = true;
        }
    }
}