using System.Diagnostics;

namespace VideoDownloader.Core.Tools
{
    /// <summary>
    /// Wraps yt-dlp's own self-update mechanism (yt-dlp -U) to keep yt-dlp current with
    /// https://github.com/yt-dlp/yt-dlp releases.
    /// </summary>
    /// <remarks>
    /// The copy shipped next to the app (<see cref="BundledPath"/>) is never run or updated in
    /// place: under Program Files a non-elevated -U can't write it, and an elevated one makes it
    /// newer than the MSI's copy, so the next major upgrade's file costing skips installing the
    /// "older" bundled file and RemoveExistingProducts then deletes the old one, leaving no
    /// yt-dlp.exe at all. Instead a per-user working copy under %LOCALAPPDATA% is seeded from
    /// the bundled one and that copy is the one that runs and self-updates.
    /// </remarks>
    public static class YtDlpUpdater
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
        private static readonly object SeedLock = new();

        /// <summary>The yt-dlp.exe installed alongside the app (read-only seed).</summary>
        public static string BundledPath => Path.Combine(AppContext.BaseDirectory, "yt-dlp.exe");

        /// <summary>The per-user, self-updating working copy.</summary>
        public static string WorkingCopyPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Windows.Registry.COMPANY,
            Windows.Registry.APPNAME,
            "yt-dlp.exe");

        /// <summary>
        /// Returns the yt-dlp.exe to run: the working copy, (re)seeded from the bundled one when
        /// it's missing or older than the bundled one. Falls back to the bundled path if seeding
        /// fails, and to whichever copy exists if the other is missing.
        /// </summary>
        public static string ExecutablePath
        {
            get
            {
                string bundled = BundledPath;
                string working = WorkingCopyPath;

                lock (SeedLock)
                {
                    try
                    {
                        if (File.Exists(bundled) && (!File.Exists(working) || GetVersion(bundled) > GetVersion(working)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(working)!);
                            File.Copy(bundled, working, overwrite: true);
                        }
                    }
                    catch
                    {
                        // Locked (e.g. mid-update) or unwritable profile - use whatever exists.
                    }
                }

                return File.Exists(working) ? working : bundled;
            }
        }

        private static Version GetVersion(string path)
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
        }

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
