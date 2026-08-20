using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using Codice.CM.Client.Differences.Merge;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine;
using YShared.Console;
using YShared.MathHelper;

namespace YShared.Console
{
    public static class DevConsole
    {
        public struct Parameter
        {
            public bool hasDefault;
            public object defaultval;
        }

        public class Command
        {
            public string command;
            public string description;
            public YCmdArgumentAttribute[] arguments;
            public MethodInfo action;
            public Parameter[] functionParameters;
        }
        public static readonly SortedDictionary<string, Command> commands = new();
        public static string[] CommandArray;

        public static Action<string, FeedbackFlavor> CommandFeedback;
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

            DevConsole.CommandArray = DevConsole.commands.Select(cmd => cmd.Key).ToArray();
            //Debug.Log($"Registered {commands.Count} game commands.");
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
                    else if (arg is YCCmdArg ycmdarg)
                    {
                        if (ycmdarg.Parse<Command>(arguments[i + 1], out Command val))
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
                        string s = $"Expected \"{cmd.arguments[i].variableName}\" of type {cmd.arguments[i].getTypeName}";
                        throw new DevConsoleException("Failed to parse or invalid input. " + s);
                    }
                }
            }

            if (succesful_parses != result.Length)
            {
                throw new DevConsoleException($"Number of arguments do not match: Expected: {result.Length}, Got: {succesful_parses}");
            }

            return result;
        }

        // splits command to parts.
        static string[] SplitCommand(string line)
        {
            return System.Text.RegularExpressions.Regex.Matches(line, @"[\""].*?[\""]|\S+")
                .Select(m => m.Value.Trim('"'))
                .ToArray();
        }

        static bool GetCommand(string[] parts, out Command cmd)
        {
            cmd = default;

            if (parts.Length == 0)
                return false;

            string command = parts[0];

            if (!commands.TryGetValue(command, out cmd))
            {
                return false;
            }

            return true;
        }

        public static bool Execute(string line)
        {
            string[] parts = SplitCommand(line);
            if (!GetCommand(parts, out Command cmd))
            {
                feedbackActive = true;
                DevConsole.Feedback("Invalid command", FeedbackFlavor.Info);
                feedbackActive = false;
                return false;
            }

            feedbackActive = true;
            bool succesful = false;
            try
            {
                object[] parameters = GetParameters(parts, cmd);

                cmd.action.Invoke(null, parameters);

                succesful = true;
            } 
            catch (DevConsoleException dce)
            {
                DevConsole.Feedback(dce.Message, FeedbackFlavor.Warning);
                succesful = false;
            }
            catch (Exception e)
            {   
                string s = e.ToString();
                string err_only = s.Substring("System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation. ---> ".Length);
                string[] lines = err_only.Split('\n');
                string result = string.Join("\n", lines, 0, lines.Length - 6);

                DevConsole.Feedback($"Called command threw an error: {result}", FeedbackFlavor.Error);
                succesful = false;
            }
            finally
            {
                feedbackActive = false;
            }

            return succesful;
        }

        public static string[] GetAutocompleteList(string line)
        {
            if (line.Length == 0)
                return null;

            bool has_space_at_the_end = line.Substring(line.Length - 1) == " ";
            string[] parts = SplitCommand(line);

            int next_input = parts.Length - 1;
            if (has_space_at_the_end)
                next_input++;

        
            if (next_input <= 0)
            {
                return CommandArray;
            }
            
            if (GetCommand(parts, out Command cmd))
            { 
                next_input--;
                if (next_input < cmd.arguments.Length)
                {
                    var param = cmd.arguments[next_input];

                    if (param.hasAutocompleteArray)
                    {
                        return param.getAutocompleteArray();
                    }
                }
            }

            return null;
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

        public static void Feedback(string text, FeedbackFlavor flavor = FeedbackFlavor.Feedback)
        {
            if (feedbackActive)
            {
                CommandFeedback?.Invoke(text, flavor);
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
                int longest_command = 0;

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

                int lb = PAGE_SIZE * (page - 1);
                int ub = PAGE_SIZE * (page);

                List<Command> printCommands = new List<Command>();

                foreach (var kvp in commands)
                {
                    if (page != 0)
                    {    
                        if (!(commands_looped_through >= lb && commands_looped_through < ub))
                        {
                            commands_looped_through++;
                            continue;
                        }

                        commands_looped_through++;
                    }

                    printCommands.Add(kvp.Value);

                    if (kvp.Key.Length > longest_command)
                        longest_command = kvp.Key.Length;

                    commands_showed++;
                }

                foreach (Command cmd in printCommands)
                {
                    List<string> parts = new(10);

                    parts.Add("-");

                    string padded = cmd.command.PadRight(longest_command + 1);
                    parts.Add(padded);

                    for (int i = 0; i < cmd.arguments.Length; i++)
                    {
                        string defaulttext = "";
                        if (cmd.functionParameters[i].hasDefault)
                            defaulttext = $"={cmd.functionParameters[i].defaultval}";

                        parts.Add($"[{cmd.arguments[i].getDescriptionText()}{defaulttext}]");
                    }

                    if (string.IsNullOrEmpty(cmd.description))
                        parts.Add("(No description)");
                    else
                        parts.Add(cmd.description);

                    string r = string.Join(" ", parts);
                    result += r + "\n";
                    
                    DevConsole.Feedback(r);
                }
            }

            [YCommand("helpcmd", "Show the usage of a specific command")]
            [YCCmdArg("command")]
            static void Help2(Command cmd)
            {
                List<string> parts = new(10);

                parts.Add(cmd.command);
                parts.Add(":");

                if (string.IsNullOrEmpty(cmd.description))
                    parts.Add("(No description)");
                else
                    parts.Add(cmd.description);

                for (int i = 0; i < cmd.arguments.Length; i++)
                {
                    string defaulttext = "";
                    if (cmd.functionParameters[i].hasDefault)
                        defaulttext = $" = {cmd.functionParameters[i].defaultval}";

                    string nextline = $"{cmd.arguments[i].getDescriptionText()}{defaulttext}";

                    if (cmd.arguments[i].hasAutocompleteArray)
                    {
                        string autocompletePreview = "Options: (";
                        List<string> autocompleteParts = new();

                        foreach (string s in cmd.arguments[i].getAutocompleteArray())
                        {
                            autocompleteParts.Add(s);
                        }

                        autocompletePreview += string.Join(", ", autocompleteParts);

                        autocompletePreview += ")";
                        nextline += " " + autocompletePreview;
                    }

                    parts.Add($"\n- {nextline}"); 
                }


                string r = string.Join(" ", parts);
                
                DevConsole.Feedback(r);
            }

            [YCommand("enum", "View the available options of a command that requires enums")]
            [YCCmdArg("command")]
            static void EnumFields(Command cmd)
            {
                foreach (var arg in cmd.arguments)
                {
                    int amt = 0;
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
                        amt++;
                    }

                    if (amt == 0)
                    {
                        Feedback($"That command ({cmd.command}) does not have any enum inputs.", FeedbackFlavor.Warning);
                    }
                }
                
                return;
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

    public enum FeedbackFlavor
    {
        Info, Feedback, Command, Warning, Error, Misc
    }
}