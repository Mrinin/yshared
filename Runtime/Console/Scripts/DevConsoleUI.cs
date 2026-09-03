using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
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
        [SerializeField] private int autocompleteAmount = 7;

        private const int MAX_LOG_ENTRIES = 100;
        private const int MAX_COMMMAND_HISTORY = 10;

        // -- runtime built UI --
        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform panelRect;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] TextMeshProUGUI logText;
        [SerializeField] TMP_InputField inputField;
        [SerializeField] RectTransform inputRect;
        [SerializeField] RectTransform autocompleteRoot;
        [SerializeField] TextMeshProUGUI autocompleteRow;
        TextMeshProUGUI autocompleteTopRow;
        TextMeshProUGUI autocompleteBottomRow;
        List<TextMeshProUGUI> autocompleteRows = new List<TextMeshProUGUI>();


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
            CreateAutocomplete();
            SetFontSize(fontSize);

            DevConsole.CommandFeedback += RecievedFeedback;
            DevConsole.CommandLog += RecievedLog;
            Application.logMessageReceived += HandleUnityLog;

#if UNITY_EDITOR
            ShowErrors = UnityEditor.EditorPrefs.GetBool($"yconsole_{nameof(ShowErrors)}", true);
            ShowWarnings = UnityEditor.EditorPrefs.GetBool($"yconsole_{nameof(ShowWarnings)}", false);
            ShowInfo = UnityEditor.EditorPrefs.GetBool($"yconsole_{nameof(ShowInfo)}", true);
#endif

            AppendLogLine("UberYagiz++ Console - \"help\" for list of commands.", FeedbackFlavor.Misc);

            //SetAutocompleteList(CommandRegistry.RootCommandArray);
            UpdateAutocompleteListFromText("");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            DevConsole.CommandFeedback -= RecievedFeedback;
            DevConsole.CommandLog -= RecievedLog;
            Application.logMessageReceived -= HandleUnityLog;

#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool($"yconsole_{nameof(ShowErrors)}", ShowErrors);
            UnityEditor.EditorPrefs.SetBool($"yconsole_{nameof(ShowWarnings)}", ShowWarnings);
            UnityEditor.EditorPrefs.SetBool($"yconsole_{nameof(ShowInfo)}", ShowInfo);
