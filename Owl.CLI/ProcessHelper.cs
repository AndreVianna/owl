namespace Owl.CLI;

internal static class ProcessHelper
{
    public static bool ProcessExists(string processName)
    {
        try
        {
            return Process.GetProcessesByName(processName).FirstOrDefault() != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking if service '{processName}' is running: {ex.Message}");
            return false;
        }
    }

    public static bool TryStartProcess(string processName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start {processName}.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            return Process.Start(psi) != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting '{processName}' process: {ex.Message}");
            return false;
        }
    }

    public static bool TryStopProcess(string processName)
    {
        try
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                process.Kill();
            }

            Console.WriteLine($"'{processName}' process stopped.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping '{processName}' process: {ex.Message}");
            return false;
        }
    }
}
