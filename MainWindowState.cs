using System.Windows;

namespace PrimitiveTranslator
{
    /// <summary>
    ///     Состояние главного окна.
    /// </summary>
    public sealed class MainWindowState
    {
        /// <summary>
        ///     Основное состояние.
        /// </summary>
        public WindowState State { get; set; }
        /// <summary>
        ///     Ширина.
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        ///     Высота.
        /// </summary>
        public double Height { get; set; }
        /// <summary>
        ///     Смещение слева.
        /// </summary>
        public double LeftOffset { get; set; }
        /// <summary>
        ///     Смещение сверху.
        /// </summary>
        public double TopOffset { get; set; }
    }
}