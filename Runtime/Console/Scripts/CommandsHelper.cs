using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace YShared.Console
{
    public static class CommandsHelper
    {
        public static string[] SplitCommand(string line, bool trimQuotes = true)
        {
            List<string> result = new();
            StringBuilder current = new();

            char? quote = null;
            bool escaped = false;

            foreach (char c in line)
            {
                // Only process escape characters when trimming quotes.
                if (trimQuotes)
                {
                    if (escaped)
                    {
                        current.Append(c);
                        escaped = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                }

                if (quote.HasValue)
                {
                    if (c == quote.Value)
                    {
                        if (!trimQuotes)
                            current.Append(c);

                        quote = null;
                    }
                    else
                    {
                        current.Append(c);
                    }

                    continue;
                }

                if (c is '"' or '\'' or '`')
                {
                    if (!trimQuotes)
                        current.Append(c);

                    quote = c;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (trimQuotes && escaped)
                current.Append('\\');

            if (current.Length > 0)
                result.Add(current.ToString());

            return result.ToArray();
        }



        public static string GetHelpText(this Command cmd, int padding = 0)
        {
            List<string> parts = new(10);

            parts.Add("-");

            bool has_desc = !string.IsNullOrEmpty(cmd.description);
            if (cmd.parameters.Length == 0)
                padding -= 1;

            string padded = cmd.command.PadRight(padding);
            parts.Add(padded);

            parts.Add(cmd.FormattedArguments);

            if (has_desc)
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