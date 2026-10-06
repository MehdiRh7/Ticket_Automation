using System.Diagnostics;
using System.Text;

namespace TicketAutomation.Web.Services;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError, TimeSpan Duration)
{
    public bool Succeeded => ExitCode == 0;
}

public sealed class ProcessRunner
{
    public async Task<ProcessResult> RunAsync(string executable, IEnumerable<string> arguments, string workingDirectory,
        string? standardInput, TimeSpan timeout, CancellationToken ct, IReadOnlyDictionary<string, string?>? environment = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput is not null,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        if (environment is not null)
        {
            foreach (var item in environment)
            {
                if (item.Value is null) startInfo.Environment.Remove(item.Key);
                else startInfo.Environment[item.Key] = item.Value;
            }
        }

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };
        var stopwatch = Stopwatch.StartNew();
        if (!process.Start()) throw new InvalidOperationException($"اجرای {executable} ممکن نشد.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput);
            process.StandardInput.Close();
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            if (ct.IsCancellationRequested) throw;
            throw new TimeoutException($"اجرای {executable} پس از {timeout.TotalMinutes:0} دقیقه متوقف شد.");
        }
        stopwatch.Stop();
        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString(), stopwatch.Elapsed);
    }
}

public static class CommandLineTokenizer
{
    public static IReadOnlyList<string> Split(string commandLine)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(commandLine, "(?:[^\\s\\\"]+|\\\"(?:\\\\.|[^\\\"])*\\\")+");
        return matches.Select(m => m.Value.StartsWith('"') && m.Value.EndsWith('"')
            ? m.Value[1..^1].Replace("\\\"", "\"")
            : m.Value).ToArray();
    }
}
