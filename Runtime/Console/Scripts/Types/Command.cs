using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YShared.Console
{
    public struct Parameter
    {
        public bool hasDefault;
        public object defaultval;
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

        public YCmdArgumentAttribute[] arguments;
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