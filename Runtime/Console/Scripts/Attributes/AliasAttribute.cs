using System;

namespace YShared.Console
{
    /// <summary>
    /// Define an alias / shortcut for the command. You may also optionally provide arguments.
    /// <list type="bullet">
    ///     <item>
    ///         Shorthand - Shorthand for the command. (ie, Shortcut: "gm", for the command "gamemode")
    ///     </item>
    ///     <item>
    ///         Arguments - If provided, the shortcut will be ran with those arguments. (ie, Shorthand: "gmc" and Arguments: "creative" to run "gamemode creative")
    ///     </item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AliasAttribute : Attribute
    {
        public string Shorthand { get; }
        public string Arguments { get; }

        public AliasAttribute(string Shorthand, string Arguments = "")
        {
            this.Shorthand = Shorthand;
            this.Arguments = Arguments;
        }
    }
}