using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
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
        public string stringName; // Only used for debugging purposes

        public Func<Timer, bool> CallbackCondition;
        public Action Callback;
        public Action<float, float> OnUpdate;

    }

    public struct TimerState
    {
        public TimerOptions opts;
        public GameObjectBinding binding;
        public bool callbackSent;
        public bool hasConditional;

        public bool pause;

        public TimerState(TimerOptions opts)
        {
            this.opts = opts;

            callbackSent = false;
            pause = false;
            binding = default;

            if (opts.Callback == null)
                hasConditional = false;
            else
                hasConditional = true;
        }
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
            return SetTimer(name, time, new TimerOptions() { });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SetTimerResult SetTimer(long name, float a, Action callback)
        {
            return SetTimer(name, a, new TimerOptions { Callback = callback });
        }

        public static SetTimerResult SetTimer(long name, float a, TimerOptions opts)
        {
            if (string.IsNullOrEmpty(opts.stringName))
                opts.stringName = name.ToString();

            if (Timers.TryGetValue(name, out var timer))
            {
                timer.time_left = a;
                timer.state.callbackSent = false;

                if (opts.Callback != null)
                {
                    timer.state.opts.Callback = opts.Callback;
                    return SetTimerResult.ReplacedAction;
                }

                return SetTimerResult.ChangedTimeLeft;
            }

            Timer t = new Timer
            {
                name = name,
                time_left = a,
                duration = a,
                state = new TimerState(opts)
            };

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
            t.time_left = 0;
            t.duration = 1;
            t.state = new TimerState(opts);

            Timers[n] = t;
        }

        public static void RegisterTimer(string name, long id, TimerOptions opts = default)
        {
            opts.preserve = true;
            opts.stringName = name;

            Timer t = new Timer();
            t.name = id;
            t.time_left = 0;
            t.duration = 1;
            t.state = new TimerState(opts);

            Timers[id] = t;
        }

        [Obsolete("Use SetTimeout instead")]
        public static void AddNamelessTimer(float a, Action callback)
        {
            NamelessTimers.Add(new NamelessTimer() { time_left = a, callback = callback, remove = false });
        }

        public static void SetTimeout(float a, Action callback)
        {
            NamelessTimers.Add(new NamelessTimer() { time_left = a, callback = callback, remove = false });
        }

        public static void RunNextFrame(Action callback)
        {
            NamelessTimers.Add(new NamelessTimer() { time_left = 0, callback = callback, remove = false });
        }

        public static bool RemoveTimer(long name)
        {
            if (Timers.TryGetValue(name, out var timer))
            {
                Timers.Remove(name);
                return true;
            }
            return false;
        }

        public static bool TimerCooldown(long name, float timer)
        {
            float t = GetTimer(name);

            if (t > 0)
            {
                return false;
            }
            else
            {
                SetTimer(name, timer);
                return true;
            }
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
                timer.state.binding.is_bound = true;
                timer.state.binding.bound_object = go;
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
                ref TimerState state = ref timer.state;

                if (state.binding.is_bound)
                {
                    if (state.binding.bound_object == null)
                    {
                        keys_to_clear.Add(key);
                        continue;
                    }
                    if (state.binding.bound_object.activeInHierarchy == false)
                    {
                        continue;
                    }

                    if (state.pause)
                    {
                        continue;
                    }
                }

                ref float ActiveTimer = ref timer.time_left;

                if (ActiveTimer > 0)
                {
                    if (state.opts.runOnUnscaledTime)
                    {
                        ActiveTimer -= Time.unscaledDeltaTime;
                    }
                    else
                    {
                        ActiveTimer -= Time.deltaTime;
                    }
                }

                if (state.opts.OnUpdate != null && state.callbackSent == false)
                {
                    if (ActiveTimer < 0)
                        state.opts.OnUpdate(0, timer.duration);
                    else
                        state.opts.OnUpdate(timer.time_left, timer.duration);
                }

                if (ActiveTimer <= 0 && state.callbackSent == false)
                {
                    if (state.hasConditional && state.opts.CallbackCondition(timer) == false)
                    {
                        continue;
                    }

                    ActiveTimer = 0;
                    state.callbackSent = true;

                    if (state.opts.Callback != null)
                        state.opts.Callback();

                    if (state.opts.loopInfinitely || state.opts.loops > 0)
                    {
                        ActiveTimer = timer.duration;
                        state.opts.loops--;
                        state.callbackSent = true;
                    }
                    else
                    {
                        if (!state.opts.preserve)
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

        public TimerState state;
    }
}
