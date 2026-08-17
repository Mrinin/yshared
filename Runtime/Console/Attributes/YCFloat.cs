using System;
namespace YShared.Console
{

    /// <summary>
    /// Use <c>YCFloat(min, max)</c> for float arguments. (min / max optional)
    /// </summary>
    public sealed class YCFloat: YCmdArgumentAttribute
    {
        float min, max;

        public override string getDescriptionText()
        {
            if (min != float.MinValue || max != float.MaxValue)
            {
                return $"{variableName}:float({min},{max})";
            }
            return $"{variableName}:float";
        }
        public override string getTypeName => "float";
        public override bool hasAutocompleteArray => false;

        protected override bool Validate<T>(T s) 
        {
            float a = (float)(object)(s);

            return a >= min && a <= max;
        }

        public override bool Parse<T>(string s, out T val)
        {
            val = default;
            
            if (float.TryParse(s, out float result))
            {
                val = (T)(object)result;

                if (Validate(result))
                {
                    return true;
                }
            }

            return false;
        }

        public YCFloat(string varname, float min = float.MinValue, float max = float.MaxValue)
        {
            variableName = varname;
            this.min = min;
            this.max = max;
        }
    }
}