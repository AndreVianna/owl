using Owl.Service;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService() // For Windows systems, enables running as a Windows Service
    .ConfigureServices((hostContext, services) => services.AddHostedService<Worker>())
    .Build();

host.Run();
