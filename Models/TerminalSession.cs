using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AgentBlazor.Models;
public class TerminalSession : IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StringBuilder _outputBuffer = new();
    private readonly object _lock = new();

    private readonly Queue<TaskCompletionSource<string>> _pendingResults = new();
    private readonly StringBuilder _currentCommandOutput = new();

    public event Action<string>? OnOutput;

    public TerminalSession(string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.FileName = "cmd.exe";
            psi.Arguments = "/K";
        }
        else
        {
            psi.FileName = "/bin/bash";
            psi.Arguments = "-i";
        }

        _process = new Process { StartInfo = psi };
        _process.OutputDataReceived += OnDataReceived;
        _process.ErrorDataReceived += OnDataReceived;
        _process.Start();

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _stdin = _process.StandardInput;
    }

    private void OnDataReceived(object? sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;

        lock (_lock)
        {
            _outputBuffer.AppendLine(e.Data);
            _currentCommandOutput.AppendLine(e.Data);

            if (_pendingResults.TryPeek(out var tcs))
            {
                if (e.Data.Contains("__END_OF_COMMAND__"))
                {
                    // Signal command completion
                    _pendingResults.Dequeue();
                    var result = _currentCommandOutput.ToString();
                    _currentCommandOutput.Clear();
                    tcs.TrySetResult(result.Replace("__END_OF_COMMAND__", "").Trim());
                }
            }
        }

        OnOutput?.Invoke(e.Data);
    }

    public async Task<string> ExecuteCommandAsync(string command, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock)
        {
            _pendingResults.Enqueue(tcs);
        }

        string sentinel = $"__END_OF_COMMAND__{Guid.NewGuid():N}";
        string fullCommand = $"{command} && echo {sentinel}";

        await _stdin.WriteLineAsync(fullCommand);
        await _stdin.FlushAsync();

        using (token.Register(() => tcs.TrySetCanceled()))
        {
            return await tcs.Task;
        }
    }

    public async Task SendInputAsync(string input)
    {
        await _stdin.WriteLineAsync(input);
        await _stdin.FlushAsync();
    }

    public void Dispose()
    {
        try { _stdin.WriteLine("exit"); } catch { }
        _process.Kill(entireProcessTree: true);
        _process.Dispose();
    }
}