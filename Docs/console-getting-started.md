# Quick Start

Welcome to **YShared**, a feature complete Dev Console system for Unity.

To get started, add the attribute [`YCommand`](xref:YShared.Console.YCommandAttribute) to any function.

```csharp
public class MyConsoleCommands : MonoBehaviour
{
    [YCommand]
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```

and that's it! The command `"add a:int b:int"` is now added to the console.

## YCommand Arguments

If you want the name of the console command to be different than the name of the function, you may change the name. You can also add a command description, which will be shown when:
- Using the `help` command
- Disambiguating between commands with the same name but different signature
- Listing the usage of multiple subcommands
- The user is informed there is an error in their input.

```csharp
public class MyConsoleCommands : MonoBehaviour
{
    [YCommand("multiply", "Multiply two numbers together and return the output.")]
    public int SuperComplicatedMathematicalOperation(int a, int b)
    {
        return a * b;
    }
}
```

If the function is a static method, the command will simply run without an instance.

If the function is an instance method, an [`ObjectFindType`](xref:YShared.Console.ObjectFindType) value can be given to tell YConsole how to find the object to run this command on. By default, `ObjectFindType.All` is used.

| Value | Description |
| --- | --- |
| `All` | Finds **all** objects of this class and calls the method on each of them. Internally uses `GameObject.FindObjectsByType`. |
| `Any` | Finds **any** object of this class and calls the method on it. Internally uses `GameObject.FindAnyByType`. |
| `First` | Finds the **first** object of this class and calls the method on it. Internally uses `GameObject.FindFirstByType`. |
| `Argument` | Uses the **first argument** of the command as the instance to run the method on. The first argument does not need to be explicitly typed as the class; if it is, YConsole automatically fills it in. |
| `ArgumentArray` | Uses the **first argument** of the command as an array of instances to run the method on. The first argument does not need to be explicitly typed as an array of the class; if it is, YConsole automatically fills it in. |