using UnityEngine;
using System;
using System.Linq;

namespace YShared.Console
{
    /// <summary>
    /// <b>Define a new command. The function MUST be static.</b>
    /// <list type="bullet">
    ///     <item>
    ///         Use <c>YCInt(min, max)</c> for integer arguments. (min / max optional)
    ///     </item>
    ///     <item>
    ///         Use <c>YCFloat(min, max)</c> for float arguments. (min / max optional)
    ///     </item>
    ///     <item>
    ///         Use <c>YCEnum(typeof(Enum))</c> for enum arguments.
    ///     </item>
    ///     <item>
    ///         Use <c>YCBool</c> for boolean arguments.
    ///     </item>
    ///     <item>
    ///         Use <c>YCString</c> for string arguments.
    ///     </item>
    ///     <item>
    ///         Use <c>YCArgCmd</c> to accept the name of another command as an argument.
    ///     </item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class YCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Desc { get; }

        public YCommandAttribute(string Name, string Description = "")
        {
            this.Name = Name;
            this.Desc = Description;
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class YCmdArgumentAttribute : Attribute
    {
        public string variableName;
        public abstract string getDescriptionText();
        public abstract string getTypeName { get; }

        public abstract bool hasAutocompleteArray { get; }
        public virtual string[] getAutocompleteArray() { return null; }

        protected virtual bool Validate<T>(T s) => true ;
        public abstract bool Parse<T>(string s, out T val);
    }


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

    public sealed class YCCmdArg: YCmdArgumentAttribute
    {
        public override string getDescriptionText()
        {
            return $"{variableName}:command";
        }
        public override string getTypeName => $"command";
        public override bool hasAutocompleteArray => true;
        public override string[] getAutocompleteArray()
        {
            return DevConsole.CommandArray;
        }

        // Unused for this argument type
        protected override bool Validate<T>(T s) => true;

        public override bool Parse<T>(string s, out T val)
        {
            val = default;

            if (DevConsole.commands.TryGetValue(s, out var cmd))
            {
                val = (T)(object)cmd;

                return true;
            }


            return false;
        }

        public YCCmdArg(string varname)
        {
            variableName = varname;
        }
    }
}