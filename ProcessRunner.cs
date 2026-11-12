using System.IO;
using System.Diagnostics;
using System.Text;

namespace CreationStudio2026;

/// <summary>Runs a Windows command, streams output, supports stdin and process-tree termination.</summary>
public sealed class ProcessRunner
{
    private readonly object _sync = new();
    private Process? _process;

    public bool IsRunning
    {
        get
        {
            lock (_sync)
                return _process is { HasExited: false };
        }
    }

    public async Task<int> RunAsync(string command, string workDir, Action<string> onOutput)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(workDir);
        ArgumentNullException.ThrowIfNull(onOutput);

        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            WorkingDirectory = Directory.Exists(workDir) ? workDir : Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("/d");
        startInfo.ArgumentList.Add("/s");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(command);
        startInfo.Environment["PYTHONUNBUFFERED"] = "1";
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) onOutput(e.Data + Environment.NewLine);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) onOutput(e.Data + Environment.NewLine);
        };

        lock (_sync)
        {
            if (_process is { HasExited: false })
                throw new InvalidOperationException("A process is already running.");
            _process = process;
        }

        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Windows could not start the command process.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            return process.ExitCode;
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_process, process))
                    _process = null;
            }
            process.Dispose();
        }
    }

    public void SendInput(string text)
    {
        lock (_sync)
        {
            try
            {
                if (_process is { HasExited: false })
                    _process.StandardInput.WriteLine(text);
            }
            catch (InvalidOperationException) { }
            catch (IOException) { }
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            try
            {
                if (_process is { HasExited: false })
                    _process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        }
    }
}
