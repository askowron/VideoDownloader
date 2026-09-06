using System.Diagnostics;

namespace VideoDownloader.Core.Tools
{
    /// <summary>
    /// Wraps yt-dlp's own self-update mechanism (yt-dlp -U) to keep the bundled
    /// binary current with https://github.com/yt-dlp/yt-dlp releases.
    /// </summary>
    public static class YtDlpUpdater
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

        public static string ExecutablePath => Path.Combine(AppContext.BaseDirectory, "yt-dlp.exe");

        /// <summary>
        /// Runs "yt-dlp.exe -U" and returns the trimmed output describing the result
        /// (e.g. already up to date, or updated to a newer version).
        /// </summary>
        public static async Task<string> UpdateAsync()
        {
            string path = ExecutablePath;
            if (!File.Exists(path))
                throw new FileNotFoundException("yt-dlp.exe was not found.", path);

            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "-U",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                throw new InvalidOperationException("Could not start yt-dlp.exe.");

            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(Timeout);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException("Timed out while checking for yt-dlp updates.");
            }

            string output = (await stdOutTask).Trim();
            string error = (await stdErrTask).Trim();

            if (process.ExitCode != 0)
                throw new Exception(error.Length > 0 ? error : $"yt-dlp update exited with code {process.ExitCode}.");

            return output.Length > 0 ? output : error;
        }
    }
}
