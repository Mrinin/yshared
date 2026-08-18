using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YShared.NamedTimers;
using YShared.Singleton;

namespace YShared.Console
{

    public class DevConsoleUI : YSingleton<DevConsoleUI>
    {
        public override bool CallDontDestroyOnLoad => true;

        [Header("Appearance")]
        [SerializeField] private int fontSize = 18;
        [SerializeField] private float slideDuration = 0.15f;

        [Header("Autocomplete")]
        [Tooltip("Editable at runtime via AddAutocompleteCommand / RemoveAutocompleteCommand / SetAutocompleteList.")]
        string[] autocompleteCommands;
        [SerializeField] private int maxAutocompleteResults = 6;

        private const int MAX_LOG_ENTRIES = 100;
        private const int MAX_COMMMAND_HISTORY = 10;

        // -- runtime built UI --
        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform panelRect;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform contentRect;
        [SerializeField] TextMeshProUGUI logText;
        [SerializeField] TMP_InputField inputField;
        [SerializeField] RectTransform inputRect;
        [SerializeField] RectTransform autocompleteRoot;
        [SerializeField] List<TextMeshProUGUI> autocompleteRows = new List<TextMeshProUGUI>();


        private List<string> currentSuggestions = new List<string>();
        private int autocompleteIndex = -1;

        // -- state --
        private readonly List<(string text, FeedbackFlavor flavor)> logEntries = new List<(string, FeedbackFlavor)>();
        private readonly List<string> commandHistory = new List<string>();
        private int historyCursor = -1;
        private bool isOpen;
        private bool isAnimating;
        private Coroutine slideRoutine;
        private int caretPosition;

        public bool IsOpen => isAnimating || isOpen;

        private void Start()
        {
            SetFontSize(fontSize);

            DevConsole.CommandFeedback += RecievedFeedback;

            AppendLogLine("UberYagiz Console - \"help\" for list of commands.", FeedbackFlavor.Misc);

            SetAutocompleteList(DevConsole.CommandArray);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DevConsole.CommandFeedback -= RecievedFeedback;
        }

        private void Update()
        {
            var kb = Keyboard.current;

            if (kb.backquoteKey.wasPressedThisFrame)
            {
                Toggle();
                return;
            }

            if (!isOpen) 
                return;

            if (kb.downArrowKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            {
                inputField.caretPosition = caretPosition;
            }
            else
            {
                caretPosition = inputField.caretPosition;
            }

            if (autocompleteRoot != null && autocompleteRoot.gameObject.activeSelf)
            {
                if (kb.upArrowKey.wasPressedThisFrame) { MoveAutocomplete(-1); return; }
                if (kb.downArrowKey.wasPressedThisFrame) { MoveAutocomplete(1); return; }
                if (kb.tabKey.wasPressedThisFrame) { AcceptAutocomplete(); return; }
                if (kb.escapeKey.wasPressedThisFrame) { HideAutocomplete(); return; }
            }
            else
            {
                if (kb.upArrowKey.wasPressedThisFrame) { StepHistory(-1); return; }
                if (kb.downArrowKey.wasPressedThisFrame) { StepHistory(1); return; }
            }

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (autocompleteRoot != null && autocompleteRoot.gameObject.activeSelf)
                    AcceptAutocomplete();
                else
                    OnSubmit(inputField.text);
            }
        }

        // ---------------------------------------------------------------
        // Toggle / slide
        // ---------------------------------------------------------------

        private void Toggle()
        {
            if (isAnimating)
                StopCoroutine(slideRoutine);

            if (canvas == null) 
                BuildUI();

            isOpen = !isOpen;
            canvas.gameObject.SetActive(true);

            if (slideRoutine != null) 
                StopCoroutine(slideRoutine);

            slideRoutine = StartCoroutine(Slide(isOpen));

            if (isOpen)
            {
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                inputField.ActivateInputField();
            }
            else
            {
                HideAutocomplete();
                // toggleKey (backquote) can leak a char into the field right before it loses focus
                if (inputField.text.EndsWith("\""))
                    inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);

                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private string GetKeyChar(KeyCode key)
        {
            // Only backquote is handled specially; extend if you rebind to something else.
            return key == KeyCode.BackQuote ? "`" : "";
        }

        private IEnumerator Slide(bool opening)
        {
            isAnimating = true;

            float height = panelRect.rect.height;
            float from = panelRect.anchoredPosition.y;
            float to = opening ? 0f : height;

            float t = 0f;
            while (t < slideDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / slideDuration);
                //p = p * p * (3f - 2f * p); // smoothstep
                p = 1 - Mathf.Pow(1 - p, 3);
                panelRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(from, to, p));
                yield return null;
            }
            panelRect.anchoredPosition = new Vector2(0f, to);

