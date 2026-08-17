using System;

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
}