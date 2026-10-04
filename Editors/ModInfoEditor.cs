namespace PrimitiveTranslator.Editors
{
    /// <summary>
    ///     Редактор информации мода.
    /// </summary>
    public sealed class ModInfoEditor : JsonEditor
    {
        /// <inheritdoc/>
        public override EditorType Type
        {
            get => EditorType.ModInfo;
        }

        /// <summary>
        ///     Инициализирует новый экземпляр редактора.
        /// </summary>
        /// <param name="json">
        ///     Json-файл.
        /// </param>
        public ModInfoEditor(string? json) : base(json)
        {
            Title = "modinfo.json";
        }

        /// <inheritdoc/>
        public override EditorSignature GetSignature()
        {
            EditorSignature signature = new EditorSignature();
            signature.Type = Type;
            return signature;
        }
    }
}