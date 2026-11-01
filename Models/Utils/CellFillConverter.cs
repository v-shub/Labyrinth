using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CellFillConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
        => typeof(ICellFill).IsAssignableFrom(objectType);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null) { writer.WriteNull(); return; }

        var fill = (ICellFill)value;
        Type concrete = fill.GetType();

        // Если типа нет в белом списке — GetName бросит исключение.
        // Это защита от сохранения "левых" типов.
        string typeName = FillTypeRegistry.GetName(concrete);

        writer.WriteStartObject();

        writer.WritePropertyName("Type");
        writer.WriteValue(typeName);

        writer.WritePropertyName("Data");
        // Сериализуем по конкретному типу, чтобы попали все поля River/Swamp/etc.
        serializer.Serialize(writer, fill, concrete);

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType,
        object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        var jo = JObject.Load(reader);

        string typeName = jo["Type"]?.ToString();
        if (string.IsNullOrEmpty(typeName))
            throw new JsonSerializationException("В наполнении отсутствует поле 'Type'");

        // Белый список: если тип не разрешён — Resolve бросит исключение
        Type concrete = FillTypeRegistry.Resolve(typeName);

        if (!typeof(ICellFill).IsAssignableFrom(concrete))
            throw new JsonSerializationException(
                $"Тип '{concrete.FullName}' не реализует ICellFill");

        var data = jo["Data"] as JObject ?? new JObject();

        // Newtonsoft сам создаст объект:
        // - через [JsonConstructor], если он есть;
        // - через безпараметрический конструктор + сеттеры/поля;
        // - через единственный публичный конструктор (зависит от версии).
        object result = data.ToObject(concrete, serializer);
        if (result == null)
            throw new JsonSerializationException(
                $"Не удалось создать экземпляр '{concrete.FullName}'");

        return result;
    }
}