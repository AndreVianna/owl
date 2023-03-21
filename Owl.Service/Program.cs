Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(builder => builder.AddUserSecrets<Program>())
    .UseWindowsService() // For Windows systems, enables running as a Windows Service
    .ConfigureServices((_, services) => services.AddHostedService<Worker>())
    .UseSerilog((_, loggerConfiguration) => loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.File("logs/owl_.log", 
                rollingInterval: RollingInterval.Hour, 
                retainedFileCountLimit: 31,
                outputTemplate: "[{Timestamp:yyyy-MM-ddTHH:mm:ss.ffffff}] {Message}{NewLine}{Exception}"))
    .Build()
    .Run();
