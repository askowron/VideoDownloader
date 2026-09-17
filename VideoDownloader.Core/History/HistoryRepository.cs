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

        /// <summary>Inserts a new row and returns its id, or -1 if the insert failed.</summary>
        public async Task<long> AddAsync(DownloadHistoryEntry entry)
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
                        ($url, $title, $startedAtUtc, $finishedAtUtc, $durationSeconds, $fileSizeBytes, $averageSpeedBytesPerSec, $status, $resolution, $format, $errorMessage);
                    SELECT last_insert_rowid();";

                AddEntryParameters(command, entry);

                var id = await command.ExecuteScalarAsync();
                return Convert.ToInt64(id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return -1;
            }
        }

        /// <summary>Overwrites an existing row (matched by <paramref name="id"/>) in place, e.g.
        /// turning the "Downloading" row <see cref="AddAsync"/> created into its final
        /// Completed/Failed/Canceled state. A no-op if <paramref name="id"/> is not positive
        /// (the initial insert itself failed, per <see cref="AddAsync"/>'s convention).</summary>
        public async Task UpdateAsync(long id, DownloadHistoryEntry entry)
        {
            if (id <= 0) return;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                await EnsureTableAsync(connection);

                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE DownloadHistory SET
                        Url = $url, Title = $title, StartedAtUtc = $startedAtUtc, FinishedAtUtc = $finishedAtUtc,
                        DurationSeconds = $durationSeconds, FileSizeBytes = $fileSizeBytes,
                        AverageSpeedBytesPerSec = $averageSpeedBytesPerSec, Status = $status,
                        Resolution = $resolution, Format = $format, ErrorMessage = $errorMessage
                    WHERE Id = $id;";

                AddEntryParameters(command, entry);
                command.Parameters.AddWithValue("$id", id);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private static void AddEntryParameters(SqliteCommand command, DownloadHistoryEntry entry)
        {
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
                    SELECT Id, Url, Title, StartedAtUtc, FinishedAtUtc, DurationSeconds, FileSizeBytes, AverageSpeedBytesPerSec, Status, Resolution, Format, ErrorMessage
                    FROM DownloadHistory
                    ORDER BY StartedAtUtc DESC;";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new DownloadHistoryEntry
                    {
                        Id = reader.GetInt64(0),
                        Url = reader.GetString(1),
                        Title = reader.GetString(2),
                        StartedAtUtc = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        FinishedAtUtc = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        DurationSeconds = reader.GetDouble(5),
                        FileSizeBytes = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                        AverageSpeedBytesPerSec = reader.IsDBNull(7) ? null : reader.GetDouble(7),
                        Status = reader.GetString(8),
                        Resolution = reader.IsDBNull(9) ? null : reader.GetString(9),
                        Format = reader.IsDBNull(10) ? null : reader.GetString(10),
                        ErrorMessage = reader.IsDBNull(11) ? null : reader.GetString(11),
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