#endif
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

            if (kb.downArrowKey.isPressed || kb.upArrowKey.isPressed)
            {
                inputField.caretPosition = caretPosition;
            }
            else
            {
                caretPosition = inputField.caretPosition;
            }

            if (autocompleteRoot != null && autocompleteRoot.gameObject.activeSelf)
            {
                if (IsUpArrow()) { MoveAutocomplete(-1); return; }
                if (IsDownArrow()) { MoveAutocomplete(1); return; }

                if (IsForwardTab()) { AcceptAutocomplete(); return; }
                if (kb.escapeKey.wasPressedThisFrame) { HideAutocomplete(); return; }
            }
            else
            {
                if (IsUpArrow()) { StepHistory(-1); return; }
                if (IsDownArrow()) { StepHistory(1); return; }
            }

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                OnSubmit(inputField.text);
            }

            if (IsBackwardTab())
            {
                DeleteCtrlBacksapce();
            }
        }

        // ---------------------------------------------------------------
        // Toggle / slide
        // ---------------------------------------------------------------

        private void Toggle()
        {
            if (isAnimating)
                StopCoroutine(slideRoutine);

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
            if (!isOpen)
                return;

            AppendLogLine(message, flavor);
        }

        private void RecievedLog(string message, FeedbackFlavor flavor)
        {
            AppendLogLine(message, flavor);
        }

        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            FeedbackFlavor flavor = FeedbackFlavor.Info;
            switch (type)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    flavor = FeedbackFlavor.Error;
                    break;
                case LogType.Warning:
                    flavor = FeedbackFlavor.Warning;
                    break;
                case LogType.Log:
                    flavor = FeedbackFlavor.Info;
                    break;
            }

            AppendLogLine(condition, flavor);
        }

        private void AppendLogLine(string message, FeedbackFlavor flavor)
        {
            logEntries.Add((message, flavor));

            if (logEntries.Count > MAX_LOG_ENTRIES) 
                logEntries.RemoveAt(0);

            if (logText == null) return; // UI not built yet, entry is still stored above and shown once it is

            string hex;
            switch (flavor)
            {
                case FeedbackFlavor.Warning: hex = "#FFD100"; break;
                case FeedbackFlavor.Error: hex = "#FF4C4C"; break;
                case FeedbackFlavor.Misc: hex = "#1980ff"; break;
                case FeedbackFlavor.Return: hex = "#8cbccf"; break;
                case FeedbackFlavor.Command: hex = "#4C9CFF"; break;
                case FeedbackFlavor.Info: hex = "#adadad"; break;
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
            caretPosition = inputField.text.Length;
        }

        // ---------------------------------------------------------------
        // Autocomplete list - swap freely at runtime
        // ---------------------------------------------------------------

        string[] emptyCommandList = new string[] { } ;
        public void SetAutocompleteList(string[] commands) 
        {
            commands ??= emptyCommandList;

            autocompleteCommands = commands;
        }

        void UpdateAutocompleteListFromText(string text)
        {
            string[] lst = Autocomplete.GetAutocompleteList(text);
            SetAutocompleteList(lst);
        }

        void ShowSuggestions(string beginning, int location)
        {
            currentSuggestions = autocompleteCommands
                .Where(c => c.Contains(beginning, StringComparison.OrdinalIgnoreCase))
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

            
            for (int i = 0; i < autocompleteAmount; i++)
            {
                autocompleteRows[i].text = currentSuggestions[Mathf.Clamp(i, 0, currentSuggestions.Count - 1)];
                autocompleteRows[i].color = i == autocompleteIndex ? Color.yellow : Color.white;
            }

            bool allVisibleAtOnce = currentSuggestions.Count <= autocompleteAmount;
            autocompleteBottomRow.gameObject.SetActive(!allVisibleAtOnce);
            autocompleteTopRow.gameObject.SetActive(!allVisibleAtOnce);

            int largest_width = currentSuggestions.Max(str => str.Length);
            autocompleteRoot.sizeDelta = new Vector2(largest_width * fontSize, autocompleteRoot.sizeDelta.y);

            MoveAutocomplete(0);

            // Position the popup horizontally over the word being typed, just above the input field.
            float wordX = GetTextWidth(inputField.text.Substring(0, wordStartIndex));

            autocompleteRoot.anchoredPosition = new Vector2(
                inputRect.anchoredPosition.x + wordX + 8f,
                inputRect.anchoredPosition.y + inputRect.rect.height);
            autocompleteRoot.gameObject.SetActive(true);
        }

        private void HideAutocomplete()
        {
            if (autocompleteRoot != null) 
                autocompleteRoot.gameObject.SetActive(false);

            currentSuggestions.Clear();
            autocompleteIndex = -1;
        }

        private int selectedSuggestionIndex = 0;        // Index in currentSuggestions
        private int autocompleteScrollOffset = 0;       // First visible suggestion

        private void MoveAutocomplete(int dir)
        {
            if (currentSuggestions.Count == 0)
                return;

            int count = currentSuggestions.Count;
            int visibleCount = Mathf.Min(autocompleteAmount, count);
            int maxScroll = count - visibleCount; // the index at which the highest scroll amount is reached
            bool allVisibleAtOnce = maxScroll == 0;

            // Move through the actual suggestion list.
            selectedSuggestionIndex = selectedSuggestionIndex + dir;

            if (selectedSuggestionIndex < 0)
            {
                selectedSuggestionIndex = count - 1;
                autocompleteScrollOffset = maxScroll;
            }

            if (selectedSuggestionIndex >= count)
            {
                selectedSuggestionIndex = 0;
                autocompleteScrollOffset = 0;
            }

            int halfway = visibleCount / 2;

            // Move selection relative to the visible window.
            int relativeIndex = selectedSuggestionIndex - autocompleteScrollOffset;

            // Moving down past the halfway point.
            if (dir > 0 && (relativeIndex - 0.5f) >= halfway)
            {
                if (autocompleteScrollOffset < maxScroll)
                    autocompleteScrollOffset++;
            }

            // Moving up past the halfway point.
            else if (dir < 0 && (relativeIndex + 0.5f) <= halfway)
            {
                if (autocompleteScrollOffset > 0)
                    autocompleteScrollOffset--;
            }

            // Calculate which visible row the selected suggestion occupies.
            autocompleteIndex = selectedSuggestionIndex - autocompleteScrollOffset;

            // Update the visible rows.
            for (int i = 0; i < visibleCount; i++)
            {
                int suggestionIndex = autocompleteScrollOffset + i;

                autocompleteRows[i].text = currentSuggestions[suggestionIndex];

                autocompleteRows[i].color = i == autocompleteIndex ? Color.yellow : Color.white;
            }

            // Hide unused rows.
            for (int i = visibleCount; i < autocompleteAmount; i++)
            {
                autocompleteRows[i].text = "";
            }

            if (allVisibleAtOnce)
            {
                autocompleteTopRow.text = "";
                autocompleteBottomRow.text = "";
            }
            else
            {
                if (autocompleteScrollOffset == 0)
                    autocompleteTopRow.text = $" - ";
                else
                    autocompleteTopRow.text = $" ^ {autocompleteScrollOffset}";

                if (autocompleteScrollOffset == maxScroll)
                    autocompleteBottomRow.text = $" - ";
                else
                    autocompleteBottomRow.text = $" v {maxScroll - autocompleteScrollOffset}";
            }
        }

        private void AcceptAutocomplete()
        {
            if (selectedSuggestionIndex < 0 || selectedSuggestionIndex >= currentSuggestions.Count)
                return;

            string text = inputField.text;
            int caret = inputField.caretPosition;
            int wordStart = text.LastIndexOf(' ', Mathf.Max(0, caret - 1)) + 1;

            //wordStart = inputField.text.Length;

            string chosen = currentSuggestions[selectedSuggestionIndex];
            string newText = text.Substring(0, wordStart) + chosen + " " + text.Substring(caret);

            inputField.text = newText;
            inputField.caretPosition = wordStart + chosen.Length + 1;

            inputField.ActivateInputField();

            UpdateAutocompleteListFromText(newText);
            ShowSuggestions("", newText.Length);

            //gameObject.SetTimeout(1f, () => ShowAutocomplete(wordStart));
        }

        private float GetTextWidth(string s)
        {
            //LayoutRebuilder.ForceRebuildLayoutImmediate(inputField.textComponent.rectTransform);
            return inputField.textComponent.GetPreferredValues().x;
        }
        public void SetFontSize(int size)
        {
            fontSize = size;

            for (int i = 0; i < autocompleteRows.Count; i++)
            {
                autocompleteRows[i].fontSize = size;
            }

            autocompleteBottomRow.fontSize = size;
            autocompleteTopRow.fontSize = size;

            inputField.pointSize = size;
            RectTransform rect = inputField.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, size * 2);
            logText.fontSize = size;
        }

        private void DeleteCtrlBacksapce()
        {
            string text = inputField.text;
            int caret = inputField.caretPosition;
            int newCaretPosition = caret;

            while (newCaretPosition > 0 && !char.IsWhiteSpace(text[newCaretPosition - 1]))
                newCaretPosition--;

            string newText = text.Remove(newCaretPosition, caret - newCaretPosition);

            inputField.text = newText;
            inputField.caretPosition = newCaretPosition;

            inputField.ActivateInputField();

            UpdateAutocompleteListFromText(newText);
            ShowSuggestions("", newText.Length);

            //gameObject.SetTimeout(1f, () => ShowAutocomplete(wordStart));
        }
        
        /// COMMANDS
        /// /// COMMANDS
        /// /// COMMANDS


        [YCommand("yconsole fontsize", "Change the font size used in this console. Leave empty to check the current value.")]
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

        
        [YCommand("yconsole unity_console show_errors", "Toggle whether the Unity Console errors should be showed in the YConsole or not.")] [YToggle] 
        public bool ShowErrors;
        [YCommand("yconsole unity_console show_warnings", "Toggle whether the Unity Console warnings should be showed in the YConsole or not.")] [YToggle] 
        public bool ShowWarnings;
        [YCommand("yconsole unity_console show_info", "Toggle whether the Unity Console info logs should be the YConsole or not.")] [YToggle] 
        public bool ShowInfo;

        // Delayed Auto Key

        float downArrowKeyLastPress;
        public bool IsDownArrow()
        {
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                downArrowKeyLastPress = Time.time + 0.5f;
                return true;
            }

            if (downArrowKeyLastPress < Time.time && Keyboard.current.downArrowKey.isPressed)
            {
                downArrowKeyLastPress = Time.time + 0.05f;
                return true;
            }

            return false;
        }

        float upArrowKeyLastPress;
        public bool IsUpArrow()
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                upArrowKeyLastPress = Time.time + 0.5f;
                return true;
            }

            if (upArrowKeyLastPress < Time.time && Keyboard.current.upArrowKey.isPressed)
            {
                upArrowKeyLastPress = Time.time + 0.05f;
                return true;
            }

            return false;
        }

        bool IsForwardTab()
        {
            if (!Keyboard.current.leftShiftKey.isPressed && Keyboard.current.tabKey.wasPressedThisFrame)
                return true;

            return false;
        }

        bool IsBackwardTab()
        {
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.backspaceKey.wasPressedThisFrame)
                return true;

            return false;
        }

        // Create Autocomplete Files
        public void CreateAutocomplete()
        {
            autocompleteTopRow = GameObject.Instantiate(autocompleteRow, autocompleteRoot);
            autocompleteTopRow.text = "";

            for (int i = 0; i < autocompleteAmount; i++)
            {
                TextMeshProUGUI txt = GameObject.Instantiate(autocompleteRow, autocompleteRoot);
                txt.text = "";
                autocompleteRows.Add(txt);
            }

            autocompleteBottomRow = GameObject.Instantiate(autocompleteRow, autocompleteRoot);
            autocompleteBottomRow.text = "";

            autocompleteRow.gameObject.SetActive(false);
        }
    }
}