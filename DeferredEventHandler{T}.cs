using System;
using System.Reflection;
using System.Windows.Threading;
using ExceptionHelper = Cybrex.Core.Exceptions.ExceptionHelper;

namespace PrimitiveTranslator
{
    /// <summary>
    ///     Отложенный обработчик события.
    /// </summary>
    /// <typeparam name="T">
    ///     Тип обработчика.
    /// </typeparam>
    public sealed class DeferredEventHandler<T> where T : Delegate
    {
        /// <summary>
        ///     Обработчик.
        /// </summary>
        private readonly T Handler;
        /// <summary>
        ///     Таймер выполнения.
        /// </summary>
        private readonly DispatcherTimer Timer;

        /// <summary>
        ///     Флаг запущенного таймера.
        /// </summary>
        private bool Started;
        /// <summary>
        ///     Аргументы вызова.
        /// </summary>
        private object?[]? Arguments;

        /// <summary>
        ///     Инициализирует новый экземпляр обработчика.
        /// </summary>
        /// <param name="handler">
        ///     Обработчик.
        /// </param>
        /// <param name="delay">
        ///     Задержка выполнения в миллисекундах.
        /// </param>
        public DeferredEventHandler(T handler, int delay)
        {
            // Проверка аргументов
            ExceptionHelper.ThrowIfArgumentIsNull(handler, nameof(handler));
            ExceptionHelper.ThrowIfArgumentOutOfRange(delay, 0, int.MaxValue, nameof(delay));

            // Инициализация
            Handler = handler;
            Timer = new DispatcherTimer();
            Timer.Tick += Execute;
            Timer.Interval = TimeSpan.FromMilliseconds(delay);
        }

        /// <summary>
        ///     Инициирует отложенное событие.
        /// </summary>
        /// <param name="args">
        ///     Аргументы обработчика.
        /// </param>
        public void Invoke(params object?[] args)
        {
            Timer.Stop();

            Started = true;
            Arguments = args;
            Timer.Start();
        }
        /// <summary>
        ///     Производит выполнение обработчика.
        /// </summary>
        /// <param name="sender">
        ///     Объект, вызвавший событие.
        /// </param>
        /// <param name="e">
        ///     Аргументы события.
        /// </param>
        /// <exception cref="TargetParameterCountException">
        ///     Количество аргументов соответствует обработчику.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Аргументы не соответствуют сигнатуре обработчика.
        /// </exception>
        public void Execute(object? sender, EventArgs? e)
        {
            Timer.Stop();
            if (!Started)
                return;
            
            Handler.DynamicInvoke(Arguments);
            Arguments = null;
            Started = false;
        }
        /// <summary>
        ///     Прерывает ожидание.
        /// </summary>
        public void Abort()
        {
            Timer.Stop();
            Arguments = null;
            Started = false;
        }
    }
}