using UnityExplorer.UI;
using System.Globalization;

namespace UnityExplorer.Config
{
    public static class ConfigManager
    {
        internal static readonly Dictionary<string, IConfigElement> ConfigElements = new();
        internal static readonly Dictionary<string, IConfigElement> InternalConfigs = new();

        // Each Mod Loader has its own ConfigHandler.
        // See the UnityExplorer.Loader namespace for the implementations.
        public static ConfigHandler Handler { get; private set; }

        // Actual UE Settings
        public static ConfigElement<Language> LanguageSetting;
        public static ConfigElement<KeyCode> Master_Toggle;
        public static ConfigElement<bool> Hide_On_Startup;
        public static ConfigElement<float> Startup_Delay_Time;
        public static ConfigElement<bool> Disable_EventSystem_Override;
        public static ConfigElement<bool> Disable_Setup_Force_ReLoad_ManagedAssemblies;
        public static ConfigElement<bool> Bypass_UniverseLib_ICall;
        public static ConfigElement<int> Target_Display;
        public static ConfigElement<bool> Force_Unlock_Mouse;
        public static ConfigElement<KeyCode> Force_Unlock_Toggle;
        public static ConfigElement<string> Default_Output_Path;
        public static ConfigElement<string> DnSpy_Path;
        public static ConfigElement<bool> Log_Unity_Debug;
        public static ConfigElement<bool> Log_To_Disk;
        public static ConfigElement<UIManager.VerticalAnchor> Main_Navbar_Anchor;
        public static ConfigElement<KeyCode> World_MouseInspect_Keybind;
        public static ConfigElement<KeyCode> UI_MouseInspect_Keybind;
        public static ConfigElement<KeyCode> TimeScale_Toggle_Keybind;
        public static ConfigElement<KeyCode> TimeScale_Zero_Keybind;
        public static ConfigElement<KeyCode> TimeScale_Normal_Keybind;
        public static ConfigElement<KeyCode> TimeScale_Half_Keybind;
        public static ConfigElement<KeyCode> TimeScale_Double_Keybind;
        public static ConfigElement<string> CSConsole_Assembly_Blacklist;
        public static ConfigElement<string> Reflection_Signature_Blacklist;
        public static ConfigElement<bool> Reflection_Hide_NativeInfoPtrs;

        public static ConfigElement<bool> McpBridge_Enabled;

        public static ConfigElement<int> McpBridge_Port;

        public static ConfigElement<int> McpBridge_RequestTimeoutMs;

        public static ConfigElement<int> McpBridge_MaxRequestsPerFrame;

        public static ConfigElement<int> McpBridge_MaxFrameBudgetMs;

        public static ConfigElement<ParalivesSafeActionMode> Paralives_SafeActionMode;

        public static ConfigElement<int> Paralives_SavedGameListLimit;

        public static ConfigElement<int> Paralives_LoadingWaitTimeoutMs;

        public static ConfigElement<bool> Paralives_PreferUiFlowForSaveLoad;

        public enum Language
        {
            English,
            Chinese
        }

        public enum ParalivesSafeActionMode
        {
            ConfirmRequired,
            OneClickInUI
        }

        // internal configs
        internal static InternalConfigHandler InternalHandler { get; private set; }
        internal static readonly Dictionary<UIManager.Panels, ConfigElement<string>> PanelSaveData = new();

        internal static ConfigElement<string> GetPanelSaveData(UIManager.Panels panel)
        {
            if (!PanelSaveData.ContainsKey(panel))
                PanelSaveData.Add(panel, new ConfigElement<string>(panel.ToString(), string.Empty, string.Empty, true));
            return PanelSaveData[panel];
        }

        public static void Init(ConfigHandler configHandler)
        {
            Handler = configHandler;
            Handler.Init();

            InternalHandler = new InternalConfigHandler();
            InternalHandler.Init();

            CreateConfigElements();

            Handler.LoadConfig();
            InternalHandler.LoadConfig();

#if STANDALONE
            Loader.Standalone.ExplorerEditorBehaviour.Instance?.LoadConfigs();
#endif
        }

        internal static void RegisterConfigElement<T>(ConfigElement<T> configElement)
        {
            if (!configElement.IsInternal)
            {
                Handler.RegisterConfigElement(configElement);
                ConfigElements.Add(configElement.Name, configElement);
            }
            else
            {
                InternalHandler.RegisterConfigElement(configElement);
                InternalConfigs.Add(configElement.Name, configElement);
            }
        }

