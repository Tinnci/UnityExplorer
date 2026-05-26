using System.Collections.Generic;
using UnityExplorer.Config;

namespace UnityExplorer.Localization
{
    public static class Localizer
    {
        private static readonly Dictionary<string, string> zhCN = new()
        {
            // Panel Names
            { "PANEL_OBJECT_EXPLORER", "物体浏览器" },
            { "PANEL_INSPECTOR", "检查器" },
            { "PANEL_CS_CONSOLE", "C# 控制台" },
            { "PANEL_HOOK_MANAGER", "Hook 管理器" },
            { "PANEL_FREECAM", "自由相机" },
            { "PANEL_CLIPBOARD", "剪贴板" },
            { "PANEL_LOG", "日志" },
            { "PANEL_OPTIONS", "设置" },

            // Tabs
            { "TAB_SCENE_EXPLORER", "场景浏览器" },
            { "TAB_OBJECT_SEARCH", "物体搜索" },

            // Buttons / Labels
            { "BTN_SEARCH", "搜索" },
            { "BTN_CLEAR", "清除" },
            { "BTN_RESET", "重置" },
            { "BTN_RUN", "运行" },
            { "BTN_SAVE", "保存" },
            { "BTN_CLOSE", "关闭" },
            { "LBL_ACTIVE", "活跃" },
            { "LBL_INACTIVE", "非活跃" },
            { "LBL_ALL", "全部" },
            { "LBL_ENABLED", "已启用" },
            { "LBL_DISABLED", "已禁用" },

            // CS Console Specific
            { "BTN_COMPILE", "编译" },
            { "LBL_HELP", "帮助" },
            { "LBL_COMPILE_CTRL_R", "按 Ctrl+R 编译" },
            { "LBL_SUGGESTIONS", "代码建议" },
            { "LBL_AUTO_INDENT", "自动缩进" },

            // Options Specific
            { "BTN_SAVE_OPTIONS", "保存设置" },

            // Log Specific
            { "BTN_OPEN_LOG_FILE", "打开日志文件" },
            { "LBL_LOG_UNITY_DEBUG", "记录 Unity 调试日志" },

            // Clipboard Specific
            { "LBL_CURRENT_PASTE", "当前剪贴内容:" },
            { "BTN_CLEAR_CLIPBOARD", "清空剪贴板" },
            { "BTN_INSPECT", "检查" },
            { "BTN_CLOSE_ALL", "全部关闭" },
            { "MOUSE_INSPECT", "鼠标检查" },
        };

        public static string Get(string key, string defaultEnglish)
        {
            if (ConfigManager.LanguageSetting != null && 
                ConfigManager.LanguageSetting.Value == ConfigManager.Language.Chinese)
            {
                if (zhCN.TryGetValue(key, out string value))
                {
                    return value;
                }
            }
            return defaultEnglish;
        }
    }
}
