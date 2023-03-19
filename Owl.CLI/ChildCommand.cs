namespace Owl.CLI;

abstract class ChildCommand : CommandBase
{
    protected ChildCommand(string name, string[] args) : base(name, args)
    {
    }

    public sealed override int Execute()
    {
        if (Arguments.Length > 0 && (Arguments[0] == "-h" || Arguments[0] == "--help"))
        {
            ShowHelp(Usage);
            return 1;
        }

        if (TryExecute())
        {
            return 1;
        }

        ShowHelp(Usage, $"Invalid option for command '{Name}'. Use \"-h\" or \"--help\" for help.");
        return 0;
    }
}