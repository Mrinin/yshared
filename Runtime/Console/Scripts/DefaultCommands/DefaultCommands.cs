using System;
using System.Collections.Generic;
using UnityEngine;
using YShared.Console.Suggestions;
using YShared.MathHelper;

namespace YShared.Console
{
    public static class DefaultCommands
    {
        [YCommand("help", "Show this text.")]
        static void Help(int page = 0)
        {
            const int PAGE_SIZE = 15;
            string result = "";

            int commands_showed = 0;
            int commands_looped_through = 0;

            int page_amount = ((CommandRegistry.alphabeticalCommands.Length - 1) / PAGE_SIZE) + 1;

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

            foreach (Command cmd in CommandRegistry.alphabeticalCommands)
            {
                if (cmd.HideInHelp)
                    continue;

                if (page != 0)
                {
                    if (!(commands_looped_through >= lb && commands_looped_through < ub))
                    {
                        commands_looped_through++;
                        continue;
                    }

                    commands_looped_through++;
                }

                printCommands.Add(cmd);

                commands_showed++;
            }

            result += CommandsHelper.PrintCommands(printCommands);

            DevConsole.Feedback(result);
        }

        [YCommand("help", "Show the usage of a specific command")]
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

            if (cmds.Length == 0)
            {
                DevConsole.Feedback($"No command with that name was found.", FeedbackFlavor.Warning);
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
                for (int i = 0; i < cmd.parameters.Length; i++)
                {
                    YCmdParser parser = cmd.parser(i);

                    string defaulttext = "";
                    if (cmd.parameters[i].hasDefault)
                        defaulttext = $" = {cmd.parameters[i].defaultval}";

                    string nextline = $"{parser.getDescriptorText()}{defaulttext}";
                    List<Suggestion> autocompleteList = cmd.parameters[i].getAutocomplete();

                    if (autocompleteList != null)
                    {
                        string autocompletePreview = "Options: (";
                        List<string> autocompleteParts = new();

                        foreach (Suggestion s in autocompleteList)
                        {
                            autocompleteParts.Add(s.Word);
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
        static void Echo(string str, FeedbackFlavor flavor)
        {
            DevConsole.Feedback(str, flavor);
        }
    }
}
