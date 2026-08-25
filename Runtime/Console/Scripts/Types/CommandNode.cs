using System.Collections.Generic;

namespace YShared.Console
{
    public class CommandNode
    {
        public Dictionary<string, CommandNode> children = new();

        // Commands that terminate at this node.
        public List<Command> commands = new();

        public void Clear()
        {
            commands.Clear();
            children.Clear();
        }
    }
}