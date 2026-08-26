using System;
using UnityEngine;

namespace YShared.Console
{
    /// <summary>
    /// <b>Define a new command.</b>
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
    ///         Use <c>YCCmdArg</c> to get registered commands as an input. (Use Command[])
    ///     </item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class YCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Desc { get; }
        public ObjectFindType objectFindType { get; }
        public FindObjectsInactive findObjectsInactive { get; }

        /// <summary>
        /// <b>Define a new command.</b>
        /// </summary>
        /// <param name="Name">The name of the command. This will be the name used in the console.</param>
        /// <param name="Description">(Optional) Description of the command, shown when using the "help" command.</param>
        /// <param name="objectFindType">(Optional) Used only when the function is not static to determine which objects to run the method on.</param>
        /// <param name="findObjectsInactive">(Optional) Used only when the function is not static to determine whether non-active objects should be able to run the method.</param>
        public YCommandAttribute(string Name, string Description = "", ObjectFindType objectFindType = ObjectFindType.All, FindObjectsInactive findObjectsInactive = FindObjectsInactive.Exclude)
        {
            this.Name = Name;
            this.Desc = Description;
            this.objectFindType = objectFindType;
            this.findObjectsInactive = findObjectsInactive;
        }

        public YCommandAttribute(string NameAddendum, YCommandAttribute @base)
        {
            this.Name = @base.Name + " " + NameAddendum;
            this.Desc = @base.Desc;
            this.objectFindType = @base.objectFindType;
            this.findObjectsInactive = @base.findObjectsInactive;
        }
    }
}