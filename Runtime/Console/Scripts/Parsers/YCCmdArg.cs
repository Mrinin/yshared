using System;
using System.Collections.Generic;
namespace YShared.Console
{
    /// <summary>
    ///         Use <c>YCCmdArg</c> to get registered commands as an input. (Use Command[])
    /// </summary>
    public sealed class YCCmdArg: YCmdParser<Command[]>
    {
        public override string getDescriptorText()
        {
            return $"{variableName}:command";
        }
        public override string getTypeName => $"command";
        public override bool hasDefaultAutocompleteArray => true;
        public override string[] defaultAutocompleteArray()
        {
            return Autocomplete.CommandAsAnArgumentAutocompleteArray();
        }

        // Unused for this argument type
        protected override bool Validate(Command[] s) => true;

        public override bool Parse(string s, out Command[] val)
        {
            val = default;

            if (CommandRegistry.GetCommands(s, out List<Command> cmds))
            {
                val = cmds.ToArray();

                return true;
            }

            return false;
        }

        /*public YCCmdArg(string varname)
        {
            variableName = varname;
        }*/
    }
}