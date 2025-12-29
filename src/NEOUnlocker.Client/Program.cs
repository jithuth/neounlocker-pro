using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NEOUnlocker.Client.Services;

namespace NEOUnlocker.Client;

/// <summary>
/// Application entry point with dependency injection.
/// </summary>
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        var app = new App();
        
        // Get main window from DI container
        var mainWindow = host.Services.GetRequiredService<MainWindow>();
        
        app.Run(mainWindow);
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Register HttpClient
                services.AddHttpClient<IFlashClient, FlashClient>();
                
                // Register application services
                services.AddSingleton<IKeyManagementService, KeyManagementService>();
                services.AddSingleton<IHWIDService, HWIDService>();
                services.AddSingleton<INativeToolExecutor, NativeToolExecutor>();
                services.AddTransient<IFlashClient, FlashClient>();
                
                // Register main window
                services.AddTransient<MainWindow>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            });
}
