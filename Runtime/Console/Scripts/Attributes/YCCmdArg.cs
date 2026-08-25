using System;
namespace YShared.Console
{
    /// <summary>
    ///         Use <c>YCCmdArg</c> to get registered commands as an input. (Use Command[])
    /// </summary>
    public sealed class YCCmdArg: YCmdArgumentAttribute
    {
        public override string getDescriptorText()
        {
            return $"{variableName}:command";
        }
        public override string getTypeName => $"command";
        public override bool hasAutocompleteArray => true;
        public override string[] getAutocompleteArray()
        {
            return Autocomplete.RootCommandArray;
        }

        // Unused for this argument type
        protected override bool Validate<T>(T s) => true;

        public override bool Parse<T>(string s, out T val)
        {
            val = default;

            if (CommandRegistry.GetCommands(s, out var cmds))
            {
                val = (T)(object)cmds;
                
                return true;
            }

            return false;
        }

        public YCCmdArg(string varname)
        {
            variableName = varname;
        }
    }
}