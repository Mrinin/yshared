using System;
namespace YShared.Console
{
    /// <summary>
    /// Use <c>YCInt(min, max)</c> for integer arguments. (min / max optional)
    /// </summary>
    public sealed class YCInt: YCmdParser<int>
    {
        int min = int.MinValue;
        int max = int.MaxValue;

        public override string getDescriptorText()
        {
            if (min != int.MinValue || max != int.MaxValue)
            {
                if (max == int.MaxValue)
                    return $"{variableName}:int({min}..)";
                else if (min == int.MinValue)
                    return $"{variableName}:int(..{max})";
                else
                    return $"{variableName}:int({min}..{max})";
            }
            return $"{variableName}:int";
        }
        public override string getTypeName => "int";
        public override bool hasDefaultAutocompleteArray => false;

        protected override bool Validate(int s) 
        {
            int a = (int)(object)(s);

            return a >= min && a <= max;
        }

        public override bool Parse(string s, out int val)
        {
            val = default;
            
            if (int.TryParse(s, out int result))
            {
                val = (int)(object)result;

                if (Validate(result))
                {
                    return true;
                }
            }

            return false;
        }
    }
}