namespace Owl.CLI;

internal class StartCommand : ChildCommand
{
    public StartCommand(string[] args) : base("start", args)
    {
    }

    protected override bool TryExecute()
    {
        if (ProcessHelper.ProcessExists(ServiceName))
        {
            Console.WriteLine($"The {ServiceName} process is already running.");
            ExitCode = ExitCode.Success;
            return true;
        }

        Console.WriteLine($"Starting '{ServiceName}' process...");
        var hasStarted = ProcessHelper.TryStartProcess(ServiceName);
        if (hasStarted)
        {
            Console.WriteLine($"'{ServiceName}' started...");
        }

        ExitCode = hasStarted ? ExitCode.Success : ExitCode.InternalError;
        return hasStarted;
    }


    protected override string Usage => """
         Usage: owl start [options]
         Options:
                -h, --help   Show help information for the start command.
         
         """;
}