            if (!opening)
                canvas.gameObject.SetActive(false);
                
            isAnimating = false;
        }

        // ---------------------------------------------------------------
        // Feedback -> log
        // ---------------------------------------------------------------

        private void RecievedFeedback(string message, FeedbackFlavor flavor)
        {
            logEntries.Add((message, flavor));

            if (logEntries.Count > MAX_LOG_ENTRIES) 
                logEntries.RemoveAt(0);

            AppendLogLine(message, flavor);
        }

        private void AppendLogLine(string message, FeedbackFlavor flavor)
        {
            if (logText == null) return; // UI not built yet, entry is still stored above and shown once it is

            string hex;
            switch (flavor)
            {
                case FeedbackFlavor.Warning: hex = "#FFD100"; break;
                case FeedbackFlavor.Error: hex = "#FF4C4C"; break;
                case FeedbackFlavor.Misc: hex = "#1980ff"; break;
                case FeedbackFlavor.Command: hex = "#4C9CFF"; break;
                case FeedbackFlavor.Info:
                case FeedbackFlavor.Feedback:
                    default: hex = "#FFFFFF"; break; // Feedback, Info
            }

            logText.text += $"<color={hex}>{message}</color>\n";
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f; // pin to bottom
        }

        private void RebuildLogText()
        {
            if (logText == null) return;
            logText.text = "";

            foreach (var e in logEntries)
                AppendLogLine(e.text, e.flavor);
        }

        private void ClearLogs()
        {
            logText.text = "";
            logEntries.Clear();
        }

        // ---------------------------------------------------------------
        // Command history (up/down)
        // ---------------------------------------------------------------

        private void OnSubmit(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                inputField.text = "";
                inputField.ActivateInputField();
                return;
            }

            AppendLogLine($"> {text}", FeedbackFlavor.Command);

            commandHistory.Add(text);

            if (commandHistory.Count > MAX_COMMMAND_HISTORY) 
                commandHistory.RemoveAt(0);

            historyCursor = commandHistory.Count;

            DevConsole.Execute(text);

