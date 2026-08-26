using System;
namespace YShared.Console
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class YCmdArgumentAttribute : Attribute
    {
        /// <summary>
        /// Name of the argument used when previewing command arguments.
        /// </summary>
        public string variableName;
        /// <summary>
        /// Returns the name and type information of this argument in a nice, descriptive way.
        /// </summary>
        /// /// <returns>Descriptor</returns>
        public abstract string getDescriptorText();

        /// <summary> The string name of this argument's type</summary>
        public abstract string getTypeName { get; }

        /// <summary> Check for whether this argument has an autcomplete array. If it returns <c>true</c>, <c>getAutocompleteArray</c> is called to retrieve the list. </summary>
        public abstract bool hasAutocompleteArray { get; }
        /// <summary>
        /// The autocomplete array used for this argument. Optionally return "null" for no autocomplete.
        /// </summary>
        /// <returns>Autocomplete Array</returns>
        public virtual string[] getAutocompleteArray() { return null; }

        /// <summary>
        /// Validate type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns>IsValid</returns>
        protected virtual bool Validate<T>(T s) => true ;

        /// <summary>
        /// Return the thing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public abstract bool Parse<T>(string s, out T val);
    }
}