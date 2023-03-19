namespace Owl.CLI;

abstract class CommandBase : ICommand
{
    protected CommandBase(string name, string[] args)
    {
        Name = name;
        Arguments = args;
    }

    public string Name { get; }
    protected string[] Arguments { get; }

    public abstract int Execute();
    protected abstract string Usage { get; }
    protected abstract bool TryExecute();
    // Allow each command to define the list of valid sub-commands.
    protected virtual ICommand? CreateSubCommand() => null; // No sub-command by default;
}