namespace Owl.CLI;

static class MessageHelper
{
    public static void ShowHelp(string helpMessage, string? errorMessage = null)
    {
        if (!string.IsNullOrEmpty(errorMessage))
        {
            Console.WriteLine(errorMessage);
            Console.WriteLine();
        }

        Console.WriteLine(helpMessage);
    }
}