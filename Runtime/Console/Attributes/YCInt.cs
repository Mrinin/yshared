using System;
namespace YShared.Console
{
    /// <summary>
    /// Use <c>YCInt(min, max)</c> for integer arguments. (min / max optional)
    /// </summary>
    public sealed class YCInt: YCmdArgumentAttribute
    {
        int min, max;

        public override string getDescriptionText()
        {
            if (min != int.MinValue || max != int.MaxValue)
            {
                return $"{variableName}:int({min},{max})";
            }
            return $"{variableName}:int";
        }
        public override string getTypeName => "int";
        public override bool hasAutocompleteArray => false;

        protected override bool Validate<T>(T s) 
        {
            int a = (int)(object)(s);

            return a >= min && a <= max;
        }

        public override bool Parse<T>(string s, out T val)
        {
            val = default;
            
            if (int.TryParse(s, out int result))
            {
                val = (T)(object)result;

                if (Validate(result))
                {
                    return true;
                }
            }

            return false;
        }

        public YCInt(string varname, int min = int.MinValue, int max = int.MaxValue)
        {
            variableName = varname;
            this.min = min;
            this.max = max;
        }
    }
}