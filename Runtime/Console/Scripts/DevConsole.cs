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

        public static Dictionary<string, string[]> commandAutocompleteLists { get; private set; }
        public static Action<string, FeedbackFlavor> CommandFeedback;
        public static Action<string, FeedbackFlavor> CommandLog;
        static bool feedbackActive;

        static bool ignoreExceedingParameters = true;
        static bool correctIncorrectParametersIfDefaultExists = true;

        static object[] GetParameters(string[] parameters, int parameter_start_index, Command cmd)
        {
            // arguments[0] is the command itself

            object[] result = new object[cmd.arguments.Length];
            int succesful_parses = 0;

            if (!ignoreExceedingParameters && parameters.Length - parameter_start_index > cmd.arguments.Length)
            {
                throw new DevConsoleException($"Got more arguments than expected: {result.Length}, Got: {parameters.Length - 1}");
            }

            for (int i = 0; i < cmd.arguments.Length; i++)
            {
                YCmdArgumentAttribute arg = cmd.arguments[i];

                bool par_successfully_acquired = false;
                bool parsing_attempted = false;
                object parsed_arg = 0;

                int ind = i + parameter_start_index;

                if (ind < parameters.Length)
                {
                    parsing_attempted = true;

                    if (arg is YCInt yc)
                    {
                        if (yc.Parse<int>(parameters[ind], out int val))
                        {
                            parsed_arg = val;
                            par_successfully_acquired = true;
                        }
                    }
                    else if (arg is YCBool yb)
                    {
                        if (yb.Parse<bool>(parameters[ind], out bool val))
                        {
                            parsed_arg = val;
                            par_successfully_acquired = true;
                        }
                    }
                    else if (arg is YCString ys)
                    {
                        if (ys.Parse<string>(parameters[ind], out string val))
                        {
                            parsed_arg = val;
                            par_successfully_acquired = true;
                        }
                    }
                    else if (arg is YCEnum ye)
                    {
                        if (ye.Parse<Enum>(parameters[ind], out Enum val))
                        {
                            parsed_arg = val;
                            par_successfully_acquired = true;
                        }
                    }
                    else if (arg is YCCmdArg ycmdarg)
                    {
                        if (ycmdarg.Parse<List<Command>>(parameters[ind], out List<Command> val))
                        {
                            parsed_arg = val.ToArray();
                            par_successfully_acquired = true;
                        }
                    }
                }

                if (par_successfully_acquired)
                {
                    result[i] = parsed_arg;
                }
                else
                {
                    if (cmd.functionParameters[i].hasDefault)
                    {                
                        if (!parsing_attempted || (parsing_attempted && correctIncorrectParametersIfDefaultExists))
                        {
                            result[i] = cmd.functionParameters[i].defaultval;
                            par_successfully_acquired = true;
                        }
                    }
                }

                if (par_successfully_acquired)
                {
                    succesful_parses++;
                }
                else
                {
                    string s = $"Expected \"{cmd.arguments[i].variableName}\" of type {cmd.arguments[i].getTypeName}.\nProper usage: {cmd.ProperUsage}";
                    throw new DevConsoleException("Failed to parse or invalid input. " + s);
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
            string[] parts = CommandsHelper.SplitCommand(line);
            if (parts.Length == 0)
            {
                Debug.Log("Early exit?");
                return false;
            }

            int wordCount = parts.Length;

            CommandNode node = CommandRegistry.Root;

            // Traverse all completely entered command words.
            int parameter_start = 0;

            for (int i = 0; i < wordCount; i++)
            {
                if (node.children.ContainsKey(parts[i]))
                {
                    node = node.children[parts[i]];
                    parameter_start++;
                }
                else
                {
                    break;
                }
            }

            bool empty_input = parameter_start == parts.Length;

            if (node.commands.Count == 0)
            {
                string entered_command_portion = string.Join(" ", parts.Take(parameter_start));
                DisplayCommandIndistinctionFailure(node, empty_input, entered_command_portion, parts[^1]);
                return false;
            }

            string main_command = node.commands[0].command;

            List<Command> cmds = node.commands;

            feedbackActive = true;
            bool success = false;
            object[] parameters = null;
            Command commandToRun = null;

            if (cmds.Count == 1)
            {
                ignoreExceedingParameters = true;
                correctIncorrectParametersIfDefaultExists = true;
                commandToRun = cmds[0];
                
                success = GetParameters(cmds[0], parts, parameter_start, out parameters, out string failMessage);

                if (!success)
                    DevConsole.Feedback(failMessage, FeedbackFlavor.Warning);
            }
            else if (cmds.Count >= 2)
            {
                ignoreExceedingParameters = false;
                correctIncorrectParametersIfDefaultExists = false;
                
                string[] failMessages = new string[cmds.Count];

                for (int i = 0; i < cmds.Count; i++)
                {
                    success = GetParameters(cmds[i], parts, parameter_start, out parameters, out failMessages[i]);

                    if (success)
                    {
                        commandToRun = cmds[i];
                        //Debug.Log($"found {commandToRun.FormattedArguments}");
                        break;
                    }
                }

                if (!success)
                {
                    string error_message;
                    FeedbackFlavor flavor;

                    if (empty_input)
                    {
                        error_message = $"Multiple commands are registered to {main_command}. Options:\n";
                        flavor = FeedbackFlavor.Info;
                    }
                    else
                    {
                        error_message = $"Multiple commands are registered to {main_command}, but none fit! Possible matches:\n";
                        flavor = FeedbackFlavor.Error;
                    }

                    for (int i = 0; i < cmds.Count; i++)
                    {
                        error_message += $"- {cmds[i].command} {cmds[i].FormattedArguments}\n";

                        if (flavor == FeedbackFlavor.Error)
                            error_message += $"{failMessages[i]}\n";
                    }
                    DevConsole.Feedback(error_message, flavor);
                }
            }

            if (success)
            {
                RunCommand(commandToRun, parameters);
            }

            feedbackActive = false;
            

            return success;
        }

        static bool RunCommand(Command cmd, object[] parameters)
        {
            try
            {
                object returnVal = null;
                bool hasReturnValue = cmd.hasReturnType;

                if (cmd.IsStatic)
                {
                    returnVal = Invoke(cmd, null, parameters);
                }
                else
                {
                    Type t = cmd.action.DeclaringType;

                    if (cmd.objectFindType == ObjectFindType.First)
                    {
                        UnityEngine.Object obj = GameObject.FindFirstObjectByType(t, cmd.findObjectsInactive);
                        if (obj == null)
                        {
                            DevConsole.Feedback(
                                $"Command \"{cmd.command}\" calls a non-static method on the first instance of type \"{t.ToString()}\", but there is currently no instance of \"{t.ToString()}\" in any scene!", FeedbackFlavor.Warning);
                            return false;
                        }
                        returnVal = Invoke(cmd, obj, parameters);
                    } 
                    else if (cmd.objectFindType == ObjectFindType.Any)
                    {
                        UnityEngine.Object obj = GameObject.FindAnyObjectByType(t, cmd.findObjectsInactive);
                        if (obj == null)
                        {
                            DevConsole.Feedback(
                                $"Command \"{cmd.command}\" calls a non-static method on any instance of type \"{t.ToString()}\", but there is currently no instance of \"{t.ToString()}\" in any scene!", FeedbackFlavor.Warning);
                            return false;
                        }
                        returnVal = Invoke(cmd, obj, parameters);
                    } 
                    else if (cmd.objectFindType == ObjectFindType.All)
                    {
                        hasReturnValue = false;
                        UnityEngine.Object[] objs = GameObject.FindObjectsByType(t, cmd.findObjectsInactive, FindObjectsSortMode.None);
                        if (objs.Length == 0)
                        {
                            DevConsole.Feedback(
                                $"Command \"{cmd.command}\" calls a non-static method on all instances of type \"{t.ToString()}\", but there aren't any instances of \"{t.ToString()}\" in any scene!", FeedbackFlavor.Warning);
                            return false;
                        }

                        for (int i = 0; i < objs.Length; i++)
                        {
                            Invoke(cmd, objs[i], parameters);
                        }
                        DevConsole.Feedback($"Ran on {objs.Length} instances.", FeedbackFlavor.Info);
                    } 
                    else
                    {
                        DevConsole.Feedback($"Unrecognized ObjectFindType {cmd.objectFindType}", FeedbackFlavor.Error);
                        return false;
                    }
                }

                if (hasReturnValue)
                {
                    if (returnVal != null)
                        DevConsole.Feedback(returnVal.ToString(), FeedbackFlavor.Return);
                    else
                        DevConsole.Feedback("null", FeedbackFlavor.Return);
                }

                return true;
            } 
            catch (Exception e)
            {   
                string s = e.ToString();
                string err_only = s.Substring("System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation. ---> ".Length);
                string[] lines = err_only.Split('\n');
                string result = string.Join("\n", lines, 0, Mathf.Max(0, lines.Length - 6));

                DevConsole.Feedback($"Called command threw an error: {result}", FeedbackFlavor.Error);
                return false;
            }
        }

        static object Invoke(Command cmd, UnityEngine.Object targetObject, object[] parameters)
        {
            if (cmd.action is MethodInfo mi)
            {
                return mi.Invoke(targetObject, parameters);
            }

            if (cmd.action is FieldInfo fi)
            {
                if (cmd.functionParameters.Length == 1)
                {
                    fi.SetValue(targetObject, parameters);
                    return null;   
                }

                if (cmd.functionParameters.Length == 0)
                    return fi.GetValue(targetObject);
            }

            return null;
        }

        static bool GetParameters(Command cmd, string[] parts, int parameter_start_index, out object[] parameters, out string propagatedFailMessage)
        {
            propagatedFailMessage = "";

            try
            {
                parameters = GetParameters(parts, parameter_start_index, cmd);
                
                return true;
            } 
            catch (DevConsoleException dce)
            {
                parameters = null;
                propagatedFailMessage = dce.Message;
                return false;
            }
        }

        static void DisplayCommandIndistinctionFailure(CommandNode node, bool isParametersEmpty, string commandPortion, string lastWord)
        {
            feedbackActive = true;

            if (node == CommandRegistry.Root)
            {
                DevConsole.Feedback("Command not found.", FeedbackFlavor.Error);    
            }
            else
            {
                if (!isParametersEmpty)
                {
                    DevConsole.Feedback($"\"{lastWord}\" is not a valid subcommand of {commandPortion}", FeedbackFlavor.Error);
                }

                List<Command> subcommands = new();
                CommandRegistry.GetRecursiveCommands(node, subcommands);

                string result = $"\"{commandPortion}\" contains {subcommands.Count} subcommands:\n";
                result += CommandsHelper.PrintCommands(subcommands, "  ");


                DevConsole.Feedback(result, FeedbackFlavor.Info);
            }

            feedbackActive = false;
        }

        public static void Feedback(string text, FeedbackFlavor flavor = FeedbackFlavor.Feedback)
        {
            if (feedbackActive)
            {
                CommandFeedback?.Invoke(text, flavor);
            }
        }

        public static void Log(string text, FeedbackFlavor flavor = FeedbackFlavor.Feedback)
        {
            CommandLog?.Invoke(text, flavor);
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
        /// <summary>
        /// Gray color. Should be used when a command simply gives back info without doing anything.
        /// </summary>
        Info, 
        /// <summary>
        /// White color, and the default feedback flavor. Should be used when a command completes succesfuly, giving details on how the operation was handled.
        /// </summary>
        Feedback, 
        /// <summary>
        /// Blue-ish color. Should generally not be used - it is the flavor of the return values shown in the console history.
        /// </summary>
        Return, 
        /// <summary>
        /// Blue color. Should generally not be used - it is the flavor of the submitted commands shown in the console history.
        /// </summary>
        Command, 
        /// <summary>
        /// Yellow color. Should be used when a command completes with warnings.
        /// </summary>
        Warning, 
        /// <summary>
        /// Red color. Should be used when a command fails entirely. Exceptions are automatically handled by the DevConsole and are shown in this flavor.
        /// </summary>
        Error, 

        /// <summary>
        /// Blue-ish color.  Should generally not be used - it is the flavor of the introductory text.
        /// </summary>
        Misc
    }
}