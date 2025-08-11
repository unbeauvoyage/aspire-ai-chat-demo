using System.Diagnostics;

namespace MyApi.Services;

public class OpenApiGeneratorService : IHostedService, IDisposable
{
    private readonly ILogger<OpenApiGeneratorService> _logger;
    private readonly IHostEnvironment _environment;
    private Process? _nodemonProcess;

    public OpenApiGeneratorService(
        ILogger<OpenApiGeneratorService> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogInformation("Skipping OpenAPI generator in non-development environment");
            return Task.CompletedTask;
        }

        var nextAppPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "nextapp"));
        
        if (!Directory.Exists(nextAppPath))
        {
            _logger.LogWarning("Next.js app directory not found at {Path}", nextAppPath);
            return Task.CompletedTask;
        }

        _nodemonProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "npx",
                Arguments = "nodemon --watch ../MyApi/DTOs --ext cs --delay 2 --exec \"npm run generate-api-if-running\"",
                WorkingDirectory = nextAppPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _nodemonProcess.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                _logger.LogInformation("OpenAPI Generator: {Output}", args.Data);
        };

        _nodemonProcess.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                _logger.LogError("OpenAPI Generator Error: {Error}", args.Data);
        };

        try
        {
            _nodemonProcess.Start();
            _nodemonProcess.BeginOutputReadLine();
            _nodemonProcess.BeginErrorReadLine();
            _logger.LogInformation("Started OpenAPI generator watcher");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start OpenAPI generator watcher");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_nodemonProcess != null && !_nodemonProcess.HasExited)
        {
            try
            {
                _nodemonProcess.Kill(entireProcessTree: true);
                _logger.LogInformation("Stopped OpenAPI generator watcher");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping OpenAPI generator watcher");
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _nodemonProcess?.Dispose();
    }
}
