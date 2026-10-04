using Newtonsoft.Json;
using PrimitiveTranslator.Data.Models;
using PrimitiveTranslator.Editors.Highlightning;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using ExceptionHelper = Cybrex.Core.Exceptions.ExceptionHelper;

namespace PrimitiveTranslator.Editors
{
    /// <summary>
    ///     Редактор словаря.
    /// </summary>
    public sealed class DictionaryEditor : JsonEditor
    {
        /// <summary>
        ///     Разделитель оригинального и неактуального значений.
        /// </summary>
        public const string ModifiedValuesSeparator = " | ";

        /// <summary>
        ///     Окрашиватель ключей.
        /// </summary>
        private readonly KeyHighlightTransformer KeyHighlighter;
        /// <summary>
        ///     Паттерны неактуальных ключей.
        /// </summary>
        private readonly List<string> ModifiedKeyPatterns;
        /// <summary>
        ///     Паттерны новых ключей.
        /// </summary>
        private readonly List<string> NewKeyPatterns;

        /// <summary>
        ///     Уникальный идентификатор мода.
        /// </summary>
        public string ModId { get; }
        /// <summary>
        ///     Язык, с которого происходит перевод.
        /// </summary>
        public Language OriginalLanuage { get; }
        /// <summary>
        ///     Язык, на который происходит перевод.
        /// </summary>
        public Language TranslationLanguage { get; }
        /// <inheritdoc/>
        public override EditorType Type
        {
            get => EditorType.Dictionary;
        }
        /// <summary>
        ///     Ключи, которые требуется перевести.
        /// </summary>
        public IReadOnlyList<string> KeysToTranslate
        {
            get => field;
            private set => SetProperty(ref field, value);
        }

        /// <summary>
        ///     Инициализирует новый экземпляр редактора.
        /// </summary>
        /// <param name="mod">
        ///     Уникальный идентификатор мода.
        /// </param>
        /// <param name="from">
        ///     Язык, с которого происходит перевод.
        /// </param>
        /// <param name="to">
        ///     Язык, на который происходит перевод.
        /// </param>
        /// <param name="dictionary">
        ///     Словарь. <br/>
        ///     Должен включать записи из <c><paramref name="modified"/></c> и <c><paramref name="new"/></c>.
        /// </param>
        /// <param name="modified">
        ///     Словарь модифицированный значений (запись на языке <c><paramref name="from"/></c> новее, чем для 
        ///     <c><paramref name="to"/></c>).
        /// </param>
        /// <param name="new">
        ///     Словарь новых значений (запись не существует на языке <c><paramref name="to"/></c>).
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="mod"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="from"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="to"/></c> равен <c>null</c>.
        /// </exception>
        public DictionaryEditor(string mod, Language from, Language to, IReadOnlyDictionary<string, string> dictionary, IReadOnlyList<string>? modified = null, IReadOnlyList<string>? @new = null) : base(ToJson(dictionary))
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(mod, nameof(mod));
            ExceptionHelper.ThrowIfArgumentIsNull(from, nameof(from));
            ExceptionHelper.ThrowIfArgumentIsNull(to, nameof(to));

            // Инициализация
            ModId = mod;
            OriginalLanuage = from;
            TranslationLanguage = to;
            NewKeyPatterns = new List<string>();
            ModifiedKeyPatterns = new List<string>();
            KeysToTranslate = Array.Empty<string>();
            KeyHighlighter = new KeyHighlightTransformer();
            Title = $"{OriginalLanuage}>{TranslationLanguage}:{ModId}";
            TextEditor.TextArea.TextView.LineTransformers.Add(KeyHighlighter);
            TextChangedHandler = new DeferredEventHandler<EventHandler>(OnTextChangedDeferred, TextChangedDelay);
            TextEditor.TextChanged += OnTextChanged;
            ConfigureHighlightning(modified, @new);
        }

