using System;
namespace YShared.Console
{
    //[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class YCmdParser
    {
        /// <summary>
        /// Name of the argument used when previewing command arguments.
        /// </summary>
        public string variableName;
        public string variableDescription;
        /// <summary>
        /// Returns the name and type information of this argument in a nice, descriptive way.
        /// </summary>
        /// /// <returns>Descriptor</returns>
        public abstract string getDescriptorText();

        /// <summary> The string name of this argument's type</summary>
        public abstract string getTypeName { get; }

        /// <summary> Check for whether this argument has an autcomplete array. If it returns <c>true</c>, <c>getAutocompleteArray</c> is called to retrieve the list. </summary>
        public abstract bool hasDefaultAutocompleteArray { get; }
        /// <summary>
        /// The default autocomplete array used for this value type. For example, "bool"s default array is ["true", "false"]. Optionally return "null" for no autocomplete.
        /// </summary>
        /// <returns>Autocomplete Array</returns>
        public virtual string[] defaultAutocompleteArray() { return null; }

        public void Initialize(string Name, string Description)
        {
            this.variableName = Name;
            this.variableDescription = Description;
        }

        public virtual Type ParsingType { get; }

        internal abstract bool ValidateUnsafe(object s);
        internal abstract bool ParseUnsafe(string s, out object val);
    }
    
    public abstract class YCmdParser<T>: YCmdParser
    {
        

        public override Type ParsingType => typeof (T);

        /// <summary>
        /// Validate type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns>IsValid</returns>
        protected virtual bool Validate(T s) => true;

        internal sealed override bool ValidateUnsafe(object s) { return Validate((T)s); }

        /// <summary>
        /// Return the thing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public abstract bool Parse(string s, out T val);

        internal sealed override bool ParseUnsafe(string s, out object val) 
        { 
            T val2;
            bool result = Parse(s, out val2);
            val = (object)val2;
            return result;
        }
    }
}