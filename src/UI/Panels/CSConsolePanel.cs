using System.Collections;
using UnityExplorer.CSConsole;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;
using UniverseLib.Localization;

namespace UnityExplorer.UI.Panels
{
    public class CSConsolePanel : UEPanel
    {
        public override string Name => LocalizationManager.GetString("CSConsolePanel_Title");
        public override UIManager.Panels PanelType => UIManager.Panels.CSConsole;

        public override int MinWidth => 750;
        public override int MinHeight => 300;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.175f);
        public override Vector2 DefaultAnchorMax => new(0.85f, 0.925f);

        public InputFieldScroller InputScroller { get; private set; }
        public InputFieldRef Input => InputScroller.InputField;
        public Text InputText { get; private set; }
        public Text HighlightText { get; private set; }
        public Text LineNumberText { get; private set; }

        public Dropdown HelpDropdown { get; private set; }

        // events
        public Action<string> OnInputChanged;
        public Action OnResetClicked;
        public Action OnCompileClicked;
        public Action<int> OnHelpDropdownChanged;
        public Action<bool> OnCtrlRToggled;
        public Action<bool> OnSuggestionsToggled;
        public Action<bool> OnAutoIndentToggled;
        public Action OnPanelResized;

        public CSConsolePanel(UIBase owner) : base(owner)
        {
        }

        private void InvokeOnValueChanged(string value)
        {
            if (value.Length == UniversalUI.MAX_INPUTFIELD_CHARS)
                ExplorerCore.LogWarning(string.Format(LocalizationManager.GetString("CSConsolePanel_Warning_MaxLengthReached"), UniversalUI.MAX_INPUTFIELD_CHARS));

            OnInputChanged?.Invoke(value);
        }

        public override void Update()
        {
            base.Update();

            ConsoleController.Update();
        }

        // UI Construction

        public override void OnFinishResize()
        {
            OnPanelResized?.Invoke();
        }

