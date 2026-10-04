using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrimitiveTranslator.Data.Models;
using System;

namespace PrimitiveTranslator.Converters.Json
{
    /// <summary>
    ///     Json-конвертер <c><see cref="Mod"/></c>.
    /// </summary>
    public sealed class ModConverter : JsonConverter<Mod>
    {
        /// <summary>
        ///     Общий экземпляр.
        /// </summary>
        public static ModConverter Instance
        {
            get => field ??= new ModConverter();
        }

        /// <summary>
        ///     Считывает объект.
        /// </summary>
        /// <param name="reader">
        ///     Инструмент чтения.
        /// </param>
        /// <param name="objectType">
        ///     Тип объекта.
        /// </param>
        /// <param name="existingValue">
        ///     Существующее значение.
        /// </param>
        /// <param name="hasExistingValue">
        ///     Флаг наличия существующего значения.
        /// </param>
        /// <param name="serializer">
        ///     Сериализатор.
        /// </param>
        /// <returns>
        ///     Прочитанный объект.
        /// </returns>
        public override Mod? ReadJson(JsonReader reader, Type objectType, Mod? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);

            string? uid = obj.GetValue("modid", StringComparison.OrdinalIgnoreCase)?.ToString();
            if (uid is null)
                return null;

            string? name = obj.GetValue("name", StringComparison.OrdinalIgnoreCase)?.ToString();
            string? version = obj.GetValue("version", StringComparison.OrdinalIgnoreCase)?.ToString();
            string? description = obj.GetValue("description", StringComparison.OrdinalIgnoreCase)?.ToString();
            return new Mod(uid, name, version, description);
        }
        /// <summary>
        ///     Записывает объект.
        /// </summary>
        /// <param name="writer">
        ///     Инструмент записи.
        /// </param>
        /// <param name="value">
        ///     Записываемый объект.
        /// </param>
        /// <param name="serializer">
        ///     Сериализатор.
        /// </param>
        public override void WriteJson(JsonWriter writer, Mod? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("modid");
            writer.WriteValue(value.UID);
            if (value.Name is not null)
            {
                writer.WritePropertyName("name");
                writer.WriteValue(value.Name);
            }
            if (value.Version is not null)
            {
                writer.WritePropertyName("version");
                writer.WriteValue(value.Version);
            }
            if (value.Description is not null)
            {
                writer.WritePropertyName("description");
                writer.WriteValue(value.Description);
            }
            writer.WriteEndObject();
        }
    }
}