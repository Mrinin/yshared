
using System;
namespace YShared.Console
{
    /// <summary>
    ///         Use <c>YCBool</c> for boolean arguments.
    /// </summary>
    public sealed class YCBool: YCmdArgumentAttribute
    {
        public override string getDescriptionText()
        {
            return $"{variableName}:bool";
        }

        protected override bool Validate<T>(T s) => true;
        public override string getTypeName => "bool";
        public override bool hasAutocompleteArray => true;
        public override string[] getAutocompleteArray()
        {
            return new [] { "true", "false" };
        }

        public override bool Parse<T>(string s, out T val)
        {
            val = default;
            
            if (s.ToLower() == "true" || s == "1")
            {
                val = (T)(object)true;
                return true;
            }

            if (s.ToLower() == "false" || s == "0")
            {
                val = (T)(object)false;
                return true;
            }

            return false;
        }

        public YCBool(string varname)
        {
            variableName = varname;
        }
    }
}