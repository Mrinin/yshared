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

        static readonly Dictionary<MethodSignature, Command> commandSignatures = new();
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
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic))
                    {
                        RegisterCommand(method);
                    }
                }
            }

            alphabeticalCommands = Commands
                .OrderBy(cmd => cmd.command)
                .ToArray();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterCommand(MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<YCommandAttribute>();

            if (attribute == null)
                return;

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
            cmd.ProperUsageWordAmount = cmd.CommandWords.Length + pi.Length;

            cmd.aliases = new();
            var aliases = method.GetCustomAttributes<AliasAttribute>().ToArray();
            foreach (AliasAttribute alias in aliases)
            {
                cmd.aliases.Add(alias.Shorthand, new(alias));
            }

            var signature = MethodSignature.Create(method);
            if (commandSignatures.ContainsKey(signature))
            {
                throw new Exception($"Two methods with the same function signature was attempted to be registered to the CommandRegistry.\nKeeping: \"{commandSignatures[signature].action.Name}\", Not Registering: \"{method.Name}\"");
            }

            commandSignatures[signature] = cmd;
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