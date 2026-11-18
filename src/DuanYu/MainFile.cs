using System.Reflection;
using DuanYu.Assets;
using DuanYu.Cards;
using DuanYu.Runtime;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace DuanYu;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "CQBZLY_DUAN_YU";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        DuanYuAssets.Initialize();
        DuanYuState.Initialize();
        DuanYuCardExecutor.Initialize();
        new Harmony(ModId).PatchAll(Assembly.GetExecutingAssembly());
        Logger.Info("Duan Yu initialized.");
#if DEBUG
        DebugTests.EngineSelfTests.StartIfRequested();
#endif
    }
}
