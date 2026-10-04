using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Newtonsoft.Json;
using PrimitiveTranslator.Views;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Threading;
using System.Xml;

namespace PrimitiveTranslator.Editors
{
    /// <summary>
    ///     Редактор.
    /// </summary>
    public abstract class JsonEditor : BindableBase, IDisposable
    {
        /// <summary>
        ///     Стиль редактора текста.
        /// </summary>
        private static Style? TextEditorStyle;

        /// <summary>
        ///     Хеш-сумма сохранённых данных.
        /// </summary>
        private ReadOnlyMemory<byte> SavedHash;
        /// <summary>
        ///     Текстовый редактор.
        /// </summary>
        protected readonly TextEditor TextEditor;
        /// <summary>
        ///     Диспатчер потока.
        /// </summary>
        protected readonly Dispatcher Dispatcher;
        /// <summary>
        ///     Правила подсветки json-синтаксиса.
        /// </summary>
        protected readonly ReadOnlyMemory<HighlightingRule> JsonRuleSet;

        /// <summary>
        ///     Заголовок.
        /// </summary>
        public string Title
        {
            get => field ??= string.Empty;
            protected set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Текст.
        /// </summary>
        public string Text
        {
            get => Dispatcher.Invoke(() => TextEditor.Text) ?? string.Empty;
            set => Dispatcher.Invoke(() => TextEditor.Text = value ?? string.Empty);
        }
        /// <summary>
        ///     Тип редактора.
        /// </summary>
        public abstract EditorType Type { get; }

        /// <summary>
        ///     Флаг несохранённых изменений.
        /// </summary>
        public bool IsModified
        {
            get => field;
            private set => SetProperty(ref field, value);
        }
        /// <summary>
        ///     Снимает флаг несохранённых изменений.
        /// </summary>
        public void ApplyChanges()
        {
            SavedHash = ComputeHash(Text);
            IsModified = false;
        }

        /// <summary>
        ///     Инициализирует новый экземпляр редактора.
        /// </summary>
        /// <param name="text">
        ///     Начальный текст.
        /// </param>
        public JsonEditor(string? text)
        {
            SavedHash = ComputeHash(text);
            TextEditor = new TextEditor();
            Dispatcher = Dispatcher.CurrentDispatcher;
            using (Stream stream = typeof(JsonEditor).Assembly.GetManifestResourceStream("PrimitiveTranslator.Resources.Syntax.Json.xshd.xml")!)
                using (XmlTextReader reader = new XmlTextReader(stream))
                    TextEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            JsonRuleSet = TextEditor.SyntaxHighlighting.MainRuleSet.Rules.ToArray();
            ConfigureEditorStyle(TextEditor);

            Text = text ?? string.Empty;
            TextChangedHandler = new DeferredEventHandler<EventHandler>(OnTextChangedDeferred, TextChangedDelay);
            TextEditor.TextChanged += OnTextChanged;
        }

        /// <summary>
        ///     Получает сигнатуру редактора.
        /// </summary>
        /// <returns>
        ///     Сигнатура.
        /// </returns>
        public abstract EditorSignature GetSignature();
        /// <summary>
        ///     Выполняет все ожидающие обработчики немедленно.
        /// </summary>
        public virtual void ExecuteDeferredHandlers()
        {
            TextChangedHandler.Execute(this, null);
        }
        /// <summary>
        ///     Пытается десериализовать текст.
        /// </summary>
        /// <typeparam name="T">
        ///     Тип объекта.
        /// </typeparam>
        /// <param name="obj">
        ///     Десериализованный объект или <c>default(<typeparamref name="T"/>)</c>.
        /// </param>
        /// <param name="converters">
        ///     Набор конвертеров.
        /// </param>
        /// <returns>
        ///     <c>true</c>, если json корректен, иначе – <c>false</c>.
        /// </returns>
        public bool TryDeserializeJson<T>([NotNullWhen(true)] out T? obj, params JsonConverter[] converters)
        {
            obj = default(T);
            try { obj = JsonConvert.DeserializeObject<T>(Text, converters); }
            catch { return false; }
            return !EqualityComparer<T>.Default.Equals(obj, default(T));
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
            ReadOnlyMemory<byte> hash = ComputeHash(Text);
            IsModified = !hash.Span.SequenceEqual(SavedHash.Span);
        }

        /// <summary>
        ///     Получает текстовый редактор.
        /// </summary>
        /// <returns>
        ///     Текстовый редактор.
        /// </returns>
        internal TextEditor GetTextEditor()
        {
            return TextEditor;
        }
        /// <summary>
        ///     Освобождает ресурсы.
        /// </summary>
        public virtual void Dispose()
        {
            TextEditor.TextChanged -= OnTextChanged;
            TextChangedHandler.Abort();
        }

        /// <summary>
        ///     Вычисляет хеш-сумму текста.
        /// </summary>
        /// <param name="text">
        ///     Текст.
        /// </param>
        /// <returns>
        ///     Хеш-сумма.
        /// </returns>
        private static ReadOnlyMemory<byte> ComputeHash(string? text)
        {
            ReadOnlySpan<char> chars = text;
            ReadOnlySpan<byte> data = MemoryMarshal.Cast<char, byte>(chars);
            return SHA256.HashData(data);
        }
        /// <summary>
        ///     Конфигурирует стиль текстового редактора.
        /// </summary>
        /// <param name="editor">
        ///     Текстовый редактор.
        /// </param>
        private static void ConfigureEditorStyle(TextEditor editor)
        {
            // Конфргурация параметров
            editor.TextArea.Caret.CaretBrush = UIHelper.CarretColorDefault;
            TextEditorStyle ??= Application.Current.TryFindResource("TextEditorStyle") as Style;
            if (TextEditorStyle is not null)
                editor.Style = TextEditorStyle;
        }
    }
}