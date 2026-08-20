using System;
namespace YShared.Console
{
    /// <summary>
    ///         Use <c>YCString</c> for string arguments.
    /// </summary>
    public sealed class YCString: YCmdArgumentAttribute
    {
        string[] autocompleteOptions;
        bool hasArray;

        public override string getDescriptionText()
        {
            return $"{variableName}:string";
        }
        public override string getTypeName => "string";
        public override bool hasAutocompleteArray => hasArray;
        public override string[] getAutocompleteArray()
        {
            return autocompleteOptions;
        }

        protected override bool Validate<T>(T s) => true;

        public override bool Parse<T>(string s, out T val)
        {
            val = default;
            
            val = (T)(object)s;
            return true;
        }

        public YCString(string varname, string[] autocompleteOptions = null)
        {
            variableName = varname;
            hasArray = autocompleteOptions != null;

            this.autocompleteOptions = autocompleteOptions;
        }
    }
}