using UnityExplorer.Localization;
using UnityExplorer.UI.Widgets;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;

#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif

#if INTEROP
using Il2CppInterop.Runtime.Injection;
#endif

namespace UnityExplorer.UI.Panels
{
    internal class FreeCamPanel : UEPanel
    {
        public FreeCamPanel(UIBase owner) : base(owner) { }

        public override string Name => Localizer.Get("PANEL_FREECAM", "Freecam");
        public override UIManager.Panels PanelType => UIManager.Panels.Freecam;
        public override int MinWidth => 430;
        public override int MinHeight => 350;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;

        internal static bool inFreeCamMode;
        internal static bool usingGameCamera;
        internal static Camera ourCamera;
        internal static Camera lastMainCamera;
        internal static FreeCamBehaviour freeCamScript;
        internal static float desiredMoveSpeed = 10f;
        internal static Vector3 originalCameraPosition;
        internal static Quaternion originalCameraRotation;
        internal static Vector3? currentUserCameraPosition;
        internal static Quaternion? currentUserCameraRotation;
        internal static Vector3 previousMousePosition;
        internal static Vector3 lastSetCameraPosition;

        private static ButtonRef startStopButton;
        private static Toggle useGameCameraToggle;
        private static InputFieldRef positionInput;
        private static InputFieldRef moveSpeedInput;
        private static InputFieldRef fovInput;
        private static ButtonRef inspectButton;
        private static Text statusLabel;

        internal static void BeginFreecam()
        {
            inFreeCamMode = true;
            previousMousePosition = InputManager.MousePosition;
            CacheMainCamera();
            SetupFreeCamera();
            inspectButton.GameObject.SetActive(true);
            RefreshStatus();
        }

        private static void CacheMainCamera()
        {
            Camera currentMain = Camera.main;
            if (currentMain)
            {
                lastMainCamera = currentMain;
                originalCameraPosition = currentMain.transform.position;
                originalCameraRotation = currentMain.transform.rotation;

                if (currentUserCameraPosition == null)
                {
                    currentUserCameraPosition = currentMain.transform.position;
                    currentUserCameraRotation = currentMain.transform.rotation;
                }
            }
            else
            {
                originalCameraRotation = Quaternion.identity;
            }
        }

