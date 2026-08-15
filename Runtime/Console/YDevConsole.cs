using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using YShared.Console;
using YShared.Singleton;

namespace YShared.Console
{
    public class YDevConsole : Singleton<YDevConsole>
    {
        public override bool CallDontDestroyOnLoad => true;
        [SerializeField] private Font font;
        public bool Togglable = true;
        public bool visible = false;

        private string input = "";

        private readonly List<string> history = new();
        private readonly List<string> inputHistory = new();
        int historyScroll = 0;

        private Vector2 scrollPosition;

        private GUIStyle textStyle;
        private GUIStyle inputStyle;

        void Start()
        {
            DevConsole.CommandFeedback += RecievedFeedback;
        }

        int dontRegisterbackquoteKey = 0;
        private void Update()
        {
            if (Keyboard.current.backquoteKey.wasPressedThisFrame && Togglable)
            {
                visible = !visible;
                historyScroll = 0;
                dontRegisterbackquoteKey = 4;
            }

            if (visible)
            {
                UpDownArrowKey();

                // Submit
                if (
                    Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    Submit();
                }

            }
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            InitializeStyles();

            float height = Screen.height / 2f;

            // Console background
            GUI.Box(
                new Rect(0, 0, Screen.width, height),
                ""
            );

            float contentWidth = Screen.width - 40;
            float y = 0f;

            // Calculate total content height first.
            float contentHeight = 0f;

            for (int i = 0; i < history.Count; i++)
            {
                float textHeight = textStyle.CalcHeight(
                    new GUIContent(history[i]),
                    contentWidth
                );

                contentHeight += Mathf.Max(25f, textHeight);
            }

            // History
            scrollPosition = GUI.BeginScrollView(
                new Rect(10, 10, Screen.width - 20, height - 55),
                scrollPosition,
                new Rect(0, 0, contentWidth, contentHeight)
            );

            // Draw history
            for (int i = 0; i < history.Count; i++)
            {
                float textHeight = Mathf.Max(
                    25f,
                    textStyle.CalcHeight(
                        new GUIContent(history[i]),
                        contentWidth
                    )
                );

                GUI.TextArea(
                    new Rect(0, y, contentWidth, textHeight),
                    history[i],
                    textStyle
                );

                y += textHeight;
            }

            GUI.EndScrollView();

            // Current input
            GUI.SetNextControlName("ConsoleInput");

            if (dontRegisterbackquoteKey > 0)
            {
                if (input.Length > 0 && input[input.Length - 1] == '"')
                {
                    input = input.Substring(0, input.Length - 1);
                }
                dontRegisterbackquoteKey -= 1;
            }


            input = GUI.TextField(
                new Rect(
                    10,
                    height - 40,
                    Screen.width - 20,
                    30
                ),
                input,
                inputStyle
            );
    
            GUI.FocusControl("ConsoleInput");
        }

        void UpDownArrowKey()
        {
            int oldHistoryScroll = historyScroll;
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                historyScroll += 1;
            }
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                historyScroll -= 1;
            }
            int newHistoryScroll = Mathf.Clamp(historyScroll, -1, inputHistory.Count - 1);

            if (oldHistoryScroll != newHistoryScroll && inputHistory.Count != 0 && newHistoryScroll != -1)
            {
                historyScroll = newHistoryScroll;
                input = inputHistory[newHistoryScroll];
            }
        }

        private void Submit()
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            history.Add("> " + input);

            inputHistory.Insert(0, input);
            if (inputHistory.Count > 100)
                inputHistory.RemoveAt(inputHistory.Count - 1);

            bool success = DevConsole.Execute(input);

            input = "";
            historyScroll = -1;

            // Scroll to bottom
            scrollPosition.y = float.MaxValue;
        }

        private void InitializeStyles()
        {
            if (textStyle != null)
                return;

            textStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 16,
                alignment = TextAnchor.UpperLeft
            };

            inputStyle = new GUIStyle(GUI.skin.textField)
            {
                font = font,
                fontSize = 16
            };
        }

        private void RecievedFeedback(string text)
        {
            history.Add(text);
        }

        [YCommand("clear", "Clear console")]
        static void Clear()
        {
            FindFirstObjectByType<YDevConsole>()?.history.Clear();
        }
    }
}