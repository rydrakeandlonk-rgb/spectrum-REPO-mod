using BepInEx;
using BepInEx.Logging;

namespace SpectrumRepoMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("Zehs.REPOLib", BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.spectrum.repomod";
    public const string PluginName = "Spectrum R.E.P.O. Mod";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        Log.LogInfo("BepInEx and REPOLib dependencies resolved. Game integration is being initialized.");
    }
}
