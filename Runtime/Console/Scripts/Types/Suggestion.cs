using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YShared.Console.Suggestions
{
    public struct Suggestion 
    {
        public string Word;
        public SuggestionType SuggestionType;
        public Color SuggestedColor;
        public bool Underline;

        public static Suggestion Subcommand(string Word)
        {
            Suggestion s = new();
            s.Word = Word;
            s.SuggestionType = SuggestionType.Command;
            s.SuggestedColor = Color.white;
            return s;
        }

        public static Suggestion Parameter(string Word, Color suggested_color)
        {
            Suggestion s = new();
            s.Word = Word;
            s.SuggestionType = SuggestionType.Parameter;
            s.SuggestedColor = suggested_color;
            return s;
        }

        public static List<Suggestion> CreateParameterSuggestionArray(List<string> arr, Color suggestColor)
        {
            if (arr == null)
                return null;

            Suggestion[] suggestions = new Suggestion[arr.Count];
            for (int i = 0; i < arr.Count; i++)
            {
                suggestions[i] = Suggestion.Parameter(arr[i], suggestColor);
            }
            return suggestions.ToList();
        }
    }

    public enum SuggestionType { Command, Parameter }
}
