using System;
namespace YShared.Console
{
    /// <summary>
    ///         Use <c>YCString</c> for string arguments.
    /// </summary>
    public sealed class YCString: YCmdParser<string>
    {
        string[] autocompleteOptions;
        bool hasArray;

        public override string getDescriptorText()
        {
            return $"{variableName}:string";
        }
        public override string getTypeName => "string";
        public override bool hasDefaultAutocompleteArray => hasArray;
        public override string[] defaultAutocompleteArray()
        {
            return autocompleteOptions;
        }

        protected override bool Validate(string s) => true;

        public override bool Parse(string s, out string val)
        {
            val = default;
            
            val = (string)(object)s;

            return true;
        }
    }
}