using System;
using System.Collections.Generic;

public static class FillPairRules
{
    private static readonly HashSet<(Type, Type)> _forbidden = new()
    {
        MakePair(typeof(RubberRoom), typeof(River)),
        MakePair(typeof(RubberRoom), typeof(Dragon)),
        MakePair(typeof(RubberRoom), typeof(Portal)),

        MakePair(typeof(Dragon), typeof(Portal)),
        MakePair(typeof(Dragon), typeof(Sword)),
        MakePair(typeof(Dragon), typeof(Exit)),

        MakePair(typeof(Key), typeof(Sword)),
        MakePair(typeof(Key), typeof(Exit)),
    };

    private static (Type, Type) MakePair(Type a, Type b)
        => a.GetHashCode() < b.GetHashCode() ? (a, b) : (b, a);

    public static bool AreCompatible(Type a, Type b)
    {
        if (a == b) return false;
        return !_forbidden.Contains(MakePair(a, b));
    }
}