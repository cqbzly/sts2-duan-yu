namespace DuanYu.Cards;

public sealed record DuanYuCardNumbers(int Damage = 0, int DamageUp = 0, int Block = 0, int BlockUp = 0, int ExtraDamage = 0, int ExtraDamageUp = 0)
{
    private static readonly IReadOnlyDictionary<string, DuanYuCardNumbers> Values = new Dictionary<string, DuanYuCardNumbers>
    {
        ["DY_B01"] = new(6, 9), ["DY_B02"] = new(Block: 5, BlockUp: 8),
        ["DY_B03"] = new(Block: 5, BlockUp: 7), ["DY_B04"] = new(Block: 4, BlockUp: 6), ["DY_B05"] = new(20, 28),
        ["DY_C01"] = new(11, 15), ["DY_C03"] = new(7, 10), ["DY_C04"] = new(10, 14),
        ["DY_C05"] = new(8, 11), ["DY_C06"] = new(11, 15, 5, 7), ["DY_C07"] = new(11, 15, 4, 6), ["DY_C08"] = new(8, 12),
        ["DY_C13"] = new(7, 10, 2, 4),
        ["DY_C15"] = new(Block: 12, BlockUp: 16), ["DY_C16"] = new(10, 14, 5, 7), ["DY_C17"] = new(6, 9),
        ["DY_C18"] = new(8, 12, 4, 6), ["DY_C20"] = new(11, 15), ["DY_C23"] = new(7, 10),
        ["DY_U03"] = new(Block: 15, BlockUp: 20), ["DY_U07"] = new(30, 42), ["DY_U08"] = new(16, 23), ["DY_U10"] = new(10, 14),
        ["DY_U11"] = new(8, 12, 2, 3), ["DY_U12"] = new(10, 14, 2, 2), ["DY_U16"] = new(13, 18),
        ["DY_U17"] = new(23, 31), ["DY_U18"] = new(Block: 7, BlockUp: 10),
        ["DY_U20"] = new(11, 15, 4, 6), ["DY_U21"] = new(10, 14), ["DY_U22"] = new(13, 18), ["DY_U24"] = new(Block: 12, BlockUp: 15),
        ["DY_U29"] = new(Block: 6, BlockUp: 9), ["DY_U30"] = new(Block: 4, BlockUp: 6),
        ["DY_U31"] = new(13, 19), ["DY_U32"] = new(12, 17), ["DY_U33"] = new(16, 22), ["DY_U34"] = new(Block: 5, BlockUp: 7), ["DY_U35"] = new(Block: 2, BlockUp: 3), ["DY_U36"] = new(12, 17), ["DY_U37"] = new(Block: 4, BlockUp: 6),
        ["DY_R03"] = new(Block: 8, BlockUp: 11), ["DY_R05"] = new(54, 72, ExtraDamage: 22, ExtraDamageUp: 29),
        ["DY_R06"] = new(22, 30), ["DY_R09"] = new(15, 20), ["DY_R10"] = new(15, 20), ["DY_R11"] = new(Block: 16, BlockUp: 21), ["DY_R12"] = new(Block: 6, BlockUp: 9), ["DY_R13"] = new(58, 79),
        ["DY_R16"] = new(Block: 7, BlockUp: 7), ["DY_R17"] = new(Block: 16, BlockUp: 21), ["DY_R19"] = new(Block: 6, BlockUp: 8), ["DY_R20"] = new(20, 27, 16, 22), ["DY_R21"] = new(7, 10),
        ["DY_T01"] = new(7, 10), ["DY_T02"] = new(3, 5), ["DY_T03"] = new(5, 7), ["DY_T04"] = new(3, 5, 3, 5),
        ["DY_T05"] = new(2, 3), ["DY_T06"] = new(2, 4)
    };
    private static readonly DuanYuCardNumbers Empty = new();
    public static DuanYuCardNumbers For(string id) => Values.GetValueOrDefault(id, Empty);
}
