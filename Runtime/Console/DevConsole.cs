using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEditor;
using UnityEngine;
using YShared.Console;
using YShared.MathHelper;

namespace YShared.Console
{
    public static class DevConsole
    {
        class Command
        {
            public string command;
            public string description;
            public YCmdArgumentAttribute[] arguments;
            public MethodInfo action;
        }
        private static readonly Dictionary<string, Command> commands = new();

        public static Action<string> CommandFeedback;
        static bool feedbackActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            commands.Clear();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetTypesSafe(assembly))
                {
                    foreach (MethodInfo method in type.GetMethods(
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic))
                    {
                        var attribute = method.GetCustomAttribute<YCommandAttribute>();

                        if (attribute == null)
                            continue;

                        Command cmd = new Command();

                        var arguments = method.GetCustomAttributes<YCmdArgumentAttribute>().ToArray();

                        cmd.command = attribute.Name;
                        cmd.description = attribute.Desc;

                        cmd.arguments = arguments;
                        cmd.action = method;

                        commands[attribute.Name] = cmd;
                    }
                }
            }

            Debug.Log($"Registered {commands.Count} game commands.");
        }

        static object[] GetParameters(string[] arguments, Command cmd)
        {
            object[] result = new object[cmd.arguments.Length];

            if (arguments.Length - 1 != result.Length)
            {
                throw new DevConsoleException($"Number of arguments do not match! {cmd.arguments.Length - 1 }, {result.Length}");
            }

            for (int i = 1; i < arguments.Length; i++)
            {
                YCmdArgumentAttribute arg = cmd.arguments[i - 1];

                bool successful_parse = false;
                object parsed_arg = 0;
                
                if (arg is YCInt yc)
                {
                    if (yc.Parse<int>(arguments[i], out int val))
                    {
                        parsed_arg = val;
                        successful_parse = true;
                    }
                }
                else if (arg is YCBool yb)
                {
                    if (yb.Parse<bool>(arguments[i], out bool val))
                    {
                        parsed_arg = val;
                        successful_parse = true;
                    }
                }
                else if (arg is YCString ys)
                {
                    if (ys.Parse<string>(arguments[i], out string val))
                    {
                        parsed_arg = val;
                        successful_parse = true;
                    }
                }
                else if (arg is YCEnum ye)
                {
                    if (ye.Parse<Enum>(arguments[i], out Enum val))
                    {
                        parsed_arg = val;
                        successful_parse = true;
                    }
                }

                if (successful_parse)
                {
                    result[i - 1] = parsed_arg;
                }
                else
                {
                    throw new DevConsoleException("Failed to parse or invalid input.");
                }
            }

            return result;
        }

        public static bool Execute(string line)
        {
            string[] parts = line.Split(" ");

            if (parts.Length == 0)
                return false;

            string command = parts[0];

            if (!commands.TryGetValue(command, out Command cmd))
                return false;

            try
            {
                object[] parameters = GetParameters(parts, cmd);

                feedbackActive = true;
                cmd.action.Invoke(null, parameters);
                feedbackActive = false;

                return true;
            } 
            catch (DevConsoleException dce)
            {
                Debug.Log(dce.Message);
            }

            return false;
        }

        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types;
            }
        }

        [YCommand("help", "Show this text.")]
        static void Help()
        {
            string result = "";

            foreach (var kvp in commands)
            {
                List<string> parts = new(10);

                Command cmd = commands[kvp.Key];

                parts.Add(kvp.Key);

                for (int i = 0; i < cmd.arguments.Length; i++)
                {
                    parts.Add($"[{cmd.arguments[i].getDescriptionText()}]");
                }

                parts.Add("-");
                parts.Add(cmd.description);

                string r = string.Join(" ", parts);
                result += r + "\n";
                
                Feedback(r);
            }
        }

        [YCommand("dildo", "uuu i wonder what this does")]
        static void Dildo()
        {
            //string dick = "⠀⠖⠖⡆⠀⠀⠀⠀⣀⣀⣀⠀⠀\n⢸⠀⠀⡗⠐⠉⠁⠀⠀⣇⡤⠽⡆\n⠀⢉⡟⠳⡄⠀⠀⠀⢀⣇⣀⡴⠃\n⠀⡏⠀⠀⡸⠉⠉⠉⠁⠀⠀⠀⠀\n⠀⠙⠒⠚⠁⠀⠀⠀⠀⠀⠀⠀⠀";
            //string dick = "  /---\\\n |     |\n  \\---/\n   | |\n   | |\n   | |\n";
            string dick = "\\\n 8=====D -~ --~\n/";
            dick.Split("\n").ForEach(str => Feedback(str));
        }

        /*[YCommand("getenum", "Show possible values of an enum")]
        [YCString("typeId")]
        static void GetEnum(string typeid)
        {
            Type t = Type.GetType(typeid);

            if (!(t != null && t.IsEnum))
            {
                Feedback($"Enum of type \"{typeid}\" has not been found.");
                if (t != null)
                {
                    Feedback(t.ToString());
                }
                return;
            }

            Array arr = Enum.GetValues(t);

            List<string> parts = new(10);

            foreach (var asd in arr)
            {
                parts.Add(asd.ToString());
            }

            string r = string.Join(", ", parts);
            Feedback($"Values: {r}");
        }*/

        public static void Feedback(string text)
        {
            if (feedbackActive)
            {
                CommandFeedback?.Invoke(text);
            }
        }
    }

    public class DevConsoleException : Exception
    {
        public DevConsoleException(string message) : base(message)
        {
            
        }
    }
}