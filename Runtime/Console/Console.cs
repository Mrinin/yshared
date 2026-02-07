using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System;
using UnityEditor;

namespace YShared.Console
{
    public class Console : Singleton.Singleton<Console>
    {

        [SerializeField] Font FontToUse;

        public GUIStyle textStyle;
        private Rect textRect;
        private Rect backgroundRect;
        private int fontSize = 18;

        public int FPS_Limit = 0;

        bool isEnabled = true;

        string FinalText = "";

        static Console _instance;

        /*[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Auto-create the DisplayText component without attaching to a GameObject
            GameObject displayTextObject = new GameObject("ConsoleObject");
            _instance = displayTextObject.AddComponent<Console>();
            DontDestroyOnLoad(displayTextObject); 
        }*/

        private void Start()
        {
        
            // Set up text style
            textStyle = new GUIStyle
            {
                font = FontToUse,
                fontSize = fontSize,
                normal = { textColor = Color.white }
            };
        }

        public static Console instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindAnyObjectByType<Console>();

                return _instance;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                isEnabled = !isEnabled;
            }
        }

        public void OnGUI()
        {
            if (isEnabled)
            {
                Application.targetFrameRate = FPS_Limit;
                CalculateFPS();

                F3Text();

                

                // Calculate text positioning
                Vector2 textSize = textStyle.CalcSize(new GUIContent(FinalText));
                textRect = new Rect(Screen.width - textSize.x - 10, 10, textSize.x, textSize.y);
                backgroundRect = new Rect(textRect.x - 5, textRect.y - 5, textRect.width + 10, textRect.height + 10);

                // Display the box around the text
                GUI.color = new Color(0, 0, 0, 0.5f);  // 50% transparent gray
                GUI.Box(backgroundRect, GUIContent.none);

                // Draw the text
                float a = 0.95f;
                GUI.color = new Color(a, a, a, 1.0f);
                GUI.Label(textRect, FinalText, textStyle);
            }
        }

        static Text[] DebugTexts = new Text[256];

        struct Text
        {
            public string key;
            public string value;
        }

        void F3Text()
        {
            FinalText = "";
            int LongestKey = 0;
            for (int i = 0; i < DebugTexts.Length; i++)
            {
                string key = DebugTexts[i].key;
                string value = DebugTexts[i].value;
                if (key == null || key == "")
                    continue;

                if(key.Length >LongestKey)
                    LongestKey = key.Length;
            }

            for (int i = 0; i < DebugTexts.Length; i++)
            {
                string key = DebugTexts[i].key;
                string value = DebugTexts[i].value;
                if (key == null ||key == "")
                    continue;

                string space = "";
                while(space.Length + key.Length < LongestKey)
                {
                    space += " ";
                }

                FinalText += $"{space}{key.ToUpper()}: {value}\n";
            }
        }
        public static void Write(int id, string _key, object _value)
        {
            string s = _value.ToString();
            DebugTexts[id] = new Text { key = _key, value = s };
        }

        public static void WriteFloat(int id, string _key, float _float_value, int decimals = 2)
        {
            Write(id, _key, (_float_value).ToString($"F{decimals}"));
        }

        public static float RoundToXDecimals(float number, int x)
        {
            float m = Mathf.Pow(10, x);
            return Mathf.RoundToInt(number * m) / m;
        }


        static float deltaTime = 0;
        static int frameCount = 0;
        const float updateInterval = 1.0f;
        public static void CalculateFPS()
        {
            deltaTime += Time.deltaTime;
            frameCount += 1;

            if (deltaTime > updateInterval)
            {
                // Calculate frames per second
                float fps = frameCount / deltaTime;

                // Print FPS to the console
                WriteFloat(5, "FPS", fps, 4);

                // Reset the timer
                deltaTime = 0.0f;
                frameCount = 0;
            }
        }
    }
}