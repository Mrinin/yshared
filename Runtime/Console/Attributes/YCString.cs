using System;
namespace YShared.Console
{
    /// <summary>
    ///         Use <c>YCString</c> for string arguments.
    /// </summary>
    public sealed class YCString: YCmdArgumentAttribute
    {
        public override string getDescriptionText()
        {
            return $"{variableName}:string";
        }
        public override string getTypeName => "string";
        public override bool hasAutocompleteArray => false;

        protected override bool Validate<T>(T s) => true;

        public override bool Parse<T>(string s, out T val)
        {
            val = default;
            
            val = (T)(object)s;
            return true;
        }

        public YCString(string varname)
        {
            variableName = varname;
        }
    }
}