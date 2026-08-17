using System;
namespace YShared.Console
{
    /// <summary>
    /// Use <c>YCEnum(typeof(Enum))</c> for enum arguments.
    /// </summary>

   public sealed class YCEnum: YCmdArgumentAttribute
    {
        public Type enumType { get; private set; }
        int enumTypeValueAmount;
        string[] enumValues;

        public override string getDescriptionText()
        {
            return $"{variableName}:enum({enumType.ToString()})";
        }
        public override string getTypeName => $"enum({enumType.ToString()})";
        public override bool hasAutocompleteArray => enumTypeValueAmount <= 50;
        public override string[] getAutocompleteArray() { return enumValues; }

        protected override bool Validate<T>(T s) 
        {
            if (s.GetType() == enumType)
            {
                return true;
            }

            return false;
        }

        public override bool Parse<T>(string s, out T val)
        {
            val = default;

            // this also automatically parses int inputs
            if (Enum.TryParse(enumType, s, true, out object result))
            {
                Enum enumresult = (Enum)result;
                val = (T)result;

                if (Validate<Enum>(enumresult))
                {
                    return true;
                }
            }

            return false;
        }

        public YCEnum(string varname, Type enumtype)
        {
            variableName = varname;

            enumType = enumtype;

            string[] arr = enumtype.GetEnumNames();

            enumTypeValueAmount = arr.Length;
            if (hasAutocompleteArray)
            {
                enumValues = arr;
            }
        }
    }
}