        private static void CreateConfigElements()
        {
            LanguageSetting = new("Language",
                "The language used by UnityExplorer. Requires restart to fully take effect.",
                DetectDefaultLanguage(),
                category: "General",
                requiresRestart: true);

            Master_Toggle = new("UnityExplorer Toggle",
                "The key to enable or disable UnityExplorer's menu and features.",
                KeyCode.F7,
                category: "General");

            Hide_On_Startup = new("Hide On Startup",
                "Should UnityExplorer be hidden on startup?",
                false,
                category: "UI");

            McpBridge_Enabled = new("MCP Bridge Enabled",
                "Expose a local WebSocket bridge for Model Context Protocol tooling.",
                true,
                category: "MCP",
                requiresRestart: true);

            McpBridge_Port = new("MCP Bridge Port",
                "The localhost WebSocket port used by the UnityExplorer MCP bridge.",
                8765,
                category: "MCP",
                requiresRestart: true);

            McpBridge_RequestTimeoutMs = new("MCP Bridge Request Timeout Ms",
                "How long the bridge waits for Unity main-thread MCP commands before timing out.",
                5000,
                category: "MCP");

            McpBridge_MaxRequestsPerFrame = new("MCP Bridge Max Requests Per Frame",
                "Maximum MCP bridge requests to execute on the Unity main thread in one frame.",
                2,
                category: "MCP");

            McpBridge_MaxFrameBudgetMs = new("MCP Bridge Max Frame Budget Ms",
                "Maximum MCP bridge main-thread time budget per frame. A single request is allowed to finish even if it exceeds this budget.",
                2,
                category: "MCP");

            Paralives_SafeActionMode = new("Paralives Safe Action Mode",
                "Controls whether game-side Paralives UI actions require a second click confirmation. MCP writes still require dryRun false and the confirmation phrase.",
                ParalivesSafeActionMode.ConfirmRequired,
                category: "Paralives");

            Paralives_SavedGameListLimit = new("Paralives Saved Game List Limit",
                "Maximum saved games to display in the Paralives panel.",
                50,
                category: "Paralives");

            Paralives_LoadingWaitTimeoutMs = new("Paralives Loading Wait Timeout Ms",
                "Maximum time to wait for Paralives loading actions before treating them as timed out.",
                30000,
                category: "Paralives",
                advanced: true);

            Paralives_PreferUiFlowForSaveLoad = new("Paralives Prefer UI Flow For Save Load",
                "Prefer visible Paralives UI flows for save loading when available; fallback methods are shown before execution.",
                true,
                category: "Paralives");

            Startup_Delay_Time = new("Startup Delay Time",
                "The delay on startup before the UI is created.",
                1f,
                category: "UI",
                requiresRestart: true);

            Target_Display = new("Target Display",
                "The monitor index for UnityExplorer to use, if you have multiple. 0 is the default display, 1 is secondary, etc. " +
                "Restart recommended when changing this setting. Make sure your extra monitors are the same resolution as your primary monitor.",
                0,
                category: "UI",
                requiresRestart: true);

            Force_Unlock_Mouse = new("Force Unlock Mouse",
                "Force the Cursor to be unlocked (visible) when the UnityExplorer menu is open.",
                true,
                category: "UI");
            Force_Unlock_Mouse.OnValueChanged += (bool value) => UniverseLib.Config.ConfigManager.Force_Unlock_Mouse = value;

            Force_Unlock_Toggle = new("Force Unlock Toggle Key",
                "The keybind to toggle the 'Force Unlock Mouse' setting. Only usable when UnityExplorer is open.",
                KeyCode.None,
                category: "UI");

            Disable_EventSystem_Override = new("Disable EventSystem override",
                "If enabled, UnityExplorer will not override the EventSystem from the game.\n<b>May require restart to take effect.</b>",
                false,
                category: "Advanced",
                requiresRestart: true,
                advanced: true);
            Disable_EventSystem_Override.OnValueChanged += (bool value) => UniverseLib.Config.ConfigManager.Disable_EventSystem_Override = value;

            Disable_Setup_Force_ReLoad_ManagedAssemblies = new("Disable Setup Force Reload ManagedAssemblies",
                "If enabled, UnityExplorer will not reload ManagedAssemblies on setup. Currently only Mono is supported.\n<b>May require restart to take effect.</b>",
                false,
                category: "Advanced",
                requiresRestart: true,
                advanced: true);
            Disable_Setup_Force_ReLoad_ManagedAssemblies.OnValueChanged += (bool value) => UniverseLib.Config.ConfigManager.Disable_Setup_Force_ReLoad_ManagedAssemblies = value;

            Bypass_UniverseLib_ICall = new("Bypass UniverseLib ICall",
                "If enabled, UnityExplorer will bypass UniverseLib's ICall reflection system. This may help with compatibility in some games.\n<b>May require restart to take effect.</b>",
                false,
                category: "Advanced",
                requiresRestart: true,
                advanced: true);
            Bypass_UniverseLib_ICall.OnValueChanged += (bool value) => UniverseLib.Config.ConfigManager.Bypass_UniverseLib_ICall = value;

            Default_Output_Path = new("Default Output Path",
                "The default output path when exporting things from UnityExplorer.",
                Path.Combine(ExplorerCore.ExplorerFolder, "Output"),
                category: "Export");

            DnSpy_Path = new("dnSpy Path",
                "The full path to dnSpy.exe (64-bit).",
                @"C:/Program Files/dnspy/dnSpy.exe",
                category: "Inspector",
                advanced: true);

            Main_Navbar_Anchor = new("Main Navbar Anchor",
                "The vertical anchor of the main UnityExplorer Navbar, in case you want to move it.",
                UIManager.VerticalAnchor.Top,
                category: "UI");

            Log_Unity_Debug = new("Log Unity Debug",
                "Should UnityEngine.Debug.Log messages be printed to UnityExplorer's log?",
                false,
                category: "Console");

            Log_To_Disk = new("Log To Disk",
                "Should UnityExplorer save log files to the disk?",
                true,
                category: "Console");

            World_MouseInspect_Keybind = new("World Mouse-Inspect Keybind",
                "Optional keybind to being a World-mode Mouse Inspect.",
                KeyCode.None,
                category: "Inspector");

            UI_MouseInspect_Keybind = new("UI Mouse-Inspect Keybind",
                "Optional keybind to begin a UI-mode Mouse Inspect.",
                KeyCode.None,
                category: "Inspector");

            TimeScale_Toggle_Keybind = new("TimeScale Toggle Keybind",
                "Optional keybind to lock or unlock Time.timeScale.",
                KeyCode.None,
                category: "UI");

            TimeScale_Zero_Keybind = new("TimeScale Zero Keybind",
                "Optional keybind to lock Time.timeScale to 0.0.",
                KeyCode.None,
                category: "UI");

            TimeScale_Normal_Keybind = new("TimeScale Normal Keybind",
                "Optional keybind to lock Time.timeScale to 1.0.",
                KeyCode.None,
                category: "UI");

            TimeScale_Half_Keybind = new("TimeScale Half Keybind",
                "Optional keybind to halve and lock the current Time.timeScale target.",
                KeyCode.None,
                category: "UI");

            TimeScale_Double_Keybind = new("TimeScale Double Keybind",
                "Optional keybind to double and lock the current Time.timeScale target.",
                KeyCode.None,
                category: "UI");

            CSConsole_Assembly_Blacklist = new("CSharp Console Assembly Blacklist",
                "Use this to blacklist Assembly names from being referenced by the C# Console. Requires a Reset of the C# Console.\n" +
                "Separate each Assembly with a semicolon ';'." +
                "For example, to blacklist Assembly-CSharp, you would add 'Assembly-CSharp;'",
                "",
                category: "Console",
                advanced: true);

            Reflection_Signature_Blacklist = new("Member Signature Blacklist",
                "Use this to blacklist certain member signatures if they are known to cause a crash or other issues.\r\n" +
                "Seperate signatures with a semicolon ';'.\r\n" +
                "For example, to blacklist Camera.main, you would add 'UnityEngine.Camera.main;'",
                "",
                category: "Inspector",
                advanced: true);

            Reflection_Hide_NativeInfoPtrs = new("Hide NativeMethodInfoPtr_s and NativeFieldInfoPtr_s",
                "Use this to blacklist NativeMethodPtr_s and NativeFieldInfoPtrs_s from the class inspector, mainly to reduce clutter.\r\n" +
                "For example, this will hide 'Class.NativeFieldInfoPtr_value' for the field 'Class.value'.",
                false,
                category: "Inspector",
                advanced: true);
        }

        private static Language DetectDefaultLanguage()
        {
            try
            {
                string unityLanguage = Application.systemLanguage.ToString();
                if (unityLanguage.StartsWith("Chinese", StringComparison.OrdinalIgnoreCase))
                    return Language.Chinese;

                string cultureName = CultureInfo.CurrentUICulture.Name;
                if (cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                    return Language.Chinese;
            }
            catch
            {
            }

            return Language.English;
        }
    }
}
