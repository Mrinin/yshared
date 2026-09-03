using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace YShared.Console.Suggestion
{
    public abstract class DynamicSuggestionModifierAttribute: SuggestionModifierAttribute
    {
        protected bool isCacheCompatible = false;
        public override bool cacheCompatible => isCacheCompatible;
    }

    /// <summary>
    /// Adds the <c>include_list</c> to the list of possible suggestions.
    /// </summary>
    public class IncludeDynamicAttribute: DynamicSuggestionModifierAttribute
    {
        string functionName;
        MethodInfo dynamicStringArrayMethod;

        public override void modifyAutocompleteList(List<string> strings)
        {
            if (dynamicStringArrayMethod == null)
            {
                Type declaringType = thisMethodOrField.DeclaringType;

                dynamicStringArrayMethod = declaringType.GetMethod(functionName);
            }

            if (dynamicStringArrayMethod == null)
            {
                return;
            }

            if (!dynamicStringArrayMethod.IsStatic)
            {
                Debug.LogError("Name of the method used in IncludeDynamic must be of a static method!");
                return;
            }

            try
            {    
                object str = dynamicStringArrayMethod.Invoke(null, null);

                if (str is string[] strarray)
                {
                    strings.AddRange(strarray);
                    return;
                }

                if (str is IEnumerable<string> strenumerable)
                {
                    strings.AddRange(strenumerable);
                    return;
                }

            } catch (TargetInvocationException e)
            {
                throw (e.InnerException ?? e);
            }
            catch (TargetParameterCountException)
            {
                Debug.LogError("Name of the method used in IncludeDynamic must have no parameters!");
            }
            catch (ArgumentException)
            {
                Debug.LogError("Name of the method used in IncludeDynamic must have no parameters!");
            }
        }

        public override bool cacheCompatible => isCacheCompatible;

        public IncludeDynamicAttribute(string functionName, bool cacheCompatible = false)
        {
            this.functionName = functionName;
            this.isCacheCompatible = cacheCompatible;
        }
    }
}
