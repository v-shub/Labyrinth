using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

public class CellContainer
{
    [JsonProperty("Fills", ItemConverterType = typeof(CellFillConverter))]
    private readonly List<ICellFill> _fills = new();

    [JsonIgnore]
    public IReadOnlyList<ICellFill> Fills => _fills;

    public bool CanAdd(ICellFill fill)
    {
        return _fills.All(existing => FillPairRules.AreCompatible(existing.FillType, fill.FillType));
    }

    public void Add(ICellFill fill)
    {
        if (!CanAdd(fill))
            throw new InvalidOperationException(
                $"Нельзя добавить {fill.FillType.Name} в ячейку " +
                $"[{string.Join(", ", _fills.Select(f => f.FillType.Name))}]");
        _fills.Add(fill);
    }
    public bool Remove(ICellFill fill) => _fills.Remove(fill);
}
