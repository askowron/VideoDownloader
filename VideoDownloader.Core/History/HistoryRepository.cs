using System.Globalization;
using Microsoft.Data.Sqlite;
using VideoDownloader.Core.Windows;

namespace VideoDownloader.Core.History
{
    /// <summary>
    /// Reads/writes <see cref="DownloadHistoryEntry"/> rows in a per-user SQLite database.
    /// Every public method swallows its own exceptions, mirroring <see cref="Registry"/>'s
    /// silent-fallback convention, so a locked/corrupt/inaccessible DB file never breaks a
    /// download or crashes the history screen.
    /// </summary>
    public class HistoryRepository
    {
        private readonly string _connectionString;

        public HistoryRepository() : this(GetDefaultDatabasePath())
        {
        }

        public HistoryRepository(string databasePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
            _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        }

        public static string GetDefaultDatabasePath()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Registry.COMPANY,
                Registry.APPNAME);
            return Path.Combine(folder, "history.db");
        }

        public async Task AddAsync(DownloadHistoryEntry entry)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                await EnsureTableAsync(connection);

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO DownloadHistory
                        (Url, Title, StartedAtUtc, FinishedAtUtc, DurationSeconds, FileSizeBytes, AverageSpeedBytesPerSec, Status, Resolution, Format, ErrorMessage)
                    VALUES
                        ($url, $title, $startedAtUtc, $finishedAtUtc, $durationSeconds, $fileSizeBytes, $averageSpeedBytesPerSec, $status, $resolution, $format, $errorMessage);";

                command.Parameters.AddWithValue("$url", entry.Url);
                command.Parameters.AddWithValue("$title", entry.Title);
                command.Parameters.AddWithValue("$startedAtUtc", entry.StartedAtUtc.ToString("o", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$finishedAtUtc", entry.FinishedAtUtc.ToString("o", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$durationSeconds", entry.DurationSeconds);
                command.Parameters.AddWithValue("$fileSizeBytes", (object?)entry.FileSizeBytes ?? DBNull.Value);
                command.Parameters.AddWithValue("$averageSpeedBytesPerSec", (object?)entry.AverageSpeedBytesPerSec ?? DBNull.Value);
                command.Parameters.AddWithValue("$status", entry.Status);
                command.Parameters.AddWithValue("$resolution", (object?)entry.Resolution ?? DBNull.Value);
                command.Parameters.AddWithValue("$format", (object?)entry.Format ?? DBNull.Value);
                command.Parameters.AddWithValue("$errorMessage", (object?)entry.ErrorMessage ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public async Task<IReadOnlyList<DownloadHistoryEntry>> GetAllAsync()
        {
            var results = new List<DownloadHistoryEntry>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                await EnsureTableAsync(connection);

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Url, Title, StartedAtUtc, FinishedAtUtc, DurationSeconds, FileSizeBytes, AverageSpeedBytesPerSec, Status, Resolution, Format, ErrorMessage
                    FROM DownloadHistory
                    ORDER BY StartedAtUtc DESC;";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new DownloadHistoryEntry
                    {
                        Url = reader.GetString(0),
                        Title = reader.GetString(1),
                        StartedAtUtc = DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        FinishedAtUtc = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        DurationSeconds = reader.GetDouble(4),
                        FileSizeBytes = reader.IsDBNull(5) ? null : reader.GetInt64(5),
                        AverageSpeedBytesPerSec = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                        Status = reader.GetString(7),
                        Resolution = reader.IsDBNull(8) ? null : reader.GetString(8),
                        Format = reader.IsDBNull(9) ? null : reader.GetString(9),
                        ErrorMessage = reader.IsDBNull(10) ? null : reader.GetString(10),
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            return results;
        }

        public async Task ClearAllAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                await EnsureTableAsync(connection);

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM DownloadHistory;";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private static async Task EnsureTableAsync(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS DownloadHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Url TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    StartedAtUtc TEXT NOT NULL,
                    FinishedAtUtc TEXT NOT NULL,
                    DurationSeconds REAL NOT NULL,
                    FileSizeBytes INTEGER NULL,
                    AverageSpeedBytesPerSec REAL NULL,
                    Status TEXT NOT NULL,
                    Resolution TEXT NULL,
                    Format TEXT NULL,
                    ErrorMessage TEXT NULL
                );";
            await command.ExecuteNonQueryAsync();
        }
    }
}
