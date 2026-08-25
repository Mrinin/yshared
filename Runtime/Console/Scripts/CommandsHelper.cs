using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YShared.Console
{
    public static class CommandsHelper
    {
        public static string[] SplitCommand(string line)
        {
            return System.Text.RegularExpressions.Regex.Matches(line, @"[\""].*?[\""]|\S+")
                .Select(m => m.Value.Trim('"'))
                .ToArray();
        }

        public static string GetHelpText(this Command cmd, int padding = 0)
        {
            List<string> parts = new(10);

            parts.Add("-");

            string padded = cmd.command.PadRight(padding);
            parts.Add(padded);

            parts.Add(cmd.FormattedArguments);

            if (!string.IsNullOrEmpty(cmd.description))
                parts.Add(cmd.description);

            return string.Join(" ", parts);
        }

        public static string PrintCommands(List<Command> commands, string spaceAtStart = "")
        {
            string result = "";

            int longest_command = 0;
            foreach (Command cmd in commands)
            {    
                if (cmd.command.Length > longest_command)
                    longest_command = cmd.command.Length;
            }

            foreach (Command cmd in commands)
            {
                string r = cmd.GetHelpText(longest_command + 1);
                result += $"{spaceAtStart}{r}\n";
            }

            return result;
        }
    }
}