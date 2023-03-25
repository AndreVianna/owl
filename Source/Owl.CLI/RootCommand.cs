namespace Owl.CLI;

internal abstract class RootCommand : CommandBase
{
    protected RootCommand(string name, string[] args) : base(name, args)
    {
    }

    public sealed override int Execute()
    {
        if (Arguments.Length > 0 && (Arguments[0] == "-h" || Arguments[0] == "--help"))
        {
            ShowHelp(Usage);
            return (int)ExitCode.Success;
        }

        if (TryExecute())
        {
            return (int)ExitCode;
        }

        ShowHelp(Usage, "Invalid option or command. Use \"-h\" or \"--help\" for help.");
        return (int)ExitCode.InvalidArgument;
    }
}