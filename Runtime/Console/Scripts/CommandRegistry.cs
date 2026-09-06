using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Schema;
using Codice.CM.SEIDInfo;
using UnityEngine;
using YShared.Console.Suggestions;
using YShared.MathHelper;

namespace YShared.Console
{
    public static class CommandRegistry
    {
        //public static readonly Dictionary<string, List<Command>> commands = new();
        public static readonly CommandNode Root = new();

        public static readonly Dictionary<Type, Type> parserRegistry = new();
        static Type EnumParserType;

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
            parserRegistry.Clear();
            
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    Type? current = type;

                    while (current != null)
                    {
                        if (current.IsGenericType &&
                            current.GetGenericTypeDefinition() == typeof(YCmdParser<>))
                        {
                            Type parsedType = current.GetGenericArguments()[0];

                            //Debug.Log($"Registered {parsedType} -> {type}");

                            if (parsedType == typeof(Enum))
                            {
                                EnumParserType = type;
                            }

                            parserRegistry[parsedType] = type;
                            break;
                        }

                        current = current.BaseType;
                    }
                }
            }

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
                            Command getter = CreateMethodCommand(property.GetMethod, getterAttribute);

                            RegisterCommand(getter);
                        }

                        if (property.SetMethod != null)
                        {
                            YCommandAttribute setterAttribute = new YCommandAttribute("set", attribute);
                            Command setter = CreateMethodCommand(property.SetMethod, setterAttribute);

                            RegisterCommand(setter);
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

                        bool isToggle = field.GetCustomAttribute<YToggleAttribute>() != null;

                        if (isToggle)
                        {
                            if (!field.IsInitOnly)
                            {
                                YCommandAttribute toggleAttribute = new YCommandAttribute("", attribute);
                                YCommandAttribute setterToggleAttribute = new YCommandAttribute("", attribute);

                                Command toggle = CreateFieldCommand(field, toggleAttribute, FieldValueCommandType.toggle);
                                Command toggleSet = CreateFieldCommand(field, setterToggleAttribute, FieldValueCommandType.toggleSet);

                                toggleSet.HideInHelp = true;

                                RegisterCommand(toggle);
                                RegisterCommand(toggleSet);
                            }
                        }
                        else
                        {
                            if (!field.IsInitOnly)
                            {
                                YCommandAttribute setterAttribute = new YCommandAttribute("set", attribute);
                                RegisterCommand(CreateFieldCommand(field, setterAttribute, FieldValueCommandType.set));
                            }
                        }

                        YCommandAttribute getterAttribute = new YCommandAttribute("get", attribute);
                        Command getter = CreateFieldCommand(field, getterAttribute, FieldValueCommandType.get);

                        if (isToggle)
                            getter.HideInHelp = true;

                        RegisterCommand(getter);
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

            ParameterInfo[] pi;
            pi = method.GetParameters();

            cmd.parameters = new Parameter[pi.Length];
            for (int i = 0; i < pi.Length; i++)
            {
                ParameterInfo p = pi[i];
                if (pi[i].HasDefaultValue)
                {
                    cmd.parameters[i].hasDefault = true;
                    cmd.parameters[i].defaultval = pi[i].DefaultValue;
                }

                YCmdParser parser = GetParserUninitalized(p.ParameterType);
                YArgumentAttribute yaarg = p.GetCustomAttribute<YArgumentAttribute>();

                if (yaarg == null)
                {
                    parser.Initialize(p.Name, "<empty desc>");
                }
                else
                {
                    parser.Initialize(yaarg.Name, yaarg.Description);
                }


                SetSuggestionModifiers(ref cmd.parameters[i], p.Member, p.GetCustomAttributes<SuggestionModifierAttribute>().ToArray());

                cmd.parameters[i].Parser = parser;
            }

            cmd.command = attribute.Name;
            cmd.description = attribute.Desc;
            cmd.objectFindType = attribute.objectFindType;

            cmd.action = method;

            cmd.IsStatic = method.IsStatic;
            cmd.hasReturnType = method.ReturnType != typeof(void);

            cmd.ProperUsageWordAmount = cmd.CommandWords.Length + pi.Length;

            cmd.aliases = new();
            var aliases = method.GetCustomAttributes<AliasAttribute>().ToArray();
            foreach (AliasAttribute alias in aliases)
            {
                cmd.aliases.Add(alias.Shorthand, new(alias));
            }

            return cmd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Command CreateFieldCommand(FieldInfo field, YCommandAttribute attribute, FieldValueCommandType fct)
        {
            Command cmd = new Command();

            /*string suffix = "";
            if (fct == FieldValueCommandType.get) suffix = "get";
            if (fct == FieldValueCommandType.set) suffix = "set";
            if (fct == FieldValueCommandType.toggleSet) suffix = "";
            if (fct == FieldValueCommandType.toggle) suffix = "";*/

            cmd.command = $"{attribute.Name}";
            cmd.description = attribute.Desc;
            cmd.objectFindType = attribute.objectFindType;

            var modifiers = field.GetCustomAttributes<SuggestionModifierAttribute>();

            //cmd.arguments = arguments;
            cmd.action = field;

            cmd.IsStatic = field.IsStatic;

            cmd.hasReturnType = true;;
            cmd.fieldValueCommandType = fct;

            if (fct == FieldValueCommandType.set || fct == FieldValueCommandType.toggleSet)
            {
                cmd.parameters = new Parameter[1];
                YCmdParser parser = GetParserUninitalized(field.FieldType);
                parser.Initialize("value", "");

                SetSuggestionModifiers(ref cmd.parameters[0], field, modifiers.ToArray());

                cmd.parameters[0].Parser = parser;
            }
            if (fct == FieldValueCommandType.toggle)
            {   
                cmd.parameters = new Parameter[0];
            }
            if (fct == FieldValueCommandType.get)
            {   
                cmd.parameters = new Parameter[0];
            }

            cmd.ProperUsageWordAmount = cmd.CommandWords.Length + cmd.parameters.Length;

            return cmd;
        }

        static YCmdParser GetParserUninitalized(Type type)
        {
            Type t = type;
            bool isEnum = false;
            if (t.IsEnum)
            {
                isEnum = true;
            }
            //Debug.Log(t);

            Type parsertype = null;
            if (isEnum || parserRegistry.TryGetValue(t, out parsertype))
            {
                if (isEnum)
                    parsertype = EnumParserType;

                YCmdParser parser = (YCmdParser)Activator.CreateInstance(parsertype);

                if (isEnum)
                    ((YCEnum)parser).SetEnumType(t);

                return parser;
            }
            else
            {
                Debug.LogError($"Parser of type {t} is not implemented!");
            }

            return null;
        }

        static void SetSuggestionModifiers(ref Parameter param, MemberInfo paramInfo, SuggestionModifierAttribute[] modifiers)
        {
            if (modifiers == null || modifiers.Length == 0)
            {
                param.suggestionModifiers = null;
                param.cacheAutocompleteList = true;
                return;
            }

            param.suggestionModifiers = modifiers;

            bool cacheCompatible = true;
            foreach (var sm in param.suggestionModifiers)
            {
                sm.thisMethodOrField = paramInfo;
                if (!sm.cacheCompatible)
                {
                    cacheCompatible = false;
                    break;
                }
            }
            param.cacheAutocompleteList = cacheCompatible;
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

    public enum FieldValueCommandType { get, set, toggle, toggleSet}
}