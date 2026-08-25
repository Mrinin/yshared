using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace YShared.Console
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


        string formattedArguments;

        public string FormattedArguments
        {
            get
            {
                if (formattedArguments != null)
                    return formattedArguments;

                List<string> parts = new();
                for (int i = 0; i < arguments.Length; i++)
                {
                    string defaulttext = "";
                    if (functionParameters[i].hasDefault)
                        defaulttext = $"={functionParameters[i].defaultval}";

                    parts.Add($"[{arguments[i].getDescriptorText()}{defaulttext}]");
                }

                string r = string.Join(" ", parts);
                formattedArguments = r;
                return r;
            }
        }
    }
    public static class CommandRegistry
    {

        static readonly Dictionary<MethodSignature, Command> commandSignatures = new();
        public static readonly Dictionary<string, List<Command>> commands = new();

        /// <summary>
        /// The list of all top-level commands active, sorted alphabetically.
        /// Note that there may be duplicate entires because of commands with the same name but with different signatures.
        /// </summary>
        public static string[] CommandArray;

        public static int HighestDuplicateCommandAmt;
        public static string HighestDuplicateCommandName;

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
                        RegisterCommand(method);
                    }
                }
            }

            CommandArray = commands
                .Select(cmd => cmd.Key)
                .Distinct()
                .OrderBy(cmd => cmd)
                .ToArray();

            //Debug.Log($"Registered {commands.Count} game commands.");
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

            var signature = MethodSignature.Create(method);

            if (commandSignatures.ContainsKey(signature))
            {
                throw new Exception($"Two methods with the same function signature was attempted to be registered to the CommandRegistry.\nKeeping: \"{commandSignatures[signature].action.Name}\", Not Registering: \"{method.Name}\"");
            }

            commandSignatures[signature] = cmd;

            if (commands.TryGetValue(cmd.command, out List<Command> cmdList))
            {
                cmdList.Add(cmd);
            }
            else
            {
                commands.Add(cmd.command, new List<Command>() { cmd } );
            }
        }

        public static bool GetCommands(string[] parts, out List<Command> cmd)
        {
            cmd = new();

            if (parts.Length == 0)
                return false;

            string command = parts[0];

            

            if (!commands.TryGetValue(command, out cmd))
            {
                return false;
            }

            return true;
        }
    }
}