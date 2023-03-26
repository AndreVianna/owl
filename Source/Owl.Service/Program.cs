Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(builder => builder.AddUserSecrets<Program>())
    .UseWindowsService()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<IConsoleWindow, ConsoleWindow>();
        services.AddSingleton<IConnectedConsole, ConnectedConsole>();
        services.AddSingleton<ITimestampedFile, TimestampedFile>();
        services.AddSingleton<IRecorder, Recorder>();
        services.AddSingleton<ITranscriptionProvider>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var recorder = provider.GetRequiredService<IRecorder>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var type = configuration["TranscriptionProvider:Type"];
            return type switch
            {
                "Google" => new GoogleTranscriptionProvider(configuration, recorder, loggerFactory),
                _ => throw new NotImplementedException($"Transcription provider '{type}' is not supported.")
            };
        });

        services.AddHostedService<Worker>();
    })
    .UseSerilog(configureLogger: (builder, config) => config.ReadFrom.Configuration(builder.Configuration))
    .Build()
    .Run();
