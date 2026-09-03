
using System;
namespace YShared.Console
{
    /// <summary>
    ///         Use <c>YCBool</c> for boolean arguments.
    /// </summary>
    public sealed class YCBool: YCmdParser<bool>
    {
        public override string getDescriptorText()
        {
            return $"{variableName}:bool";
        }

        protected override bool Validate(bool s) => true;
        public override string getTypeName => "bool";
        public override bool hasDefaultAutocompleteArray => true;
        public override string[] defaultAutocompleteArray()
        {
            return new [] { "true", "false" };
        }

        public override bool Parse(string s, out bool val)
        {
            val = default;
            
            if (s.ToLower() == "true" || s == "1")
            {
                val = true;
                return true;
            }

            if (s.ToLower() == "false" || s == "0")
            {
                val = false;
                return true;
            }

            return false;
        }
    }
}