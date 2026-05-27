using HarmonyLib;
using UnityExplorer.Localization;
using UniverseLib.UI;
using UniverseLib.UI.Models;

#if UNHOLLOWER
using IL2CPPUtils = UnhollowerBaseLib.UnhollowerUtils;
#endif

#if INTEROP
using IL2CPPUtils = Il2CppInterop.Common.Il2CppInteropUtils;
#endif

namespace UnityExplorer.UI.Widgets
{
    internal class TimeScaleWidget
    {
        internal static TimeScaleWidget Instance { get; private set; }

        internal static void SetUp(GameObject parent)
        {
            Instance = new TimeScaleWidget(parent);
        }

        private TimeScaleWidget(GameObject parent)
        {
            DesiredTime = Time.timeScale;
            ConstructUI(parent);
            InitPatch();
        }

        internal float DesiredTime { get; private set; }

        private ButtonRef lockBtn;
        private bool locked;
        private InputFieldRef timeInput;
        private bool settingTimeScale;

        internal void Update()
        {
            // Fallback in case Time.timeScale patch failed for whatever reason.
            if (locked)
                UpdateTimeScale();

            if (!timeInput.Component.isFocused)
                timeInput.Text = Time.timeScale.ToString("F2");
        }

        internal void LockTo(float timeScale)
        {
            locked = true;
            SetTimeScale(timeScale);
            UpdateUi();
        }

        internal void ToggleLock()
        {
            OnPauseButtonClicked();
        }

        private void UpdateTimeScale()
        {
            settingTimeScale = true;
            Time.timeScale = DesiredTime;
            settingTimeScale = false;
        }

        private void SetTimeScale(float time)
        {
            DesiredTime = time;
            UpdateTimeScale();
        }

        // UI event listeners

        private void OnTimeInputEndEdit(string val)
        {
            if (float.TryParse(val, out float f))
                SetTimeScale(f);
        }

        private void OnPauseButtonClicked()
        {
            OnTimeInputEndEdit(timeInput.Text);
            locked = !locked;
            UpdateUi();
        }

        private void UpdateUi()
        {
            Color color = locked ? new Color(0.3f, 0.3f, 0.2f) : new Color(0.2f, 0.2f, 0.2f);
            RuntimeHelper.SetColorBlock(lockBtn.Component, color, color * 1.2f, color * 0.7f);
            lockBtn.ButtonText.text = locked
                ? Localizer.Get("BTN_UNLOCK", "Unlock")
                : Localizer.Get("BTN_LOCK", "Lock");
        }

        // UI Construction

        private void ConstructUI(GameObject parent)
        {
            Text timeLabel = UIFactory.CreateLabel(parent, "TimeLabel", Localizer.Get("LBL_TIME_SCALE", "Time:"), TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(timeLabel.gameObject, minHeight: 25, minWidth: 35);

            timeInput = UIFactory.CreateInputField(parent, "TimeInput", "timeScale");
            UIFactory.SetLayoutElement(timeInput.Component.gameObject, minHeight: 25, minWidth: 40);
            timeInput.Component.GetOnEndEdit().AddListener(OnTimeInputEndEdit);

            timeInput.Text = string.Empty;
            timeInput.Text = Time.timeScale.ToString();

            lockBtn = UIFactory.CreateButton(parent, "PauseButton", Localizer.Get("BTN_LOCK", "Lock"), new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(lockBtn.Component.gameObject, minHeight: 25, minWidth: 50);
            lockBtn.OnClick += OnPauseButtonClicked;
        }

        // Only allow Time.timeScale to be set if the user has not locked it, or if we are setting it internally.

        private static void InitPatch()
        {
            try
            {
                MethodInfo target = typeof(Time).GetProperty("timeScale")?.GetSetMethod();
                if (target == null)
                    return;

#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(target) == null)
                    return;
#endif

                ExplorerCore.Harmony.Patch(target,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(TimeScaleWidget), nameof(Prefix_Time_set_timeScale))));
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Failed to patch Time.timeScale setter: {ex.Message}");
            }
        }

        private static bool Prefix_Time_set_timeScale()
        {
            return Instance == null || !Instance.locked || Instance.settingTimeScale;
        }
    }
}
