using System;
using UnityEngine;

namespace YShared.Console
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class YArgumentAttribute : Attribute
    {
        public string Name;
        public string Description;

        public YArgumentAttribute(string Name, string Description = "")
        {
            this.Name = Name;
            this.Description = Description;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class YRangeAttribute : Attribute
    {
        public int min;
        public int max;

        public YRangeAttribute(int min, int max)
        {
            this.min = min;
            this.max = max;
        }
    }
}