using BepInEx;

namespace SpectrumRepoMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.spectrum.repomod";
    public const string PluginName = "Spectrum R.E.P.O. Mod";
    public const string PluginVersion = "0.3.0";

    private void Awake()
    {
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Stage 1 loader proof.");
    }
}
