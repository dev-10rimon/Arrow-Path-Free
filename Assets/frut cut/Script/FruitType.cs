using UnityEngine;

public class FruitType
{
    public string Id, Name;
    public float Radius;
    public Color Rind, InnerRind, Flesh, Juice;
    public int Points;
    public bool IsHeart, IsBomb;

    static Color H(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

    public static readonly FruitType[] All =
    {
        new FruitType{ Id="watermelon", Radius=64, Rind=H("15803d"), InnerRind=H("bbf7d0"), Flesh=H("f43f5e"), Juice=H("fb7185"), Points=2 },
        new FruitType{ Id="mango",      Radius=56, Rind=H("f59e0b"), InnerRind=H("fef3c7"), Flesh=H("fbbf24"), Juice=H("fcd34d"), Points=2 },
        new FruitType{ Id="orange",     Radius=53, Rind=H("ea580c"), InnerRind=H("ffedd5"), Flesh=H("fb923c"), Juice=H("fdba74"), Points=1 },
        new FruitType{ Id="apple",      Radius=52, Rind=H("dc2626"), InnerRind=H("fef2f2"), Flesh=H("fde68a"), Juice=H("f87171"), Points=1 },
        new FruitType{ Id="banana",     Radius=56, Rind=H("facc15"), InnerRind=H("fef9c3"), Flesh=H("fef08a"), Juice=H("fde047"), Points=1 },
        new FruitType{ Id="coconut",    Radius=54, Rind=H("57361a"), InnerRind=H("a16207"), Flesh=H("f8fafc"), Juice=H("e2e8f0"), Points=2 },
        new FruitType{ Id="heartfruit", Radius=55, Rind=H("ec4899"), InnerRind=H("fce7f3"), Flesh=H("f472b6"), Juice=H("f9a8d4"), Points=5, IsHeart=true },
        new FruitType{ Id="bomb",       Radius=48, Rind=H("1e293b"), InnerRind=H("ef4444"), Flesh=H("0f172a"), Juice=H("f97316"), Points=0, IsBomb=true },
    };

    public static FruitType Get(string id) => System.Array.Find(All, f => f.Id == id);
}