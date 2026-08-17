using System;
namespace YShared.Console
{
    public sealed class YCCmdArg: YCmdArgumentAttribute
    {
        public override string getDescriptionText()
        {
            return $"{variableName}:command";
        }
        public override string getTypeName => $"command";
        public override bool hasAutocompleteArray => true;
        public override string[] getAutocompleteArray()
        {
            return DevConsole.CommandArray;
        }

        // Unused for this argument type
        protected override bool Validate<T>(T s) => true;

        public override bool Parse<T>(string s, out T val)
        {
            val = default;

            if (DevConsole.commands.TryGetValue(s, out var cmd))
            {
                val = (T)(object)cmd;

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