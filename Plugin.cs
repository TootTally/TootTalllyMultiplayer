using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings;

namespace TootTallyMultiplayer
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallyAccounts", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallyWebsocketLibs", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallyGameModifiers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInIncompatibility("Tooter")]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "Multiplayer.cfg";
        private Harmony _harmony;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }

        //Change this name to whatever you want
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }

        public static TootTallySettingPage settingPage;

        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            _harmony = new Harmony(Info.Metadata.GUID);

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        public void Update() { } //Should probably rework that

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Modules", "Multiplayer", true, "Enable TootTally's Multiplayer module");
            TootTallyModuleManager.AddModule(this);
            TootTallySettings.Plugin.Instance.AddModuleToSettingPage(this);
        }

        public void LoadModule()
        {
            string bundlePath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "multiplayerassetbundle");
            AssetBundleManager.LoadAssets(bundlePath);

            string assetsPath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "Assets");
            AssetManager.LoadAssets(assetsPath);

            _harmony.PatchAll(typeof(MultiplayerManager));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            _harmony.UnpatchSelf();
            LogInfo($"Module unloaded!");
        }

    }
}