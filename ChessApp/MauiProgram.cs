using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace ChessApp;

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

		// Load configuration from appsettings.json and .env
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("ChessApp.appsettings.json");
		if (stream == null)
		{
			builder.Logging.AddConsole();
			builder.Logging.AddDebug();
			System.Diagnostics.Debug.WriteLine("ERROR: appsettings.json not found as embedded resource.");
		}
		else
		{
			var config = new ConfigurationBuilder()
				.AddJsonStream(stream)
				.AddEnvironmentVariables()
				.Build();
			builder.Configuration.AddConfiguration(config);
		}

		// Add logging
		builder.Logging.AddConsole();

		return builder.Build();
	}
}
