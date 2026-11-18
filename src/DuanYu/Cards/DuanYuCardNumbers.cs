namespace DuanYu.Cards;

public sealed record DuanYuCardNumbers(int Damage = 0, int DamageUp = 0, int Block = 0, int BlockUp = 0, int ExtraDamage = 0, int ExtraDamageUp = 0)
{
    private static readonly IReadOnlyDictionary<string, DuanYuCardNumbers> Values = new Dictionary<string, DuanYuCardNumbers>
    {
        ["DY_B01"] = new(6, 9), ["DY_B02"] = new(Block: 5, BlockUp: 8),
        ["DY_B03"] = new(Block: 4, BlockUp: 7), ["DY_B04"] = new(Block: 5, BlockUp: 8), ["DY_B05"] = new(6, 8),
        ["DY_C01"] = new(6, 9), ["DY_C02"] = new(7, 10), ["DY_C03"] = new(5, 8), ["DY_C04"] = new(5, 8),
        ["DY_C05"] = new(6, 9), ["DY_C06"] = new(7, 10, 5, 7), ["DY_C07"] = new(Block: 3, BlockUp: 6),
        ["DY_C09"] = new(Block: 5, BlockUp: 8), ["DY_C11"] = new(Block: 5, BlockUp: 8), ["DY_C13"] = new(Block: 4, BlockUp: 7),
        ["DY_C15"] = new(Block: 7, BlockUp: 10), ["DY_C16"] = new(Block: 8, BlockUp: 11), ["DY_C17"] = new(3, 5),
        ["DY_C18"] = new(Block: 3, BlockUp: 6), ["DY_C20"] = new(9, 12),
        ["DY_U03"] = new(Block: 14, BlockUp: 18), ["DY_U07"] = new(12, 16), ["DY_U08"] = new(16, 22),
        ["DY_U11"] = new(Block: 3, BlockUp: 6), ["DY_U12"] = new(Block: 3, BlockUp: 3), ["DY_U16"] = new(10, 14),
        ["DY_U17"] = new(12, 16, ExtraDamage: 8, ExtraDamageUp: 10), ["DY_U18"] = new(Block: 9, BlockUp: 12),
        ["DY_U20"] = new(Block: 6, BlockUp: 9), ["DY_U24"] = new(Block: 15, BlockUp: 20),
        ["DY_U29"] = new(Block: 8, BlockUp: 11), ["DY_U30"] = new(Block: 5, BlockUp: 8),
        ["DY_U31"] = new(10, 14), ["DY_U32"] = new(7, 10), ["DY_U33"] = new(10, 13), ["DY_U34"] = new(Block: 6, BlockUp: 9),
        ["DY_R03"] = new(Block: 12, BlockUp: 12), ["DY_R05"] = new(24, 30, ExtraDamage: 18, ExtraDamageUp: 24),
        ["DY_R06"] = new(18, 24), ["DY_R11"] = new(Block: 20, BlockUp: 26), ["DY_R13"] = new(8, 11, ExtraDamage: 36, ExtraDamageUp: 45),
        ["DY_R16"] = new(Block: 8, BlockUp: 8), ["DY_R17"] = new(Block: 20, BlockUp: 26), ["DY_R20"] = new(Block: 12, BlockUp: 16),
        ["DY_T01"] = new(7, 10), ["DY_T02"] = new(4, 7), ["DY_T03"] = new(5, 8), ["DY_T04"] = new(4, 6, 4, 6),
        ["DY_T05"] = new(3, 6), ["DY_T06"] = new(3, 6)
    };
    private static readonly DuanYuCardNumbers Empty = new();
    public static DuanYuCardNumbers For(string id) => Values.GetValueOrDefault(id, Empty);
}
