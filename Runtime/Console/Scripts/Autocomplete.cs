using System;
using System.Collections.Generic;
using System.Linq;
using Codice.Client.Common.TreeGrouper;
using Codice.CM.Client.Differences;
using UnityEngine;
using YShared.MathHelper;

namespace YShared.Console
{
    public static class Autocomplete
    {
        public static string[] RootCommandArray;
        static string[] commandAsAnArgumentAutocompleteArray;
        public static string[] CommandAsAnArgumentAutocompleteArray()
        {
            if (commandAsAnArgumentAutocompleteArray != null)
                return commandAsAnArgumentAutocompleteArray;

            commandAsAnArgumentAutocompleteArray = CommandRegistry.alphabeticalCommands
                .Select(str => $"\"{str.command}\"")
                .Distinct()
                .ToArray();
            return commandAsAnArgumentAutocompleteArray;
        }
        
        public static string[] GetAutocompleteList(string line)
        {
            if (RootCommandArray == null)
            {
                RootCommandArray = CommandRegistry.Root.children.Keys
                    .Distinct()
                    .OrderBy(str => str, StringComparer.InvariantCultureIgnoreCase)
                    .ToArray();
            }

            if (string.IsNullOrEmpty(line))
                return null;

            string trimmedVersion = line.TrimEnd(' ');
            int trailingSpaceCount = line.Length - trimmedVersion.Length;
            /*DevConsole.Log(trailingSpaceCount);
            if (trailingSpaceCount > 3)
                return null;*/
            
            string[] parts = CommandsHelper.SplitCommand(trimmedVersion);

            int wordCount = parts.Length;
            int parameter_start = 0;

            CommandNode node = CommandRegistry.Root;

            // Traverse all completely entered command words.
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

            // We're after a complete command word.
            // Therefore either another command word or an argument can follow.
            
            // If this node has only subcommands, suggest them.
            if (node.children.Count > 0 && node.commands.Count == 0)
                return node.children.Keys.ToArray();

            // Found the command, this is the end.
            if (node.children.Count == 0 && node.commands.Count == 0)
                return null;
            
            /*if (!trailingSpace)
                return null;*/

            // node.Commands.Count > 0
            List<string> autocompleteList = new List<string>(50);

            int wordIndexCurrentlyWriting = wordCount;
            if (trailingSpaceCount > 0)
                wordIndexCurrentlyWriting++;

            if (node.children.Count > 0)
                autocompleteList.AddRange(node.children.Keys.ToArray());

            for (int i = 0; i < node.commands.Count; i++)
            {
                int par_index = wordIndexCurrentlyWriting -1 - node.commands[i].CommandWords.Length;
                //Debug.Log($"par_index: {par_index} Command Word Length: {node.commands[i].CommandWordLength}, Line Length: {wordCount}");
                AddToAutocompleteList(node.commands[i], par_index, ref autocompleteList);
            }

            return autocompleteList.ToArray();
        }

        public static void AddToAutocompleteList(Command cmd,int word_at, ref List<string> list)
        {
            if (word_at >= 0 && word_at < cmd.parameters.Length)
            {    
                List<string> strings = cmd.parameters[word_at].getAutocomplete();
                if (strings != null)
                {
                    list.AddRange(strings);
                }
            }
        }
    }
}