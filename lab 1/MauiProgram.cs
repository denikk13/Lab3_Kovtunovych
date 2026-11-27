using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace lab_1
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            var logFilePath = Path.Combine(logDirectory, "app-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            RegisterGlobalExceptionHandlers();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(dispose: true);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<Services.IAppStateService, Services.AppStateService>();

            return builder.Build();
        }

        private static void RegisterGlobalExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    Log.Fatal(ex, "Unhandled AppDomain exception (IsTerminating: {IsTerminating})", args.IsTerminating);
                }
                else
                {
                    Log.Fatal("Unhandled non-CLR exception: {ExceptionObject}", args.ExceptionObject);
                }

                Log.CloseAndFlush();
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Log.Error(args.Exception, "Unobserved task exception");
                args.SetObserved();
            };
        }
    }
}