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
                throw new DevConsoleException($"Number of arguments do not match: Expected: {result.Length}, Got: {arguments.Length - 1 }");
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

            feedbackActive = true;
            if (!commands.TryGetValue(command, out Command cmd))
            {
                DevConsole.Feedback("Invalid command");
                return false;
            }

            bool succesful = false;
            try
            {
                object[] parameters = GetParameters(parts, cmd);

                cmd.action.Invoke(null, parameters);

                succesful = true;
            } 
            catch (DevConsoleException dce)
            {
                DevConsole.Feedback(dce.Message);
                succesful = false;
            } 
            finally
            {
                feedbackActive = false;
            }

            return succesful;

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

        public static void Feedback(string text)
        {
            if (feedbackActive)
            {
                CommandFeedback?.Invoke(text);
            }
        }

        static class DefaultCommands
        {
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

            [YCommand("enum", "View the available options of a command that requires enums")]
            [YCString("command")]
            static void EnumFields(string command)
            {
                if (commands.TryGetValue(command, out Command cmd))
                {
                    foreach (var arg in cmd.arguments)
                    {
                        if (arg is YCEnum ye)
                        {
                            List<string> parts = new();
                            Array arr = Enum.GetValues(ye.enumType);
                            foreach (object v in arr)
                            {
                                int ver = Convert.ToInt32(v);
                                parts.Add($"{v.ToString()}({ver})");
                            }

                            string r = string.Join(", ", parts);
                            string result = $"{ye.variableName} values: {r}.";
                            
                            Feedback(result);
                        }
                    }
                    
                    return;
                }

                Feedback($"Command {command} not found!");
            }


            [YCommand("dildo", "uuu i wonder what this does")]
            static void Dildo()
            {
                string dick = "\\\n 8=====D -~ --~\n/";
                dick.Split("\n").ForEach(str => Feedback(str));
            }

            [YCommand("echo", "Echo feedback")]
            [YCString("string")]
            static void Echo(string str)
            {
                Feedback(str);
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