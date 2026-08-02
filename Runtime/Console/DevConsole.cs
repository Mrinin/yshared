using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine;
using YShared.Console;
using YShared.MathHelper;

namespace YShared.Console
{
    public static class DevConsole
    {
        struct Parameter
        {
            public bool hasDefault;
            public object defaultval;
        }

        class Command
        {
            public string command;
            public string description;
            public YCmdArgumentAttribute[] arguments;
            public MethodInfo action;
            public Parameter[] functionParameters;
        }
        private static readonly SortedDictionary<string, Command> commands = new();

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

                        ParameterInfo[] pi = method.GetParameters();
                        cmd.functionParameters = new Parameter[pi.Length];
                        for (int i = 0; i < pi.Length; i++)
                        {
                            if (pi[i].HasDefaultValue)
                            {
                                cmd.functionParameters[i].hasDefault = true;
                                cmd.functionParameters[i].defaultval = pi[i].DefaultValue;
                            }
                        }

                        commands[attribute.Name] = cmd;
                    }
                }
            }

            Debug.Log($"Registered {commands.Count} game commands.");
        }

        static object[] GetParameters(string[] arguments, Command cmd)
        {
            // arguments[0] is the command itself

            object[] result = new object[cmd.arguments.Length];
            int succesful_parses = 0;

            for (int i = 0; i < cmd.arguments.Length; i++)
            {
                YCmdArgumentAttribute arg = cmd.arguments[i];

                bool successful_parse = false;
                object parsed_arg = 0;

                if (i + 1 < arguments.Length)
                {
                    if (arg is YCInt yc)
                    {
                        if (yc.Parse<int>(arguments[i + 1], out int val))
                        {
                            parsed_arg = val;
                            successful_parse = true;
                        }
                    }
                    else if (arg is YCBool yb)
                    {
                        if (yb.Parse<bool>(arguments[i + 1], out bool val))
                        {
                            parsed_arg = val;
                            successful_parse = true;
                        }
                    }
                    else if (arg is YCString ys)
                    {
                        if (ys.Parse<string>(arguments[i + 1], out string val))
                        {
                            parsed_arg = val;
                            successful_parse = true;
                        }
                    }
                    else if (arg is YCEnum ye)
                    {
                        if (ye.Parse<Enum>(arguments[i + 1], out Enum val))
                        {
                            parsed_arg = val;
                            successful_parse = true;
                        }
                    }
                }

                if (successful_parse)
                {
                    result[i] = parsed_arg;
                    succesful_parses++;
                }
                else
                {
                    if (cmd.functionParameters[i].hasDefault)
                    {
                        result[i] = cmd.functionParameters[i].defaultval;
                        succesful_parses++;
                    }
                    else
                    {
                        throw new DevConsoleException("Failed to parse or invalid input.");
                    }
                }
            }

            if (succesful_parses != result.Length)
            {
                throw new DevConsoleException($"Number of arguments do not match: Expected: {result.Length}, Got: {succesful_parses}");
            }

            return result;
        }

        public static bool Execute(string line)
        {
            string[] parts = System.Text.RegularExpressions.Regex.Matches(line, @"[\""].*?[\""]|\S+")
                .Select(m => m.Value.Trim('"'))
                .ToArray();

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
            [YCInt("page", 0)]
            static void Help(int page = 0)
            {
                const int PAGE_SIZE = 10;
                string result = "";

                int commands_showed = 0;
                int commands_looped_through = 0;

                int page_amount = ((commands.Count - 1) / PAGE_SIZE) + 1;

                if (page != 0)
                    DevConsole.Feedback($"Pages {page}/{page_amount}");
                else
                {
                    if (page_amount > 1)
                        DevConsole.Feedback($"{page_amount} Pages (all shown)");
                    else
                        DevConsole.Feedback($"{page_amount} Page (all shown)");
                }

                foreach (var kvp in commands)
                {
                    if (page != 0)
                    {    
                        commands_looped_through++;
                        int lb = commands_showed * PAGE_SIZE * (page - 1);
                        int ub = commands_showed * PAGE_SIZE * (page);

                        if (!(commands_looped_through >= lb && commands_looped_through < ub))
                        {
                            continue;
                        }
                    }

                    List<string> parts = new(10);

                    Command cmd = commands[kvp.Key];

                    parts.Add(kvp.Key);

                    for (int i = 0; i < cmd.arguments.Length; i++)
                    {
                        string defaulttext = "";
                        if (cmd.functionParameters[i].hasDefault)
                            defaulttext = $"={cmd.functionParameters[i].defaultval}";

                        parts.Add($"[{cmd.arguments[i].getDescriptionText()}{defaulttext}]");
                    }

                    parts.Add("-");
                    parts.Add(cmd.description);

                    string r = string.Join(" ", parts);
                    result += r + "\n";
                    
                    DevConsole.Feedback(r);

                    commands_showed++;
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