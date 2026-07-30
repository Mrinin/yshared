using UnityEngine;
using System;
using UnityEngine.InputSystem.Controls;
using Codice.CM.SEIDInfo;

namespace YShared.Console
{
    public class Test
    {
        /*[YCommand("givecard", "Give Card Test")]
        [YCInt("CardId")]
        [YCBool("Animate")]
        public static void givecard(int id, bool animate)
        {
            DevConsole.Feedback($"Gave card {id} with {animate}");
        }

        [YCommand("enumtest", "Test Physicts Constirant adawjk")]
        [YCEnum("Enum", typeof(RigidbodyConstraints2D))]
        public static void givecard(RigidbodyConstraints2D rb2d)
        {
            DevConsole.Feedback($"Set constraints to {rb2d}");
        }*/
    }


    /// <summary>
    /// Use: YCInt(min,max), YCFloat(min,max), YCEnum(typeof Enum), YCBool or YCString after YCommand to add arguments.
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
        public virtual bool Validate<T>(T s) => true ;
        public abstract bool Parse<T>(string s, out T val);
    }


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

        public override bool Validate<T>(T s) 
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

    public sealed class YCFloat: YCmdArgumentAttribute
    {
        float min, max;

        public override string getDescriptionText()
        {
            if (min != float.MinValue || max != float.MaxValue)
            {
                return $"{variableName}:int({min},{max})";
            }
            return $"{variableName}:int";
        }

        public override bool Validate<T>(T s) 
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

    public sealed class YCBool: YCmdArgumentAttribute
    {
        public override string getDescriptionText()
        {
            return $"{variableName}:bool";
        }

        public override bool Validate<T>(T s) => true;

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

    public sealed class YCString: YCmdArgumentAttribute
    {
        public override string getDescriptionText()
        {
            return $"{variableName}:string";
        }

        public override bool Validate<T>(T s) => true;

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

    public sealed class YCEnum: YCmdArgumentAttribute
    {
        public Type enumType { get; private set; }

        public override string getDescriptionText()
        {
            return $"{variableName}:enum({enumType.ToString()})";
        }

        public override bool Validate<T>(T s) 
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

            if (int.TryParse(s, out int intval))
            {
                
            }

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
        }
    }
}