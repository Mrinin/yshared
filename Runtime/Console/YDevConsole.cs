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

        private void Update()
        {
            if (Keyboard.current.backquoteKey.wasPressedThisFrame && Togglable)
            {
                visible = !visible;
                historyScroll = 0;   
            }

            if (visible)
            {
                UpDownArrowKey();
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

            // History
            scrollPosition = GUI.BeginScrollView(
                new Rect(10, 10, Screen.width - 20, height - 55),
                scrollPosition,
                new Rect(0, 0, Screen.width - 40, history.Count * 25)
            );

            for (int i = 0; i < history.Count; i++)
            {
                GUI.Label(
                    new Rect(0, i * 25, Screen.width - 40, 25),
                    history[i],
                    textStyle
                );
            }

            GUI.EndScrollView();

            // Current input
            GUI.SetNextControlName("ConsoleInput");

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

            // Submit
            if (
                Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame)
            {
                Submit();
                Event.current.Use();
            }

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

            if (!success)
                history.Add("Invalid command.");

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