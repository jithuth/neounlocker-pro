using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NEOUnlocker.Client.Forms;
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
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        var host = CreateHostBuilder(args).Build();

        // Get main form from DI container
        var mainForm = host.Services.GetRequiredService<MainForm>();

        Application.Run(mainForm);
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
                // Register application services
                services.AddSingleton<ISerialPortService, SerialPortService>();
                services.AddSingleton<IFastbootService, FastbootService>();
                services.AddSingleton<IRouterService, RouterService>();

                // Register main form
                services.AddTransient<MainForm>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            });
}
