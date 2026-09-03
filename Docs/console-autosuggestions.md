# Suggestion Modifiers

Suggestion modifiers are a list of attributes that modify how the autocompletion list (the suggestions) are generated when entering commands through YConsole.

## Exclude

By default, most value types automatically autocomplete to a list of suggestions that make sense for that value type. For example, the following enum,

```csharp
public enum Gamemode
{
    Unset,
    Survival,
    Adventure,
    Creative,
    Spectator   
}
```

automatically knows that it can autocomplete to those 5 values. So if you have a command such as,

```csharp
public class MyPlayer : MonoBehaviour
{
    [YCommand("gamemode", "Change the current player's gamemode.")]
    public int ChangeGamemode(Gamemode gm)
    {
        // Complicated Gamemode change logic here
    }
}
```

YConsole will conveniently suggest the possible, easy to read string values of that enum to you. You could also use the numerical values of the enum to use this command if you want, but YConsole makes it easy by providing you with the list and convenience of autocompletion.

But there is something missing here! Or rather, there is something extra. In our game, `Gamemode.Unset` is the "unassigned" gamemode. Let's say it's used when the player first loads in to our server, but it's not a gamemode we are ever meant to be in to play the actual game. How can we prevent `Unset` from being selectable in this command?

By simply using `[Exclude(nameof(Gamemode.Unset))]` as a parameter attribute.

```csharp
public class MyPlayer : MonoBehaviour
{
    [YCommand("gamemode", "Change the current player's gamemode.")]
    public int ChangeGamemode( [Exclude(nameof(Gamemode.Unset))] Gamemode gm)
    {
        // Complicated Gamemode change logic here... but now without the possibility of Unset
    }
}
```

"Unset" is no longer suggested when trying to use the command "gamemode". (Of course, `[Exclude("Unset")]` would have also worked.)

Exclude accepts a `param string[] exclude_list`, so you can put as many excluded strings in there as you want.


## Include

```csharp
public class MyPlayer : MonoBehaviour
{
    [YCommand("set_name", "Change the current player's current name")]
    public string SetName( [Include("Player A", "Player B", "Player C")] string new_name)
    {
        // Complicated Gamemode change logic here
    }
}
```


| Value | Description |
| --- | --- |
| `[Include(include_list: string[])]` | Adds the list of strings to suggestions. |
| `[Exclude(exclude_list: string[])]` | Removes the list of strings from the suggestions. |
| `[Override(override_list: string[])]` | Overrides the list of suggestions with the given list. |
| `[NoSuggestions]` | Removes all suggestions. |
| `[SuggestionRange(int start_inclusive, int end_exclusive, int step = 1)]` | Generates and adds a suggestions list of all integers from `start_inclusive` to `end_exclusive` in increments of `step`. |
| `[IncludeDynamic(functionName: string, bool cacheCompatible = false)]` | Takes the name of a static function that takes no parameters and returns a string array or IEnumerable<string>. At runtime this function is called to generate a suggestion list and is added to existing suggestions. If `cacheCompatible` is false, this function is called every time this command is typed, if true, this function is called once and the suggestion list is cached. |