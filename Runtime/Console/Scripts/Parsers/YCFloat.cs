using System;
namespace YShared.Console
{

    /// <summary>
    /// Use <c>YCFloat(min, max)</c> for float arguments. (min / max optional)
    /// </summary>
    public sealed class YCFloat: YCmdParser<float>
    {
        float min = float.MinValue;
        float max = float.MaxValue;

        public override string getDescriptorText()
        {
            if (min != float.MinValue || max != float.MaxValue)
            {
                return $"{variableName}:float({min},{max})";
            }
            return $"{variableName}:float";
        }
        public override string getTypeName => "float";
        public override bool hasDefaultAutocompleteArray => false;

        protected override bool Validate(float s) 
        {
            float a = (float)(object)(s);

            return a >= min && a <= max;
        }

        public override bool Parse(string s, out float val)
        {
            val = default;
            
            if (float.TryParse(s, out float result))
            {
                val = (float)(object)result;

                if (Validate(result))
                {
                    return true;
                }
            }

            return false;
        }
    }
}