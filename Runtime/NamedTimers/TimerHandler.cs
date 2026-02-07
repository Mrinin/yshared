using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace YShared.NamedTimers
{

    public struct GameObjectBinding
    {
        public bool noAutomaticBind;
        public bool is_bound;
        public GameObject bound_object;
    }

    public enum SetTimerResult
    {
        NewTimer, ChangedTimeLeft, ReplacedAction,
    }

    public struct TimerOptions
    {
        public bool loopInfinitely;
        public int loops;
        public bool runOnUnscaledTime; // runs on scaled time by default

        public bool preserve;
        public bool pause;
        public string stringName; // Only used for debugging purposes


        public bool callbackSent;
        public Action Callback;
        public Action<float, float> OnUpdate;

        public GameObjectBinding binding;
    }

    public static class TimerHandler
    {
        static Dictionary<long, Timer> Timers = new();
        static HashSet<NamelessTimer> NamelessTimers = new();
        static Dictionary<long, Chronometer> Chronometers = new();

        class NamelessTimer
        {
            public Action callback;
            public float time_left;

            public bool remove;
        }

        class Chronometer
        {
            public float value;
            public bool isCalled;
            public bool isFinished;
        }

        public static void Tick()
        {
            ManageTimers();
            ManageNamelessTimers();
            ManageChronometers();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetTimer(long name)
        {
            if (Timers.TryGetValue(name, out var t))
            {
                return t.time_left;
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SetTimerResult SetTimer(string name, float time)
        {
            return SetTimer((long)Hash(name), time, new TimerOptions() { stringName = name });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SetTimerResult SetTimer(string name, float a, Action callback)
        {
            return SetTimer((long)Hash(name), a, new TimerOptions { stringName = name, Callback = callback });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SetTimerResult SetTimer(string name, float a, TimerOptions opts)
        {
            opts.stringName = name;
            return SetTimer((long)Hash(name), a, opts);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SetTimerResult SetTimer(long name, float time)
        {
            return SetTimer(name, time, new TimerOptions());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SetTimerResult SetTimer(long name, float a, Action callback)
        {
            return SetTimer(name, a, new TimerOptions { Callback = callback });
        }

        public static SetTimerResult SetTimer(long name, float a, TimerOptions opts)
        {
            if (Timers.TryGetValue(name, out var timer))
            {
                timer.time_left = a;
                timer.opts.callbackSent = false;

                if (opts.Callback != null)
                {
                    timer.opts.Callback = opts.Callback;
                    return SetTimerResult.ReplacedAction;
                }

                return SetTimerResult.ChangedTimeLeft;
            }

            Timer t = new Timer();
            t.name = name;
            t.time_left = a;
            t.duration = a;
            t.opts = opts;

            Timers.Add(name, t);

            return SetTimerResult.NewTimer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Timer GetTimerClass(string name)
        {
            return GetTimerClass(Hash(name));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Timer GetTimerClass(long name)
        {
            if (Timers.TryGetValue(name, out var timer))
            {
                return timer;
            }
            return null;
        }

        public static void RegisterTimer(string name, TimerOptions opts = default)
        {
            int n = Hash(name);
            opts.preserve = true;
            opts.stringName = name;

            Timer t = new Timer();
            t.name = n;
            t.opts = opts;

            t.opts = opts;
            t.time_left = 0;
            t.duration = 1;
            Timers[n] = t;
        }

        public static void RegisterTimer(string name, long id, TimerOptions opts = default)
        {
            opts.preserve = true;
            opts.stringName = name;

            Timer t = new Timer();
            t.name = id;
            t.opts = opts;

            t.opts = opts;
            t.time_left = 0;
            t.duration = 1;
            Timers[id] = t;
        }

        public static void AddNamelessTimer(float a, Action callback)
        {
            NamelessTimers.Add(new NamelessTimer() { time_left = a, callback = callback, remove = false });
        }

        public static bool ClearTimer(long name)
        {
            if (Timers.TryGetValue(name, out var timer))
            {
                Timers.Remove(name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns all timers on a specific gameobject
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<long> GetTimers(GameObject go)
        {
            return Timers.Keys.Where(key => (int)(key >> 32) == go.GetInstanceID()).ToList();
        }

        /// <summary>
        /// Returns all timers
        /// </summary>

        public static List<long> GetTimers()
        {
            return Timers.Keys.ToList();
        }

        internal static List<Timer> GetTimerClasses()
        {
            return Timers.Values.ToList();
        }

        public static int ClearFromGameObject(GameObject go)
        {
            var keysToRemove = GetTimers(go);

            foreach (var key in keysToRemove)
            {
                Timers.Remove(key);
            }

            return keysToRemove.Count;
        }

        public static void ClearAll()
        {
            Timers.Clear();
        }

        public static bool BindObjectToTimer(GameObject go, long name)
        {
            if (Timers.TryGetValue(name, out var timer))
            {
                timer.opts.binding.is_bound = true;
                timer.opts.binding.bound_object = go;
                return true;
            }
            return false;
        }

        public static bool TriggerChronometer(long name, float target)
        {
            Chronometer c;

            if (!Chronometers.TryGetValue(name, out c))
            {
                Chronometers[name] = new Chronometer();
                c = Chronometers[name];
            }

            c.isCalled = true;

            if (c.isFinished)
                return false;

            if (c.value > target)
            {
                c.value = 0;
                c.isFinished = true;
                return true;
            }

            return false;
        }

        static void ManageTimers()
        {
            HashSet<long> keys_to_clear = new();
            var arr = Timers.Keys.ToArray();

            foreach (var key in arr)
            {
                Timer timer = Timers[key];

                if (timer.opts.binding.is_bound)
                {
                    if (timer.opts.binding.bound_object == null)
                    {
                        keys_to_clear.Add(key);
                        continue;
                    }
                    if (timer.opts.binding.bound_object.activeInHierarchy == false)
                    {
                        continue;
                    }

                    if (timer.opts.pause)
                    {
                        continue;
                    }
                }

                ref float ActiveTimer = ref timer.time_left;

                if (ActiveTimer > 0)
                {
                    if (timer.opts.runOnUnscaledTime)
                    {
                        ActiveTimer -= Time.unscaledDeltaTime;
                    }
                    else
                    {
                        ActiveTimer -= Time.deltaTime;
                    }
                }

                if (timer.opts.OnUpdate != null && timer.opts.callbackSent == false)
                {
                    if (ActiveTimer < 0)
                        timer.opts.OnUpdate(0, timer.duration);
                    else
                        timer.opts.OnUpdate(timer.time_left, timer.duration);
                }

                if (ActiveTimer <= 0 && timer.opts.callbackSent == false)
                {
                    ActiveTimer = 0;
                    timer.opts.callbackSent = true;


                    if (timer.opts.Callback != null)
                        timer.opts.Callback();

                    if (timer.opts.loopInfinitely || timer.opts.loops > 0)
                    {
                        ActiveTimer = timer.duration;
                        timer.opts.loops--;
                    }
                    else
                    {
                        if (!timer.opts.preserve)
                            keys_to_clear.Add(key);
                    }
                }
            }

            foreach (long s in keys_to_clear)
            {
                Timers.Remove(s);
            }
        }

        static void ManageNamelessTimers()
        {
            foreach (NamelessTimer nt in NamelessTimers)
            {
                nt.time_left -= Time.deltaTime;

                if (nt.time_left < 0)
                {
                    nt.remove = true;

                    try
                    {
                        nt.callback();
                    } catch (Exception e)
                    {
                        Debug.LogError("Error in callback of nameless timer!");
                        Debug.Log(e);
                    }
                }
            }

            NamelessTimers.RemoveWhere(nt => nt.remove);
        }

        static void ManageChronometers()
        {
            foreach (var kvp in Chronometers)
            {
                if (kvp.Value.isCalled)
                {
                    kvp.Value.value += Time.deltaTime;
                    kvp.Value.isCalled = false;
                }
                else
                {
                    kvp.Value.value = 0;
                    kvp.Value.isFinished = false;
                }
            }
        }
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

    public class Timer
    {
        public long name;
        public float time_left;
        public float duration;

        public TimerOptions opts;
    }
}
