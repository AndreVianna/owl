namespace Owl.CLI;

internal class StopCommand : ChildCommand
{
    public StopCommand(string[] args) : base("stop", args)
    {
    }

    protected override string Usage => """
         Usage: owl stop [options]
 
         Options:
                -h, --help   Show help information for the stop command.
         
         """;

    protected override bool TryExecute()
    {
        Console.WriteLine($"Stopping '{ServiceName}' process...");
        var hasStopped = ProcessHelper.TryStopProcess(ServiceName);
        ExitCode = hasStopped ? ExitCode.Success : ExitCode.InternalError;
        return hasStopped;
    }
}