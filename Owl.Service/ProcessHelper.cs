namespace Owl.Service;

internal static class ProcessHelper
{
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
