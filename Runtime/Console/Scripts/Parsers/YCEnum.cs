using System;
using System.Linq;
namespace YShared.Console
{
    /// <summary>
    /// Use <c>YCEnum(typeof(Enum))</c> for enum arguments.
    /// </summary>

   public sealed class YCEnum: YCmdParser<Enum>
    {
        public Type enumType { get; private set; }
        int enumTypeValueAmount;
        string[] enumValues;

        public override string getDescriptorText()
        {
            return $"{variableName}:enum({enumType.ToString()})";
        }
        public override string getTypeName => $"enum({enumType.ToString()})";
        public override bool hasDefaultAutocompleteArray => enumTypeValueAmount <= 50;
        public override string[] defaultAutocompleteArray() { return enumValues; }

        protected override bool Validate(Enum s) 
        {
            if (s.GetType() == enumType)
            {
                return true;
            }

            return false;
        }

        public override bool Parse(string s, out Enum val)
        {
            val = default;

            // this also automatically parses int inputs
            if (Enum.TryParse(enumType, s, true, out object result))
            {
                Enum enumresult = (Enum)result;
                val = enumresult;

                if (Validate(enumresult))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetEnumType(Type enumtype)
        {
            this.enumType = enumtype;

            string[] arr = enumtype.GetEnumNames();
            arr = arr.OrderBy(key => key.ToString(), StringComparer.CurrentCultureIgnoreCase).ToArray();

            enumTypeValueAmount = arr.Length;
            if (hasDefaultAutocompleteArray)
            {
                enumValues = arr;
            }
        }
    }
}