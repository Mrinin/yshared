using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Schema;
using UnityEngine;

namespace YShared.Console
{
    public static class CommandRegistry
    {
        //public static readonly Dictionary<string, List<Command>> commands = new();
        public static readonly CommandNode Root = new();

        /// <summary>
        /// The list of all top-level commands active, sorted alphabetically.
        /// Note that there may be duplicate entires because of commands with the same name but with different signatures.
        /// </summary>
        
        public static Command[] alphabeticalCommands;
        private static List<Command> Commands = new();

        public static int HighestDuplicateCommandAmt;
        public static string HighestDuplicateCommandName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Root.Clear();
            Commands.Clear();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetTypesSafe(assembly))
                {
                    foreach (MethodInfo method in type.GetMethods(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.DeclaredOnly
                        ))
                    {
                        var attribute = method.GetCustomAttribute<YCommandAttribute>();

                        if (attribute == null)
                            continue;

                        RegisterCommand(CreateMethodCommand(method, attribute));
                    }

                    foreach (PropertyInfo property in type.GetProperties(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.DeclaredOnly))
                    {
                        var attribute = property.GetCustomAttribute<YCommandAttribute>();

                        if (attribute == null)
                            continue;

                        if (property.GetMethod != null)
                        {
                            YCommandAttribute getterAttribute = new YCommandAttribute("get", attribute);

                            RegisterCommand(CreateMethodCommand(property.GetMethod, getterAttribute));
                        }

                        if (property.SetMethod != null)
                        {
                            YCommandAttribute setterAttribute = new YCommandAttribute("set", attribute);

                            RegisterCommand(CreateMethodCommand(property.SetMethod, setterAttribute));
                        }
                    }

                    foreach (FieldInfo field in type.GetFields(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.DeclaredOnly))
                    {
                        var attribute = field.GetCustomAttribute<YCommandAttribute>();

                        if (attribute == null)
                            continue;

                        YCommandAttribute getterAttribute = new YCommandAttribute("get", attribute);

                        RegisterCommand(CreateFieldCommand(field, getterAttribute, false));

                        if (!field.IsInitOnly)
                        {
                            YCommandAttribute setterAttribute = new YCommandAttribute("set", attribute);

                            RegisterCommand(CreateFieldCommand(field, setterAttribute, true));
                        }
                    }
                }
            }

            alphabeticalCommands = Commands
                .OrderBy(cmd => cmd.command)
                .ToArray();

            //Debug.Log($"Registered {alphabeticalCommands.Length}");
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

        public static void RegisterCommand(Command cmd)
        {
            Commands.Add(cmd);

            CommandNode node = Root;

            foreach (string word in cmd.CommandWords)
            {
                if (!node.children.TryGetValue(word, out CommandNode child))
                {
                    child = new CommandNode();
                    node.children.Add(word, child);
                }

                node = child;
            }

            node.commands.Add(cmd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Command CreateMethodCommand(MethodInfo method, YCommandAttribute attribute)
        {
            Command cmd = new Command();

            var arguments = method.GetCustomAttributes<YCmdArgumentAttribute>().ToArray();

            cmd.command = attribute.Name;
            cmd.description = attribute.Desc;
            cmd.objectFindType = attribute.objectFindType;

            cmd.arguments = arguments;
            cmd.action = method;

            cmd.IsStatic = method.IsStatic;
            cmd.hasReturnType = method.ReturnType != typeof(void);

            ParameterInfo[] pi;
            pi = method.GetParameters();

            cmd.functionParameters = new Parameter[pi.Length];
            for (int i = 0; i < pi.Length; i++)
            {
                if (pi[i].HasDefaultValue)
                {
                    cmd.functionParameters[i].hasDefault = true;
                    cmd.functionParameters[i].defaultval = pi[i].DefaultValue;
                }
            }

            cmd.ProperUsageWordAmount = cmd.CommandWords.Length + pi.Length;

            cmd.aliases = new();
            var aliases = method.GetCustomAttributes<AliasAttribute>().ToArray();
            foreach (AliasAttribute alias in aliases)
            {
                cmd.aliases.Add(alias.Shorthand, new(alias));
            }

            return cmd;
        }

        public static Command CreateFieldCommand(FieldInfo field, YCommandAttribute attribute, bool isSet)
        {
            return null;
            Command cmd = new Command();

            string suffix = isSet ? "set" : "get";

            cmd.command = $"{attribute.Name} {suffix}";
            cmd.description = attribute.Desc;
            cmd.objectFindType = attribute.objectFindType;

            //cmd.arguments = arguments;
            cmd.action = field;

            cmd.IsStatic = field.IsStatic;

            cmd.hasReturnType = !isSet;

            if (isSet)
                cmd.functionParameters = new Parameter[1];
            else
                cmd.functionParameters = new Parameter[0];

            cmd.ProperUsageWordAmount = cmd.CommandWords.Length + cmd.functionParameters.Length;

            return cmd;
        }

        public static bool GetCommands(string command_str, out List<Command> cmd)
        {
            cmd = new();

            if (string.IsNullOrEmpty(command_str))
            {
                return false;
            }

            CommandNode node = Root;
            bool fail = false;

            foreach (string s in CommandsHelper.SplitCommand(command_str))
            {
                if (node.children.TryGetValue(s, out CommandNode value))
                {
                    node = value;
                }
                else
                {
                    fail = false;
                    break;
                }
            }

            if (!fail)
            {
                cmd = node.commands;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Find all the commands under this CommandNode. Pass in <c>CommandsRegistry.Root</c> to find all commands.
        /// </summary>
        /// <param name="node">Root node</param>
        /// <param name="commands">Recursive Filled List</param>
        public static void GetRecursiveCommands(CommandNode node, List<Command> commands)
        {
            commands.AddRange(node.commands);

            foreach (var kvp in node.children)
            {
                GetRecursiveCommands(kvp.Value, commands);
            }
        }
    }
}