        protected override void ConstructPanelContent()
        {
            // Tools Row

            GameObject toolsRow = UIFactory.CreateHorizontalGroup(this.ContentRoot, "ToggleRow", false, false, true, true, 5, new Vector4(8, 8, 10, 5),
                default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(toolsRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Buttons

            ButtonRef compileButton = UIFactory.CreateButton(toolsRow, "CompileButton", LocalizationManager.GetString("CSConsolePanel_Button_Compile"), new Color(0.33f, 0.5f, 0.33f));
            UIFactory.SetLayoutElement(compileButton.Component.gameObject, minHeight: 28, minWidth: 130, flexibleHeight: 0);
            compileButton.ButtonText.fontSize = 15;
            compileButton.OnClick += () => { OnCompileClicked?.Invoke(); };

            ButtonRef resetButton = UIFactory.CreateButton(toolsRow, "ResetButton", LocalizationManager.GetString("CSConsolePanel_Button_Reset"), new Color(0.33f, 0.33f, 0.33f));
            UIFactory.SetLayoutElement(resetButton.Component.gameObject, minHeight: 28, minWidth: 80, flexibleHeight: 0);
            resetButton.ButtonText.fontSize = 15;
            resetButton.OnClick += () => { OnResetClicked?.Invoke(); };

            // Help dropdown

            GameObject helpDrop = UIFactory.CreateDropdown(toolsRow, "HelpDropdown", out Dropdown dropdown, LocalizationManager.GetString("CSConsolePanel_Dropdown_Help_DefaultText"), 14, null);
            UIFactory.SetLayoutElement(helpDrop, minHeight: 25, minWidth: 100);
            HelpDropdown = dropdown;
            HelpDropdown.onValueChanged.AddListener((int val) => { this.OnHelpDropdownChanged?.Invoke(val); });

            // Enable Ctrl+R toggle

            GameObject ctrlRToggleObj = UIFactory.CreateToggle(toolsRow, "CtrlRToggle", out Toggle CtrlRToggle, out Text ctrlRToggleText);
            UIFactory.SetLayoutElement(ctrlRToggleObj, minWidth: 150, flexibleWidth: 0, minHeight: 25);
            ctrlRToggleText.alignment = TextAnchor.UpperLeft;
            ctrlRToggleText.text = LocalizationManager.GetString("CSConsolePanel_Toggle_CompileCtrlR");
            CtrlRToggle.onValueChanged.AddListener((bool val) => { OnCtrlRToggled?.Invoke(val); });

            // Enable Suggestions toggle

            GameObject suggestToggleObj = UIFactory.CreateToggle(toolsRow, "SuggestionToggle", out Toggle SuggestionsToggle, out Text suggestToggleText);
            UIFactory.SetLayoutElement(suggestToggleObj, minWidth: 120, flexibleWidth: 0, minHeight: 25);
            suggestToggleText.alignment = TextAnchor.UpperLeft;
            suggestToggleText.text = LocalizationManager.GetString("CSConsolePanel_Toggle_Suggestions");
            SuggestionsToggle.onValueChanged.AddListener((bool val) => { OnSuggestionsToggled?.Invoke(val); });

            // Enable Auto-indent toggle

            GameObject autoIndentToggleObj = UIFactory.CreateToggle(toolsRow, "IndentToggle", out Toggle AutoIndentToggle, out Text autoIndentToggleText);
            UIFactory.SetLayoutElement(autoIndentToggleObj, minWidth: 120, flexibleWidth: 0, minHeight: 25);
            autoIndentToggleText.alignment = TextAnchor.UpperLeft;
            autoIndentToggleText.text = LocalizationManager.GetString("CSConsolePanel_Toggle_AutoIndent");
            AutoIndentToggle.onValueChanged.AddListener((bool val) => { OnAutoIndentToggled?.Invoke(val); });

            // Console Input

            GameObject inputArea = UIFactory.CreateUIObject("InputGroup", ContentRoot);
            UIFactory.SetLayoutElement(inputArea, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(inputArea, false, true, true, true);
            inputArea.AddComponent<Image>().color = Color.white;
            inputArea.AddComponent<Mask>().showMaskGraphic = false;

            // line numbers

            GameObject linesHolder = UIFactory.CreateUIObject("LinesHolder", inputArea);
            RectTransform linesRect = linesHolder.GetComponent<RectTransform>();
            linesRect.pivot = new Vector2(0, 1);
            linesRect.anchorMin = new Vector2(0, 0);
            linesRect.anchorMax = new Vector2(0, 1);
            linesRect.sizeDelta = new Vector2(0, 305000);
            linesRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 50);
            linesHolder.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(linesHolder, true, true, true, true);

            LineNumberText = UIFactory.CreateLabel(linesHolder, "LineNumbers", LocalizationManager.GetString("CSConsolePanel_LineNumber_DefaultText"), TextAnchor.UpperCenter, Color.grey, fontSize: 16);
            LineNumberText.font = UniversalUI.ConsoleFont;

            // input field

            int fontSize = 16;

            GameObject inputObj = UIFactory.CreateScrollInputField(inputArea, "ConsoleInput", LocalizationManager.GetString("CSConsolePanel_InputField_Placeholder"),
                out InputFieldScroller inputScroller, fontSize);
            InputScroller = inputScroller;
            ConsoleController.DefaultInputFieldAlpha = Input.Component.selectionColor.a;
            Input.OnValueChanged += InvokeOnValueChanged;

            // move line number text with input field
            linesRect.transform.SetParent(inputObj.transform.Find("Viewport"), false);
            inputScroller.Slider.Scrollbar.onValueChanged.AddListener((float val) => { SetLinesPosition(); });
            inputScroller.Slider.Slider.onValueChanged.AddListener((float val) => { SetLinesPosition(); });
            void SetLinesPosition()
            {
                linesRect.anchoredPosition = new Vector2(linesRect.anchoredPosition.x, inputScroller.ContentRect.anchoredPosition.y);
                //SetInputLayout();
            }

            InputText = Input.Component.textComponent;
            InputText.supportRichText = false;
            InputText.color = Color.clear;
            Input.Component.customCaretColor = true;
            Input.Component.caretColor = Color.white;
            Input.PlaceholderText.fontSize = fontSize;

            // Lexer highlight text overlay
            GameObject highlightTextObj = UIFactory.CreateUIObject("HighlightText", InputText.gameObject);
            RectTransform highlightTextRect = highlightTextObj.GetComponent<RectTransform>();
            highlightTextRect.pivot = new Vector2(0, 1);
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = Vector2.zero;
            highlightTextRect.offsetMax = Vector2.zero;

            HighlightText = highlightTextObj.AddComponent<Text>();
            HighlightText.color = Color.white;
            HighlightText.supportRichText = true;
            HighlightText.fontSize = fontSize;

            // Set fonts
            InputText.font = UniversalUI.ConsoleFont;
            Input.PlaceholderText.font = UniversalUI.ConsoleFont;
            HighlightText.font = UniversalUI.ConsoleFont;

            RuntimeHelper.StartCoroutine(DelayedLayoutSetup());
        }

        private IEnumerator DelayedLayoutSetup()
        {
            yield return null;
            SetInputLayout();
        }

        public void SetInputLayout()
        {
            Input.Transform.offsetMin = new Vector2(52, Input.Transform.offsetMin.y);
            Input.Transform.offsetMax = new Vector2(2, Input.Transform.offsetMax.y);
        }
    }
}
