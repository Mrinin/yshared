using UnityEngine;

namespace YShared.Console.Suggestions
{
    public struct Suggestion 
    {
        public string Word;
        public SuggestionType SuggestionType;
        public Color SuggestedColor;

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
    }

    public enum SuggestionType { Command, Parameter }
}
