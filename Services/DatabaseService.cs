using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ClockbusterApps.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        public DatabaseService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClockbusterApps"
            );

            Directory.CreateDirectory(appDataPath);

            _dbPath = Path.Combine(appDataPath, "timeclock.db");
            _connectionString = $"Data Source={_dbPath}";

            InitializeDatabase();
            MigrateFromTextFile();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Sessions (
                    Id TEXT PRIMARY KEY,
                    ApplicationName TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    DurationMinutes REAL NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                )";
            command.ExecuteNonQuery();

            // Create index for faster queries
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS idx_sessions_starttime 
                ON Sessions(StartTime DESC)";
            command.ExecuteNonQuery();
        }

        private void MigrateFromTextFile()
        {
            var logFilePath = Path.Combine(
                Path.GetDirectoryName(_dbPath),
                "timeclock.log"
            );

            if (!File.Exists(logFilePath))
                return;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                // Check if we've already migrated
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM Sessions";
                var count = (long)checkCommand.ExecuteScalar();

                if (count > 0)
                    return; // Already have data, skip migration

                var lines = File.ReadAllLines(logFilePath);
                using var transaction = connection.BeginTransaction();

                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
                    INSERT INTO Sessions (Id, ApplicationName, StartTime, EndTime, DurationMinutes, IsActive)
                    VALUES (@id, @app, @start, @end, @duration, 0)";

                insertCommand.Parameters.Add("@id", SqliteType.Text);
                insertCommand.Parameters.Add("@app", SqliteType.Text);
                insertCommand.Parameters.Add("@start", SqliteType.Text);
                insertCommand.Parameters.Add("@end", SqliteType.Text);
                insertCommand.Parameters.Add("@duration", SqliteType.Real);

                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 5)
                    {
                        insertCommand.Parameters["@id"].Value = parts[0];
                        insertCommand.Parameters["@app"].Value = parts[1];
                        insertCommand.Parameters["@start"].Value = parts[2];
                        insertCommand.Parameters["@end"].Value = parts[3];
                        insertCommand.Parameters["@duration"].Value = double.Parse(parts[4]);
                        insertCommand.ExecuteNonQuery();
                    }
                }

                transaction.Commit();

                // Backup old file
                File.Move(logFilePath, logFilePath + ".migrated");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration error: {ex.Message}");
            }
        }

        public void InsertSession(string id, string applicationName, DateTime startTime, DateTime? endTime, double durationMinutes, bool isActive)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Sessions (Id, ApplicationName, StartTime, EndTime, DurationMinutes, IsActive)
                VALUES (@id, @app, @start, @end, @duration, @active)";

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@app", applicationName);
            command.Parameters.AddWithValue("@start", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@end", endTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@duration", durationMinutes);
            command.Parameters.AddWithValue("@active", isActive ? 1 : 0);

            command.ExecuteNonQuery();
        }

        public void UpdateSession(string id, DateTime endTime, double durationMinutes)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Sessions 
                SET EndTime = @end, DurationMinutes = @duration, IsActive = 0
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@end", endTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@duration", durationMinutes);

            command.ExecuteNonQuery();
        }

        public List<SessionData> GetAllSessions()
        {
            var sessions = new List<SessionData>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, ApplicationName, StartTime, EndTime, DurationMinutes, IsActive
                FROM Sessions
                ORDER BY StartTime DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                sessions.Add(new SessionData
                {
                    Id = reader.GetString(0),
                    ApplicationName = reader.GetString(1),
                    StartTime = DateTime.Parse(reader.GetString(2)),
                    EndTime = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
                    DurationMinutes = reader.GetDouble(4),
                    IsActive = reader.GetInt32(5) == 1
                });
            }

            return sessions;
        }

        public void DeleteSessions(List<string> ids)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Sessions WHERE Id = @id";
            command.Parameters.Add("@id", SqliteType.Text);

            foreach (var id in ids)
            {
                command.Parameters["@id"].Value = id;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public void DeleteAllSessions()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Sessions";
            command.ExecuteNonQuery();
        }

        public int GetSessionCount()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Sessions";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }

    public class SessionData
    {
        public string Id { get; set; }
        public string ApplicationName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double DurationMinutes { get; set; }
        public bool IsActive { get; set; }
    }
}