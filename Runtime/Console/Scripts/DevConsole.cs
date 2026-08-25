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
                    string s = $"Expected \"{cmd.arguments[i].variableName}\" of type {cmd.arguments[i].getTypeName}.";
                    throw new DevConsoleException("Failed to parse or invalid input. " + s);
                }
            }

            if (succesful_parses != result.Length)
            {
                throw new DevConsoleException($"Number of arguments do not match: Expected: {result.Length}, Got: {succesful_parses}");
            }

            return result;
        }

        // splits command to parts.
        public static string[] SplitCommand(string line)
        {
            return System.Text.RegularExpressions.Regex.Matches(line, @"[\""].*?[\""]|\S+")
                .Select(m => m.Value.Trim('"'))
                .ToArray();
        }

        public static bool Execute(string line)
        {
            string[] parts = SplitCommand(line);
            string main_command = "";

            bool found_command = false;
            int parameter_start = 0;
            int parameters_amt = 0;
            List<Command> cmds = null;

            for (int i = 0; i < parts.Length; i++)
            {
                if (i != 0)
                    main_command += " ";

                main_command += parts[i];

                if (CommandRegistry.GetCommands(main_command, out cmds))
                {
                    found_command = true;
                    parameter_start = i + 1;
                    parameters_amt = parts.Length - parameter_start;
                    break;
                }
            }

            if (!found_command)
            {
                feedbackActive = true;
                DevConsole.Feedback("Command not found.", FeedbackFlavor.Error);
                feedbackActive = false;
                return false;
            }

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
                    if (parameters_amt == 0)
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
                cmd.action.Invoke(null, parameters);
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

        /*public static void CreateAutocompleteList()
        {
            commandAutocompleteLists = new();

            foreach (var kvp in CommandRegistry.commands)
            {
                List<string> autocompletes = new();

                foreach (Command subcommand in kvp.Value)
                {
                    if (subcommand.arguments[].)
                    autocompletes.Add(sub))
                }
            }
        }*/

        public static void Feedback(string text, FeedbackFlavor flavor = FeedbackFlavor.Feedback)
        {
            if (feedbackActive)
            {
                CommandFeedback?.Invoke(text, flavor);
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