using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace YShared.Console.Suggestion
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class SuggestionModifierAttribute: System.Attribute
    {
        public virtual bool cacheCompatible => true;
        public abstract void modifyAutocompleteList(List<string> strings);
        public MemberInfo thisMethodOrField { get; set; }
    }

    /// <summary>
    /// Adds the <c>include_list</c> to the list of possible suggestions.
    /// </summary>
    
    public class IncludeAttribute: SuggestionModifierAttribute
    {
        string[] include_list;

        public override void modifyAutocompleteList(List<string> strings)
        {
            strings.AddRange(include_list);
        }

        public IncludeAttribute(params string[] include_list)
        {
            this.include_list = include_list;
        }
    }

    /// <summary>
    /// Removes the <c>exclude_list</c> to the list of possible suggestions.
    /// </summary>
    
    public class ExcludeAttribute: SuggestionModifierAttribute
    {
        string[] exclude_list;

        public override void modifyAutocompleteList(List<string> strings)
        {
            strings.Except(exclude_list);
        }

        public ExcludeAttribute(params string[] exclude_list)
        {
            this.exclude_list = exclude_list;
        }
    }

    /// <summary>
    /// Replaces the existing list of possible suggestions with <c>override_list</c>.
    /// </summary>
    
    public class OverrideAttribute: SuggestionModifierAttribute
    {
        string[] override_list;

        public override void modifyAutocompleteList(List<string> strings)
        {
            strings = override_list.ToList();
        }

        public OverrideAttribute(params string[] override_list)
        {
            this.override_list = override_list;
        }
    }

    /// <summary>
    /// Removes the suggestion list.
    /// </summary>
    
    public class NoSuggestionsAttribute: SuggestionModifierAttribute
    {

        public override void modifyAutocompleteList(List<string> strings)
        {
            strings.Clear();
        }

        public NoSuggestionsAttribute()
        {
            
        }
    }

    /// <summary>
    /// <para> Adds the values in the given range to the suggestion list. The start is inclusive, the end is exclusive.</para> 
    /// <para> Example Usage: <c>[SuggestionRangeAttribute(1, 13, 2)]</c></para> 
    /// <para> The values, <c>"1", "3", "5", "7", "9", "11"</c> </para> 
    /// </summary>
    
    public class SuggestionRangeAttribute: SuggestionModifierAttribute
    {
        string[] include_list;

        public override void modifyAutocompleteList(List<string> strings)
        {
            strings.AddRange(include_list);
        }

        /// <summary>
        /// <para> Adds the values in the given range to the suggestion list. The start is inclusive, the end is exclusive.</para> 
        /// <para> Example Usage: <c>[SuggestionRangeAttribute(1, 13, 2)]</c></para> 
        /// <para> The values, <c>"1", "3", "5", "7", "9", "11"</c> </para> 
        /// </summary>
        /// <param name="start_inclusive">Start. Inclusive</param>
        /// <param name="end_exclusive">End. Exclusive.</param>
        /// <param name="step">Step number. Defaults to 1.</param>
        public SuggestionRangeAttribute(int start_inclusive, int end_exclusive, int step = 1)
        {
            List<string> strings = new List<string>((end_exclusive - start_inclusive) / step + 1);
            for (int i = start_inclusive; i < end_exclusive; i += step)
            {
                strings.Add(i.ToString());
            }
        }
    }
}