        /// <summary>
        ///     Обновляет диапазоны подсветки.
        /// </summary>
        private void UpdateRanges()
        {
            string text = Text;
            KeyHighlighter.ClearRanges();
            foreach (string pattern in ModifiedKeyPatterns)
            {
                Match match = Regex.Match(text, pattern);
                if (match.Success)
                    KeyHighlighter.RegisterRange(new HighlightRange(match.Index, match.Length, KeyHighlightTransformer.ModifiedKeyColor));
            }
            foreach (string pattern in NewKeyPatterns)
            {
                Match match = Regex.Match(text, pattern);
                if (match.Success)
                    KeyHighlighter.RegisterRange(new HighlightRange(match.Index, match.Length, KeyHighlightTransformer.NewKeyColor));
            }
        }
        /// <summary>
        ///     Конфигурирует подсветку модифицированных и новых ключей.
        /// </summary>
        /// <param name="modified">
        ///     Словарь модифицированный значений (запись на языке <c><see cref="OriginalLanuage"/></c> новее, чем для 
        ///     <c><see cref="TranslationLanguage"/></c>).
        /// </param>
        /// <param name="new">
        ///     Словарь новых значений (запись не существует на языке <c><see cref="TranslationLanguage"/></c>).
        /// </param>
        public void ConfigureHighlightning(IReadOnlyList<string>? modified, IReadOnlyList<string>? @new)
        {
            Action action = () =>
            {
                // Ключи к переводу
                List<string> keys = new List<string>((modified?.Count ?? 0) + (@new?.Count ?? 0));

                // Неактуальные ключи
                ModifiedKeyPatterns.Clear();
                foreach (string key in modified ?? [])
                {
                    ModifiedKeyPatterns.Add($"\"{Regex.Escape(key)}\"\\s*:");
                    keys.Add(key);
                }

                // Новые ключи
                NewKeyPatterns.Clear();
                foreach (string key in @new ?? [])
                {
                    NewKeyPatterns.Add($"\"{Regex.Escape(key)}\"\\s*:");
                    keys.Add(key);
                }

                // Обновление
                KeysToTranslate = keys;
                UpdateRanges();
            };
            if (object.ReferenceEquals(Dispatcher, Dispatcher.CurrentDispatcher))
                action();
            else
                Dispatcher.Invoke(action);
        }

        /// <inheritdoc/>
        public override EditorSignature GetSignature()
        {
            EditorSignature signature = new EditorSignature();
            signature.Type = Type;
            signature.ModId = ModId;
            signature.OriginalLanuage = OriginalLanuage.Name;
            signature.TranslationLanguage = TranslationLanguage.Name;
            return signature;
        }

        /// <summary>
        ///     Задержка обработчика события изменения текста.
        /// </summary>
        private const int TextChangedDelay = 1000;
        /// <summary>
        ///     Отложенный обработчик события изменения текста.
        /// </summary>
        private readonly DeferredEventHandler<EventHandler> TextChangedHandler;
        /// <summary>
        ///     Обработчик события изменения текста.
        /// </summary>
        /// <param name="sender">
        ///     Объект, вызвавший событие.
        /// </param>
        /// <param name="e">
        ///     Аргументы события.
        /// </param>
        private void OnTextChanged(object? sender, EventArgs e)
        {
            TextChangedHandler.Invoke(sender, e);
        }
        /// <summary>
        ///     Отложенный обработчик события изменения текста.
        /// </summary>
        /// <param name="sender">
        ///     Объект, вызвавший событие.
        /// </param>
        /// <param name="e">
        ///     Аргументы события.
        /// </param>
        private void OnTextChangedDeferred(object? sender, EventArgs e)
        {
            UpdateRanges();
            TextEditor.TextArea.TextView.Redraw();
        }

        /// <summary>
        ///     Освобождает ресурсы.
        /// </summary>
        public override void Dispose()
        {
            TextEditor.TextChanged -= OnTextChanged;
            TextChangedHandler.Abort();
            base.Dispose();
        }

        /// <summary>
        ///     Конвертирует словарь в json-текст.
        /// </summary>
        /// <param name="dictionary">
        ///     Словарь.
        /// </param>
        /// <returns>
        ///     Json-текст.
        /// </returns>
        private static string ToJson(IReadOnlyDictionary<string, string>? dictionary)
        {
            dictionary ??= new Dictionary<string, string>();
            return JsonConvert.SerializeObject(dictionary, Formatting.Indented);
        }
    }
}