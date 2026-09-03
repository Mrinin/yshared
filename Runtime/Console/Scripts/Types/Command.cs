using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using YShared.Console.Suggestion;

namespace YShared.Console
{
    public struct Parameter
    {
        public bool hasDefault;
        public object defaultval;

        public YCmdParser Parser;
        public SuggestionModifierAttribute[] suggestionModifiers;

        public bool cacheAutocompleteList;

        List<string> cachedAutocompleteList;
        bool isAutocompleteListCached;

        public List<string> getAutocomplete()
        {
            if (isAutocompleteListCached && cachedAutocompleteList != null)
            {
                return cachedAutocompleteList;
            }

            List<string> strings;
            if (!Parser.hasDefaultAutocompleteArray)
                strings = new List<string>();
            else
                strings = Parser.defaultAutocompleteArray().ToList();

            //UnityEngine.Debug.Log(suggestionModifiers == null);
            
            if (suggestionModifiers != null)
            {
                for (int i = 0; i < suggestionModifiers.Length; i++)
                {
                    suggestionModifiers[i].modifyAutocompleteList(strings);
                }
            }

            for (int i = 0; i < strings.Count; i++)
            {
                if (strings[i].Contains(" ") && !(strings[i].StartsWith('"') && strings[i].EndsWith('"')))
                {
                    strings[i] = $"\"{strings[i]}\"";
                }
            }

            if (strings.Count == 0)
                strings = null;

            if (cacheAutocompleteList)
            {
                cachedAutocompleteList = strings;
                isAutocompleteListCached = true;
            }

            return strings;
        }
    }

    public sealed class Command
    {
        public string command;
        public string description;
        
        public ObjectFindType objectFindType; 
        public UnityEngine.FindObjectsInactive findObjectsInactive;

        public MemberInfo action;
        // Can be: 
        // - MethodInfo
        // - Field Info

        public bool IsStatic { get; set; }
        public bool hasReturnType { get; set; }

        public Parameter[] parameters;
        public YCmdParser parser(int ind) => parameters[ind].Parser;
        public FieldValueCommandType fieldValueCommandType;


        string formattedArguments;

        public string FormattedArguments
        {
            get
            {
                if (formattedArguments != null)
                    return formattedArguments;

                List<string> parts = new();
                for (int i = 0; i < parameters.Length; i++)
                {
                    string defaulttext = "";
                    if (parameters[i].hasDefault)
                        defaulttext = $"={parameters[i].defaultval}";

                    parts.Add($"[{parser(i).getDescriptorText()}{defaulttext}]");
                }

                string r = string.Join(" ", parts);
                formattedArguments = r;
                return r;
            }
        }

        string[] commandWords;
        public string[] CommandWords
        {
            get
            {
                if (commandWords != null)
                    return commandWords;

                commandWords = System.Text.RegularExpressions.Regex.Matches(command, @"[\""].*?[\""]|\S+")
                    .Select(m => m.Value.Trim('"'))
                    .ToArray();

                return commandWords;
            }
        }

        public int ProperUsageWordAmount;
        string properUsage;
        public string ProperUsage
        {
            get
            {
                if (properUsage != null)
                    return properUsage;

                properUsage = $"{command} {formattedArguments}";

                return properUsage;
            }
        }

        public Dictionary<string, AliasData> aliases;
    }

    public class AliasData
    {
        public string Alias;
        public string Command;

        public AliasData(AliasAttribute attr)
        {
            Alias= attr.Shorthand;
            Command = $"{attr.Shorthand} {attr.Arguments}";
        }
    }
}