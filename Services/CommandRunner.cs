using System.Diagnostics;
using System.Text;
namespace GSMCheckTool.Services;
public static class CommandRunner
{
    public static async Task<(int ExitCode, string Output, string Error)> RunAsync(string file, string args, int timeoutMs = 12000)
    {
        try
        {
            var psi = new ProcessStartInfo(file, args)
            {
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
                CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            };
            using var p = new Process { StartInfo = psi };
            p.Start();
            var stdout = p.StandardOutput.ReadToEndAsync();
            var stderr = p.StandardError.ReadToEndAsync();
            using var cts = new CancellationTokenSource(timeoutMs);
            try { await p.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException) { try { p.Kill(true); } catch { } return (-1, await stdout, "Tempo limite excedido."); }
            return (p.ExitCode, await stdout, await stderr);
        }
        catch (Exception ex) { return (-1, "", ex.Message); }
    }
}