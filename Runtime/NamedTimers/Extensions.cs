using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace YShared.NamedTimers
{
    public static class Extension
    {
        public class TimerBehaviour : MonoBehaviour
        {
            void Update() => Update2();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        [Preserve]
        public static void OnInitializeLoad()
        {
            TimerBehaviour thc = new GameObject("Timer Handler").AddComponent<TimerBehaviour>();
            GameObject.DontDestroyOnLoad(thc.gameObject);
        }

        public static void Update2()
        {
            TimerHandler.Tick();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long iCombine(GameObject go, int id)
        {
            return ((long)(uint)go.GetInstanceID() << 32) | (uint)id;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int iGetId(long id)
        {
            return (int)(id & 0xFFFFFFFF);
        }

        // Shortcuts for binding timers to specific gameobjects

        public static int ClearFromGameObject(this GameObject go)
        {
            return TimerHandler.ClearFromGameObject(go);
        }

        public static List<long> GetTimers(this GameObject go)
        {
            return TimerHandler.GetTimers(go);
        }

        // int versions (used with constants)
        public static float GetTimer(this GameObject go, int name)
        {
            return TimerHandler.GetTimer(iCombine(go, name));
        }

        public static SetTimerResult SetTimer(this GameObject go, int name, float time)
        {
            return Extension.SetTimer(go, name, time, new TimerOptions());
        }

        public static SetTimerResult SetTimer(this GameObject go, int name, float time, Action callback)
        {
            return Extension.SetTimer(go, name, time, new TimerOptions() { Callback = callback });
        }

        public static SetTimerResult SetTimer(this GameObject go, int name, float time, TimerOptions opts)
        {
            long l = iCombine(go, name);
            SetTimerResult res = TimerHandler.SetTimer(l, time, opts);

            if (!opts.binding.noAutomaticBind)
                TimerHandler.BindObjectToTimer(go, l);
            return res;
        }


        public static void AddNamelessTimer(this GameObject go, float a, Action callback)
        {
            TimerHandler.AddNamelessTimer(a, callback);
        }

        public static bool ClearTimer(this GameObject go, int name)
        {
            return TimerHandler.ClearTimer(iCombine(go, name));
        }


        public static bool TriggerChronometer(this GameObject go, int name, float target)
        {
            return TimerHandler.TriggerChronometer(iCombine(go, name), target);
        }


        #region String Versions
        public static float GetTimer(this GameObject go, string name)
        {
            return go.GetTimer(Hash(name));
        }

        public static SetTimerResult SetTimer(this GameObject go, string name, float time)
        {
            return go.SetTimer(Hash(name), time, new TimerOptions() { stringName = name });
        }

        public static SetTimerResult SetTimer(this GameObject go, string name, float a, Action callback)
        {
            return go.SetTimer(Hash(name), a, new TimerOptions() { stringName = name, Callback = callback });
        }

        public static SetTimerResult SetTimer(this GameObject go, string name, float a, TimerOptions opts)
        {
            opts.stringName = name;
            return go.SetTimer(Hash(name), a, opts);
        }

        public static bool ClearTimer(this GameObject go, string name)
        {
            return go.ClearTimer(Hash(name));
        }

        public static bool TriggerChronometer(this GameObject go, string name, float target)
        {
            return go.TriggerChronometer(Hash(name), target);
        }

        
        public static Timer GetTimerClass(this GameObject go, string name)
        {
            return TimerHandler.GetTimerClass(name);
        }


        public static void RegisterTimer(this GameObject go, string name, TimerOptions opts)
        {
            opts.stringName = name;
            TimerHandler.RegisterTimer(name, iCombine(go, Hash(name)), opts);
        }

        #endregion
        
        public static int Hash(string str)
        {
            unchecked
            {
                const int fnvPrime = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < str.Length; i++)
                    hash = (hash ^ str[i]) * fnvPrime;

                return hash;
            }
        }
    }
}