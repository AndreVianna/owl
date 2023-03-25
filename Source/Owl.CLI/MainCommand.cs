namespace Owl.CLI;

internal class MainCommand : RootCommand
{
    public MainCommand(string[] args) : base("main", args)
    {
    }

    protected override string Usage => """
         Usage: owl [options] <command>"
 
         Options:
                -h, --help   Show help information for the stop command.

         Commands:
                start        Start the owl_service process.
                stop         Stop the owl_service process.

         Use "owl <command> -h" or "owl <command> --help" for help with a specific command.
         
         """;

    protected override bool TryExecute()
    {
        var command = CreateSubCommand();
        if (command == null)
        {
            return false;
        }

        command.Execute();
        return true;
    }

    protected override ICommand? CreateSubCommand()
    {
        return Arguments.Length switch
        {
            0 => null,
            _ => Arguments[0] switch
            {
                "start" => new StartCommand(Arguments[1..]),
                "stop" => new StopCommand(Arguments[1..]),
                _ => base.CreateSubCommand()
            }
        };
    }
}