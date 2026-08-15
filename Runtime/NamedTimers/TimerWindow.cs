#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace YShared.NamedTimers
{

    public class TimerWindow : EditorWindow
    {
        private List<Timer> timers = new List<Timer>();

        [MenuItem("Window/Timer Viewer")]
        public static void ShowWindow()
        {
            GetWindow<TimerWindow>("Timer Viewer");
        }

        private void OnEnable()
        {
 
        }

        void Update()
        {
            Repaint();
        }


        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("Active timers will appear here in play mode.");
                return;
            }

            timers = TimerHandler.GetTimerClasses();

            EditorGUILayout.LabelField($"Timers {timers.Count}", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.Width(150));
            GUILayout.Label("Time Left", GUILayout.Width(70));
            GUILayout.Label("Duration", GUILayout.Width(70));
            GUILayout.Label("Owner", GUILayout.Width(80));
            GUILayout.Label("Preserve", GUILayout.Width(60));
            GUILayout.Label("Loop Inf", GUILayout.Width(60));
            GUILayout.Label("Loops", GUILayout.Width(50));
            GUILayout.Label("Unscaled", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            foreach (var timer in timers)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(timer.state.opts.stringName.ToString(), GUILayout.Width(150));
                GUILayout.Label(timer.time_left.ToString(), GUILayout.Width(70));
                GUILayout.Label(timer.duration.ToString(), GUILayout.Width(70));

                if (timer.state.binding.is_bound)
                {
                    if (timer.state.binding.bound_object)
                        GUILayout.Label(timer.state.binding.bound_object.gameObject.name, GUILayout.Width(80));
                    else
                        GUILayout.Label("Destroyed", GUILayout.Width(80));
                }
                else
                    GUILayout.Label("Global", GUILayout.Width(80));

                GUILayout.Label(timer.state.opts.preserve.ToString(), GUILayout.Width(60));
                GUILayout.Label(timer.state.opts.loopInfinitely.ToString(), GUILayout.Width(60));
                GUILayout.Label(timer.state.opts.loops.ToString(), GUILayout.Width(50));
                GUILayout.Label(timer.state.opts.runOnUnscaledTime.ToString(), GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif