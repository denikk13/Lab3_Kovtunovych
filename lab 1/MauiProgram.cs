using lab_1.Services;
using lab_1.ViewModels;
using Microsoft.Extensions.Logging;
using Serilog;

namespace lab_1;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Serilog
        var logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        var logFilePath = Path.Combine(logDirectory, "app-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Logging.AddSerilog(dispose: true);

        // Сервіси сесії та автентифікації
        builder.Services.AddSingleton<ISessionService, SessionService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();

        // ViewModels та сторінки
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<Views.LoginPage>();

        builder.Services.AddTransient<StudentViewModel>();
        builder.Services.AddTransient<MainPage>();

        builder.Services.AddTransient<Views.LogsPage>();

        return builder.Build();
    }
}