            inputField.text = "";
            HideAutocomplete();
            inputField.ActivateInputField();
        }

        private void StepHistory(int dir)
        {
            if (commandHistory.Count == 0) return;

            historyCursor = Mathf.Clamp(historyCursor + dir, 0, commandHistory.Count - 1);
            inputField.text = commandHistory[historyCursor];
            inputField.caretPosition = inputField.text.Length;
        }

        // ---------------------------------------------------------------
        // Autocomplete list - swap freely at runtime
        // ---------------------------------------------------------------

        string[] emptyCommandList = new string[] { } ;
        public void SetAutocompleteList(string[] commands) {
            if (commands == null)
                commands = emptyCommandList;

            autocompleteCommands = commands;
        }

        void UpdateAutocompleteListFromText(string text)
        {
            string[] lst = DevConsole.GetAutocompleteList(text);
            SetAutocompleteList(lst);
        }

        void ShowSuggestions(string beginning, int location)
        {
            currentSuggestions = autocompleteCommands
                .Where(c => c.StartsWith(beginning, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Length)
                .Take(maxAutocompleteResults)
                .ToList();

            if (currentSuggestions.Count == 0)
            {
                HideAutocomplete();
                return;
            }

            ShowAutocomplete(location);
        }

        public void OnInputChanged(string text)
        {
            int caret = inputField.caretPosition;
            int wordStart = text.LastIndexOf(' ', Mathf.Max(0, caret - 1)) + 1;
            wordStart = Mathf.Clamp(wordStart, 0, text.Length);
            string word = text.Substring(wordStart, Mathf.Clamp(caret - wordStart, 0, text.Length - wordStart));

            /*if (string.IsNullOrEmpty(word))
            {
                HideAutocomplete();
                return;
            }*/

            UpdateAutocompleteListFromText(text);
            ShowSuggestions(word, wordStart);
        }

        private void ShowAutocomplete(int wordStartIndex)
        {
            autocompleteIndex = 0;

            for (int i = 0; i < autocompleteRows.Count; i++)
                autocompleteRows[i].gameObject.SetActive(i < currentSuggestions.Count);

            for (int i = 0; i < currentSuggestions.Count; i++)
            {
                autocompleteRows[i].text = currentSuggestions[i];
                autocompleteRows[i].color = i == autocompleteIndex ? Color.yellow : Color.white;
            }

            // Position the popup horizontally over the word being typed, just above the input field.
            float wordX = GetTextWidth(inputField.text.Substring(0, wordStartIndex));

            autocompleteRoot.anchoredPosition = new Vector2(
                inputRect.anchoredPosition.x + wordX + 8f,
                inputRect.anchoredPosition.y + inputRect.rect.height);
            autocompleteRoot.gameObject.SetActive(true);
        }

        private void HideAutocomplete()
        {
            if (autocompleteRoot != null) autocompleteRoot.gameObject.SetActive(false);
            currentSuggestions.Clear();
            autocompleteIndex = -1;
        }

        private void MoveAutocomplete(int dir)
        {
            if (currentSuggestions.Count == 0) return;
            autocompleteIndex = (autocompleteIndex + dir + currentSuggestions.Count) % currentSuggestions.Count;
            for (int i = 0; i < currentSuggestions.Count; i++)
                autocompleteRows[i].color = i == autocompleteIndex ? Color.yellow : Color.white;
        }

        private void AcceptAutocomplete()
        {
            if (autocompleteIndex < 0 || autocompleteIndex >= currentSuggestions.Count)
                return;

            string text = inputField.text;
            int caret = inputField.caretPosition;
            int wordStart = text.LastIndexOf(' ', Mathf.Max(0, caret - 1)) + 1;

            //wordStart = inputField.text.Length;

            string chosen = currentSuggestions[autocompleteIndex];
            string newText = text.Substring(0, wordStart) + chosen + " " + text.Substring(caret);

            inputField.text = newText;
            inputField.caretPosition = wordStart + chosen.Length + 1;

            HideAutocomplete();
            inputField.ActivateInputField();

            //OnInputChanged(chosen);
            //ShowAutocomplete(wordStart);

            UpdateAutocompleteListFromText(newText);
            ShowSuggestions("", newText.Length);

            //gameObject.SetTimeout(1f, () => ShowAutocomplete(wordStart));
        }

        private float GetTextWidth(string s)
        {
            return inputField.textComponent.textBounds.size.x;
            return s.Length * fontSize;
            return 0;
            /*if (string.IsNullOrEmpty(s)) 
                return 0f;
                
            var settings = inputField.textComponent.GetGenerationSettings(inputField.textComponent.rectTransform.rect.size);
            settings.scaleFactor = 1f;
            var gen = new TextGenerator();
            return gen.GetPreferredWidth(s, settings);*/
        }
        public void SetFontSize(int size)
        {
            fontSize = size;

            for (int i = 0; i < autocompleteRows.Count; i++)
            {
                autocompleteRows[i].fontSize = size;
            }

            inputField.pointSize = size;
            RectTransform rect = inputField.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, size * 2);
            logText.fontSize = size;
        }


        [YCommand("fontsize", "Change the font size used in this console. Leave empty to check the current value.")]
        [YCInt("font_size")]
        public static void SetFontSizeCmd(int val = 0)
        {
            if (val <= 0)
            {
                DevConsole.Feedback($"Font size: {Instance.fontSize}");
                return;
            }

            Instance.SetFontSize(val);
            DevConsole.Feedback($"Set font size to {Instance.fontSize}");
        }

        [YCommand("clear", "Clear console")]
        public static void Clear()
        {
            Instance.ClearLogs();
        }

        // ---------------------------------------------------------------
        // UI construction - built once, on first toggle
        // ---------------------------------------------------------------

        [ContextMenu("Build UI")]
        private void BuildUI()
        {
            return;
            /*GameObject canvasGO = new GameObject("DevConsoleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.transform.SetParent(transform, false);

            // Panel covers the top half of the screen, starts slid off above it.
            GameObject panelGO = new GameObject("Panel", typeof(Image));
            panelRect = panelGO.GetComponent<RectTransform>();
            panelGO.transform.SetParent(canvasGO.transform, false);
            panelRect.anchorMin = new Vector2(0f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelGO.GetComponent<Image>().color = backgroundColor;
            panelRect.anchoredPosition = new Vector2(0f, panelRect.rect.height > 0f ? panelRect.rect.height : Screen.height * 0.5f);

            // Scrollable log.
            GameObject scrollGO = new GameObject("LogScroll", typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(panelGO.transform, false);
            RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(8f, 40f); // room for input field
            scrollRT.offsetMax = new Vector2(-8f, -8f);
            scrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f); // needed for it to receive scroll input
            scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject viewportGO = new GameObject("Viewport", typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewportGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGO = new GameObject("Content", typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
            contentGO.transform.SetParent(viewportGO.transform, false);
            contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;

            GameObject textGO = new GameObject("LogText", typeof(Text), typeof(ContentSizeFitter));
            textGO.transform.SetParent(contentGO.transform, false);
            logText = textGO.GetComponent<Text>();
            logText.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            logText.fontSize = fontSize;
            logText.supportRichText = true;
            logText.color = Color.white;
            logText.alignment = TextAnchor.LowerLeft;
            logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            logText.verticalOverflow = VerticalWrapMode.Overflow;
            textGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRect;
            scrollRect.vertical = true;

            // Input field, pinned to the bottom of the panel.
            GameObject inputGO = new GameObject("Input", typeof(Image), typeof(InputField));
            inputGO.transform.SetParent(panelGO.transform, false);
            inputRect = inputGO.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0f);
            inputRect.anchorMax = new Vector2(1f, 0f);
            inputRect.pivot = new Vector2(0f, 0f);
            inputRect.sizeDelta = new Vector2(0f, 32f);
            inputRect.anchoredPosition = new Vector2(8f, 4f);
            inputGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            GameObject inputTextGO = new GameObject("Text", typeof(Text));
            inputTextGO.transform.SetParent(inputGO.transform, false);
            RectTransform inputTextRT = inputTextGO.GetComponent<RectTransform>();
            inputTextRT.anchorMin = Vector2.zero;
            inputTextRT.anchorMax = Vector2.one;
            inputTextRT.offsetMin = new Vector2(6f, 2f);
            inputTextRT.offsetMax = new Vector2(-6f, -2f);
            Text inputText = inputTextGO.GetComponent<Text>();
            inputText.font = logText.font;
            inputText.fontSize = fontSize;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;

            GameObject placeholderGO = new GameObject("Placeholder", typeof(Text));
            placeholderGO.transform.SetParent(inputGO.transform, false);
            RectTransform placeholderRT = placeholderGO.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = new Vector2(6f, 2f);
            placeholderRT.offsetMax = new Vector2(-6f, -2f);
            Text placeholderText = placeholderGO.GetComponent<Text>();
            placeholderText.font = logText.font;
            placeholderText.fontSize = fontSize;
            placeholderText.color = new Color(1f, 1f, 1f, 0.4f);
            placeholderText.text = "enter command...";

            inputField = inputGO.GetComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.onValueChanged.AddListener(OnInputChanged);

            // Autocomplete popup - repositioned per keystroke over the word being typed.
            GameObject acRootGO = new GameObject("Autocomplete", typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            acRootGO.transform.SetParent(panelGO.transform, false);
            autocompleteRoot = acRootGO.GetComponent<RectTransform>();
            autocompleteRoot.anchorMin = new Vector2(0f, 0f);
            autocompleteRoot.anchorMax = new Vector2(0f, 0f);
            autocompleteRoot.pivot = new Vector2(0f, 0f);
            acRootGO.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            var acLayout = acRootGO.GetComponent<VerticalLayoutGroup>();
            acLayout.childForceExpandWidth = false;
            acLayout.childControlWidth = true;
            acLayout.padding = new RectOffset(4, 4, 4, 4);
            var acFitter = acRootGO.GetComponent<ContentSizeFitter>();
            acFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            acFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < maxAutocompleteResults; i++)
            {
                GameObject rowGO = new GameObject($"Row{i}", typeof(Text), typeof(LayoutElement));
                rowGO.transform.SetParent(acRootGO.transform, false);
                Text rowText = rowGO.GetComponent<Text>();
                rowText.font = logText.font;
                rowText.fontSize = fontSize;
                rowText.color = Color.white;
                rowGO.GetComponent<LayoutElement>().minWidth = 120f;
                autocompleteRows.Add(rowText);
            }
            autocompleteRoot.gameObject.SetActive(false);

            // Flush anything received before the UI was built.
            RebuildLogText();*/
        }
    }
}