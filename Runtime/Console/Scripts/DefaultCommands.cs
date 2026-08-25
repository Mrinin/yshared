using System;
using System.Collections.Generic;
using UnityEngine;
using YShared.MathHelper;

namespace YShared.Console
{
    public static class DefaultCommands
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

            int page_amount = ((CommandRegistry.commands.Count - 1) / PAGE_SIZE) + 1;

            if (page > page_amount)
            {
                DevConsole.Feedback($"{page} is greater than the amount of pages there are. ({page_amount})", FeedbackFlavor.Warning);
                return;
            }

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

            foreach (var kvp in CommandRegistry.commands)
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

                printCommands.AddRange(kvp.Value);

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

                    parts.Add($"[{cmd.arguments[i].getDescriptorText()}{defaulttext}]");
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

        [YCommand("help", "Show the usage of a specific command")]
        [YCCmdArg("command")]
        static void Help2(Command[] cmds)
        {
            List<string> parts = new(10);

            string[] newlines = new string[] { "", "  ", "    " };

            int newline_level = 0;
            if (cmds.Length > 1)
            {
                DevConsole.Feedback($"Found {cmds.Length} commands registered to \"{cmds[0].command}\".");
                newline_level++;
            }

            for (int j = 0; j < cmds.Length; j++)
            {
                Command cmd = cmds[j];

                parts.Add(newlines[newline_level]);
                parts.Add(cmd.command);
                parts.Add(":");

                if (string.IsNullOrEmpty(cmd.description))
                    parts.Add("(No description)");
                else
                    parts.Add(cmd.description);

                newline_level++;
                for (int i = 0; i < cmd.arguments.Length; i++)
                {
                    string defaulttext = "";
                    if (cmd.functionParameters[i].hasDefault)
                        defaulttext = $" = {cmd.functionParameters[i].defaultval}";

                    string nextline = $"{cmd.arguments[i].getDescriptorText()}{defaulttext}";

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

                    parts.Add($"\n{newlines[newline_level]}- {nextline}");
                }
                newline_level--;
                string r = string.Join(" ", parts);
                parts.Clear();

                DevConsole.Feedback(r);
            }
        }

        [YCommand("dildo", "uuu i wonder what this does")]
        static void Dildo()
        {
            string dick = "\\\n 8=====D -~ --~\n/";
            dick.Split("\n").ForEach(str => DevConsole.Feedback(str));
        }

        [YCommand("echo", "Echo feedback")]
        [YCString("string")]
        [YCEnum("flavor", typeof(FeedbackFlavor))]
        static void Echo(string str, FeedbackFlavor flavor)
        {
            DevConsole.Feedback(str, flavor);
        }
    }
}
