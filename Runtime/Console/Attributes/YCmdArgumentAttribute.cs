using System;
namespace YShared.Console
{
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
}