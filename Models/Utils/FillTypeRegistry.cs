using System;
using System.Collections.Generic;

/// <summary>
/// Белый список наполнений, которые разрешено загружать из JSON.
/// Ключ — имя, которое пишется в поле "Type" в JSON.
/// Значение — конкретный C#-тип.
/// </summary>
public static class FillTypeRegistry
{
    private static readonly Dictionary<string, Type> _allowed = new()
    {
        { nameof(Exit),       typeof(Exit) },
        { nameof(Swamp),      typeof(Swamp) },
        { nameof(River),      typeof(River) },
        { nameof(Dragon),     typeof(Dragon) },
        { nameof(Portal),     typeof(Portal) },
        { nameof(Sword),      typeof(Sword) },
        { nameof(Key),        typeof(Key) },
        { nameof(RubberRoom), typeof(RubberRoom) },
        // добавляйте новые наполнения сюда — это единственное место
    };

    /// <summary>Превращает имя из JSON в C#-тип. Бросает, если тип не разрешён.</summary>
    public static Type Resolve(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            throw new ArgumentException("Имя типа наполнения пустое");

        if (_allowed.TryGetValue(typeName, out var type))
            return type;

        throw new InvalidOperationException(
            $"Тип наполнения '{typeName}' не разрешён в FillTypeRegistry. " +
            $"Разрешены: {string.Join(", ", _allowed.Keys)}");
    }

    /// <summary>Превращает C#-тип в имя для JSON. Бросает, если тип не в белом списке.</summary>
    public static string GetName(Type type)
    {
        foreach (var kvp in _allowed)
            if (kvp.Value == type)
                return kvp.Key;

        throw new InvalidOperationException(
            $"Тип '{type.FullName}' отсутствует в FillTypeRegistry — его нельзя сохранить.");
    }
}