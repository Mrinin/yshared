using System;
using UnityEngine;

namespace YShared.Console
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]

    public class YToggleAttribute : Attribute
    {
        public YToggleAttribute()
        {
            
        }
    }
}