        private static void SetupFreeCamera()
        {
            if (useGameCameraToggle.isOn)
            {
                if (!lastMainCamera)
                {
                    ExplorerCore.LogWarning("There is no previous Camera found, reverting to default Free Cam.");
                    useGameCameraToggle.isOn = false;
                }
                else
                {
                    usingGameCamera = true;
                    ourCamera = lastMainCamera;
                }
            }

            if (!useGameCameraToggle.isOn)
            {
                usingGameCamera = false;
                if (lastMainCamera)
                    lastMainCamera.enabled = false;
            }

            if (!ourCamera)
            {
                ourCamera = new GameObject("UE_Freecam").AddComponent<Camera>();
                ourCamera.gameObject.tag = "MainCamera";
                GameObject.DontDestroyOnLoad(ourCamera.gameObject);
                ourCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            if (!freeCamScript)
                freeCamScript = ourCamera.gameObject.AddComponent<FreeCamBehaviour>();

            ourCamera.transform.position = (Vector3)currentUserCameraPosition;
            ourCamera.transform.rotation = (Quaternion)currentUserCameraRotation;
            if (fovInput != null && float.TryParse(fovInput.Text, out float fov))
                ourCamera.fieldOfView = Mathf.Clamp(fov, 1f, 179f);

            ourCamera.gameObject.SetActive(true);
            ourCamera.enabled = true;
            RefreshStatus();
        }

        internal static void EndFreecam()
        {
            inFreeCamMode = false;

            if (usingGameCamera)
            {
                ourCamera = null;
                if (lastMainCamera)
                {
                    lastMainCamera.transform.position = originalCameraPosition;
                    lastMainCamera.transform.rotation = originalCameraRotation;
                }
            }

            if (ourCamera)
                ourCamera.gameObject.SetActive(false);
            else
                inspectButton.GameObject.SetActive(false);

            if (freeCamScript)
            {
                GameObject.Destroy(freeCamScript);
                freeCamScript = null;
            }

            if (lastMainCamera)
                lastMainCamera.enabled = true;

            RefreshStatus();
        }

        private static void SetCameraPosition(Vector3 pos)
        {
            if (!ourCamera || lastSetCameraPosition == pos)
                return;

            ourCamera.transform.position = pos;
            lastSetCameraPosition = pos;
            RefreshStatus();
        }

        internal static void UpdatePositionInput()
        {
            if (!ourCamera || positionInput.Component.isFocused)
                return;

            lastSetCameraPosition = ourCamera.transform.position;
            positionInput.Text = ParseUtility.ToStringForInput<Vector3>(lastSetCameraPosition);
            RefreshStatus();
        }

        protected override void ConstructPanelContent()
        {
            statusLabel = UEUI.CreateStatus(ContentRoot, "FreecamStatus", "Freecam inactive.");

            startStopButton = UIFactory.CreateButton(ContentRoot, "ToggleButton", Localizer.Get("BTN_FREECAM", "Freecam"));
            UIFactory.SetLayoutElement(startStopButton.GameObject, minWidth: 150, minHeight: 25, flexibleWidth: 9999);
            startStopButton.OnClick += StartStopButton_OnClick;
            SetToggleButtonState();

            AddSpacer(5);

            GameObject toggleObj = UIFactory.CreateToggle(ContentRoot, "UseGameCameraToggle", out useGameCameraToggle, out Text toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, flexibleWidth: 9999);
            useGameCameraToggle.onValueChanged.AddListener(OnUseGameCameraToggled);
            useGameCameraToggle.isOn = false;
            toggleText.text = Localizer.Get("LBL_USE_GAME_CAMERA", "Use Game Camera?");

            AddSpacer(5);

            GameObject posRow = AddInputField("Position", Localizer.Get("LBL_FREECAM_POS", "Freecam Pos:"), Localizer.Get("TXT_FREECAM_POS_PLACEHOLDER", "eg. 0 0 0"), out positionInput, PositionInput_OnEndEdit);
            ButtonRef resetPosButton = UIFactory.CreateButton(posRow, "ResetButton", Localizer.Get("BTN_RESET", "Reset"));
            UIFactory.SetLayoutElement(resetPosButton.GameObject, minWidth: 70, minHeight: 25);
            resetPosButton.OnClick += OnResetPosButtonClicked;

            AddSpacer(5);

            AddInputField("MoveSpeed", Localizer.Get("LBL_MOVE_SPEED", "Move Speed:"), Localizer.Get("TXT_MOVE_SPEED_PLACEHOLDER", "Default: 1"), out moveSpeedInput, MoveSpeedInput_OnEndEdit);
            moveSpeedInput.Text = desiredMoveSpeed.ToString();

            GameObject speedPresetRow = UIFactory.CreateHorizontalGroup(ContentRoot, "SpeedPresets", false, false, true, true, 4, default, new(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(speedPresetRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);
            AddSpeedPreset(speedPresetRow, "Slow", 2f);
            AddSpeedPreset(speedPresetRow, "Normal", 10f);
            AddSpeedPreset(speedPresetRow, "Fast", 30f);

            AddSpacer(5);

            AddInputField("Fov", "FOV:", "Default: 60", out fovInput, FovInput_OnEndEdit);
            fovInput.Text = "60";

            AddSpacer(5);

            string instructions = Localizer.Get("TXT_FREECAM_INSTRUCTIONS", @"Controls:
- WASD / Arrows: Movement
- Space / PgUp: Move up
- LeftCtrl / PgDown: Move down
- Right Mouse Button: Free look
- Shift: Super speed");
            Text instructionsText = UIFactory.CreateLabel(ContentRoot, "Instructions", instructions, TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(instructionsText.gameObject, flexibleWidth: 9999, flexibleHeight: 9999);

            AddSpacer(5);

            inspectButton = UIFactory.CreateButton(ContentRoot, "InspectButton", Localizer.Get("BTN_INSPECT_FREECAM", "Inspect Free Camera"));
            UIFactory.SetLayoutElement(inspectButton.GameObject, flexibleWidth: 9999, minHeight: 25);
            inspectButton.OnClick += () => { InspectorManager.Inspect(ourCamera); };
            inspectButton.GameObject.SetActive(false);

            ButtonRef teleportButton = UIFactory.CreateButton(ContentRoot, "TeleportSelected", "Teleport to Selected");
            UIFactory.SetLayoutElement(teleportButton.GameObject, flexibleWidth: 9999, minHeight: 25);
            teleportButton.OnClick += OnTeleportSelectedClicked;

            AddSpacer(5);
        }

        private void AddSpacer(int height)
        {
            GameObject obj = UIFactory.CreateUIObject("Spacer", ContentRoot);
            UIFactory.SetLayoutElement(obj, minHeight: height, flexibleHeight: 0);
        }

        private void AddSpeedPreset(GameObject parent, string label, float speed)
        {
            ButtonRef button = UIFactory.CreateButton(parent, "Speed_" + label, label);
            UIFactory.SetLayoutElement(button.GameObject, minWidth: 75, minHeight: 25, flexibleWidth: 0);
            button.OnClick += () =>
            {
                desiredMoveSpeed = speed;
                moveSpeedInput.Text = speed.ToString();
                RefreshStatus();
            };
        }

        private GameObject AddInputField(string name, string labelText, string placeHolder, out InputFieldRef inputField, Action<string> onInputEndEdit)
        {
            GameObject row = UIFactory.CreateHorizontalGroup(ContentRoot, $"{name}_Group", false, false, true, true, 3, default, new(1, 1, 1, 0));
            Text label = UIFactory.CreateLabel(row, $"{name}_Label", labelText);
            UIFactory.SetLayoutElement(label.gameObject, minWidth: 100, minHeight: 25);
            inputField = UIFactory.CreateInputField(row, $"{name}_Input", placeHolder);
            UIFactory.SetLayoutElement(inputField.GameObject, minWidth: 125, minHeight: 25, flexibleWidth: 9999);
            inputField.Component.GetOnEndEdit().AddListener(onInputEndEdit);
            return row;
        }

        private void StartStopButton_OnClick()
        {
            EventSystemHelper.SetSelectedGameObject(null);
            if (inFreeCamMode)
                EndFreecam();
            else
                BeginFreecam();
            SetToggleButtonState();
        }

        private void SetToggleButtonState()
        {
            if (inFreeCamMode)
            {
                RuntimeHelper.SetColorBlockAuto(startStopButton.Component, new(0.4f, 0.2f, 0.2f));
                startStopButton.ButtonText.text = Localizer.Get("BTN_END_FREECAM", "End Freecam");
            }
            else
            {
                RuntimeHelper.SetColorBlockAuto(startStopButton.Component, new(0.2f, 0.4f, 0.2f));
                startStopButton.ButtonText.text = Localizer.Get("BTN_BEGIN_FREECAM", "Begin Freecam");
            }
            RefreshStatus();
        }

        private void OnUseGameCameraToggled(bool value)
        {
            EventSystemHelper.SetSelectedGameObject(null);
            if (!inFreeCamMode)
                return;
            EndFreecam();
            BeginFreecam();
            SetToggleButtonState();
        }

        private void OnResetPosButtonClicked()
        {
            currentUserCameraPosition = originalCameraPosition;
            currentUserCameraRotation = originalCameraRotation;
            if (inFreeCamMode && ourCamera)
            {
                ourCamera.transform.position = (Vector3)currentUserCameraPosition;
                ourCamera.transform.rotation = (Quaternion)currentUserCameraRotation;
            }
            positionInput.Text = ParseUtility.ToStringForInput<Vector3>(originalCameraPosition);
            RefreshStatus();
        }

        private void PositionInput_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);
            if (!ParseUtility.TryParse(input, out Vector3 parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse position to Vector3: {parseEx.ReflectionExToString()}");
                UpdatePositionInput();
                return;
            }
            SetCameraPosition(parsed);
        }

        private void MoveSpeedInput_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);
            if (!ParseUtility.TryParse(input, out float parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                moveSpeedInput.Text = desiredMoveSpeed.ToString();
                return;
            }
            desiredMoveSpeed = parsed;
            RefreshStatus();
        }

        private void FovInput_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);
            if (!ParseUtility.TryParse(input, out float parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse FOV: {parseEx.ReflectionExToString()}");
                fovInput.Text = ourCamera ? ourCamera.fieldOfView.ToString() : "60";
                return;
            }
            parsed = Mathf.Clamp(parsed, 1f, 179f);
            if (ourCamera)
                ourCamera.fieldOfView = parsed;
            fovInput.Text = parsed.ToString();
            RefreshStatus();
        }

        private void OnTeleportSelectedClicked()
        {
            object target = InspectorManager.ActiveInspector?.Target;
            GameObject go = target as GameObject;
            if (!go && target is Component component)
                go = component.gameObject;

            if (!go)
            {
                ExplorerCore.LogWarning("Freecam teleport needs the active Inspector target to be a GameObject or Component.");
                return;
            }

            currentUserCameraPosition = go.transform.position;
            if (inFreeCamMode && ourCamera)
                ourCamera.transform.position = go.transform.position;
            UpdatePositionInput();
            RefreshStatus();
        }

        private static void RefreshStatus()
        {
            if (!statusLabel)
                return;

            string mode = !inFreeCamMode ? "Inactive" : usingGameCamera ? "Active | Game camera" : "Active | Custom camera";
            string pos = ourCamera ? ParseUtility.ToStringForInput<Vector3>(ourCamera.transform.position) : "none";
            string fov = ourCamera ? ourCamera.fieldOfView.ToString("0.0") : "n/a";
            statusLabel.text = $"{mode} | speed {desiredMoveSpeed:0.##} | FOV {fov} | pos {pos}";
        }
    }

    internal class FreeCamBehaviour : MonoBehaviour
    {
#if CPP
        static FreeCamBehaviour()
        {
            ClassInjector.RegisterTypeInIl2Cpp<FreeCamBehaviour>();
        }

        public FreeCamBehaviour(IntPtr ptr) : base(ptr) { }
#endif

        internal void Update()
        {
            if (!FreeCamPanel.inFreeCamMode)
                return;

            if (!FreeCamPanel.ourCamera)
            {
                FreeCamPanel.EndFreecam();
                return;
            }

            Transform transform = FreeCamPanel.ourCamera.transform;
            FreeCamPanel.currentUserCameraPosition = transform.position;
            FreeCamPanel.currentUserCameraRotation = transform.rotation;

            float moveSpeed = FreeCamPanel.desiredMoveSpeed * Time.deltaTime;
            if (InputManager.GetKey(KeyCode.LeftShift) || InputManager.GetKey(KeyCode.RightShift))
                moveSpeed *= 10f;

            if (InputManager.GetKey(KeyCode.LeftArrow) || InputManager.GetKey(KeyCode.A))
                transform.position += transform.right * -1 * moveSpeed;
            if (InputManager.GetKey(KeyCode.RightArrow) || InputManager.GetKey(KeyCode.D))
                transform.position += transform.right * moveSpeed;
            if (InputManager.GetKey(KeyCode.UpArrow) || InputManager.GetKey(KeyCode.W))
                transform.position += transform.forward * moveSpeed;
            if (InputManager.GetKey(KeyCode.DownArrow) || InputManager.GetKey(KeyCode.S))
                transform.position += transform.forward * -1 * moveSpeed;
            if (InputManager.GetKey(KeyCode.Space) || InputManager.GetKey(KeyCode.PageUp))
                transform.position += transform.up * moveSpeed;
            if (InputManager.GetKey(KeyCode.LeftControl) || InputManager.GetKey(KeyCode.PageDown))
                transform.position += transform.up * -1 * moveSpeed;

            if (InputManager.GetMouseButton(1))
            {
                Vector3 mouseDelta = InputManager.MousePosition - FreeCamPanel.previousMousePosition;
                float newRotationX = transform.localEulerAngles.y + mouseDelta.x * 0.3f;
                float newRotationY = transform.localEulerAngles.x - mouseDelta.y * 0.3f;
                transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
            }

            FreeCamPanel.UpdatePositionInput();
            FreeCamPanel.previousMousePosition = InputManager.MousePosition;
        }
    }
}
