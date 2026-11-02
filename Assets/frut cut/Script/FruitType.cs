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
        new FruitType{ Id="watermelon", Radius=64, Rind=H("166534"), InnerRind=H("dcfce7"), Flesh=H("e11d48"), Juice=H("fb7185"), Points=2 },
        new FruitType{ Id="mango",      Radius=56, Rind=H("d97706"), InnerRind=H("fef3c7"), Flesh=H("f59e0b"), Juice=H("fcd34d"), Points=2 },
        new FruitType{ Id="orange",     Radius=53, Rind=H("c2410c"), InnerRind=H("ffedd5"), Flesh=H("f97316"), Juice=H("fdba74"), Points=1 },
        new FruitType{ Id="apple",      Radius=52, Rind=H("b91c1c"), InnerRind=H("fee2e2"), Flesh=H("fde047"), Juice=H("f87171"), Points=1 },
        new FruitType{ Id="banana",     Radius=56, Rind=H("eab308"), InnerRind=H("fef9c3"), Flesh=H("fef08a"), Juice=H("fde047"), Points=1 },
        new FruitType{ Id="coconut",    Radius=54, Rind=H("451a03"), InnerRind=H("a16207"), Flesh=H("f8fafc"), Juice=H("e2e8f0"), Points=2 },
        new FruitType{ Id="heartfruit", Radius=55, Rind=H("be185d"), InnerRind=H("fbcfe8"), Flesh=H("ec4899"), Juice=H("f9a8d4"), Points=5, IsHeart=true },
        new FruitType{ Id="bomb",       Radius=48, Rind=H("0f172a"), InnerRind=H("ef4444"), Flesh=H("1e293b"), Juice=H("f97316"), Points=0, IsBomb=true },
    };

    public static FruitType Get(string id) => System.Array.Find(All, f => f.Id == id);
}