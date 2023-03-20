Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(builder => builder.AddUserSecrets<Program>())
    .UseWindowsService() // For Windows systems, enables running as a Windows Service
    .ConfigureServices((_, services) => services.AddHostedService<Worker>())
    .UseSerilog((_, loggerConfiguration) => loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.File("app.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31))
    .Build()
    .Run();
