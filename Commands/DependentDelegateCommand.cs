using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;
using ExceptionHelper = Cybrex.Core.Exceptions.ExceptionHelper;

namespace PrimitiveTranslator.Commands
{
    /// <summary>
    ///     Команда, зависимая от свойств.
    /// </summary>
    public sealed class DependentDelegateCommand : DelegateCommand
    {
        /// <summary>
        ///     Свойства зависимостей.
        /// </summary>
        private readonly SortedSet<string> Properties;
        /// <summary>
        ///     Контекст свойств.
        /// </summary>
        private readonly INotifyPropertyChanged Context;
        /// <summary>
        ///     Диспатчер контекста.
        /// </summary>
        private readonly Dispatcher ContextDispatcher;

        /// <summary>
        ///     Инициализирует новый экземпляр команды.
        /// </summary>
        /// <param name="executeMethod">
        ///     Метод выполнения команды.
        /// </param>
        /// <param name="canExecuteMethod">
        ///     Метод проверки возможности выполнения.
        /// </param>
        /// <param name="context">
        ///     Контекст свойств.
        /// </param>
        /// <param name="properties">
        ///     Свойства зависимостей.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="executeMethod"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="canExecuteMethod"/></c> равен <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="context"/></c> равен <c>null</c>.
        /// </exception>
        public DependentDelegateCommand(Action executeMethod, Func<bool> canExecuteMethod, INotifyPropertyChanged context, params string[] properties) : base(executeMethod, canExecuteMethod)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(executeMethod, nameof(executeMethod));
            ExceptionHelper.ThrowIfArgumentIsNull(canExecuteMethod, nameof(canExecuteMethod));
            ExceptionHelper.ThrowIfArgumentIsNull(context, nameof(context));

            // Инициализация
            Properties = new SortedSet<string>(properties, Comparer<string>.Default);
            Context = context;
            ContextDispatcher = Dispatcher.CurrentDispatcher;
            if (Properties.Count > 0)
                Context.PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        ///     Подписывает команду на свойство.
        /// </summary>
        /// <param name="property">
        ///     Имя свойства.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     Аргумент <c><paramref name="property"/></c> равен <c>null</c>.
        /// </exception>
        public void FollowProperty(string property)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(property, nameof(property));

            // Подписка
            Action<string> action = (string property) =>
            {
                Properties.Add(property);
                if (Properties.Count == 1)
                    Context.PropertyChanged += OnPropertyChanged;
            };
            if (object.ReferenceEquals(ContextDispatcher, Dispatcher.CurrentDispatcher))
                action(property);
            else
                ContextDispatcher.Invoke(() => action(property));
        }
        /// <summary>
        ///     Отписывает команду от свойства.
        /// </summary>
        /// <param name="property">
        ///     Имя свойства.
        /// </param>
        public void UnfollowProperty(string property)
        {
            // Проверка аргументов
            if (property is null)
                return;

            // Подписка
            Action<string> action = (string property) =>
            {
                Properties.Remove(property);
                if (Properties.Count == 0)
                    Context.PropertyChanged -= OnPropertyChanged;
            };
            if (object.ReferenceEquals(ContextDispatcher, Dispatcher.CurrentDispatcher))
                action(property);
            else
                ContextDispatcher.Invoke(() => action(property));
        }

        /// <summary>
        ///     Обработчик события изменения свойства.
        /// </summary>
        /// <param name="sender">
        ///     Объект, вызвавший событие.
        /// </param>
        /// <param name="e">
        ///     Аргументы события.
        /// </param>
        private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Проверка аргументов
            if (!object.ReferenceEquals(sender, Context))
                return;
            if (e is null || e.PropertyName is null)
                return;

            // Обновление
            if (Properties.Contains(e.PropertyName))
            {
                if (object.ReferenceEquals(ContextDispatcher, Dispatcher.CurrentDispatcher))
                    RaiseCanExecuteChanged();
                else
                    await ContextDispatcher.InvokeAsync(RaiseCanExecuteChanged);
            }
        }
    }
}