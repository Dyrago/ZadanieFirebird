using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;
using System;
using System.IO;
using System.Text;

namespace DbMetaTool
{
    public static class Program
    {
        // Przykładowe wywołania:
        // DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
        // DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
        // DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Użycie:");
                Console.WriteLine("  build-db --db-dir <ścieżka> --scripts-dir <ścieżka>");
                Console.WriteLine("  export-scripts --connection-string <connStr> --output-dir <ścieżka>");
                Console.WriteLine("  update-db --connection-string <connStr> --scripts-dir <ścieżka>");
                return 1;
            }

            try
            {
                var command = args[0].ToLowerInvariant();

                switch (command)
                {
                    case "build-db":
                        {
                            string dbDir = GetArgValue(args, "--db-dir");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            BuildDatabase(dbDir, scriptsDir);
                            Console.WriteLine("Baza danych została zbudowana pomyślnie.");
                            return 0;
                        }

                    case "export-scripts":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string outputDir = GetArgValue(args, "--output-dir");

                            ExportScripts(connStr, outputDir);
                            Console.WriteLine("Skrypty zostały wyeksportowane pomyślnie.");
                            return 0;
                        }

                    case "update-db":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            UpdateDatabase(connStr, scriptsDir);
                            Console.WriteLine("Baza danych została zaktualizowana pomyślnie.");
                            return 0;
                        }

                    default:
                        Console.WriteLine($"Nieznane polecenie: {command}");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Błąd: " + ex.Message);
                return -1;
            }
        }

        private static string GetArgValue(string[] args, string name)
        {
            int idx = Array.IndexOf(args, name);
            if (idx == -1 || idx + 1 >= args.Length)
                throw new ArgumentException($"Brak wymaganego parametru {name}");
            return args[idx + 1];
        }

        /// <summary>
        /// Buduje nową bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void BuildDatabase(string databaseDirectory, string scriptsDirectory)
        {
            // TODO:
            // 1) Utwórz pustą bazę danych FB 5.0 w katalogu databaseDirectory.
            // 2) Wczytaj i wykonaj kolejno skrypty z katalogu scriptsDirectory
            //    (tylko domeny, tabele, procedury).
            // 3) Obsłuż błędy i wyświetl raport.

            if (!Directory.Exists(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
                Logger.Info("Utworzono nową ścieżkę dla bazy danych.");
            }

            string dbPath = Path.Combine(databaseDirectory, "database.fdb");

            if (File.Exists(dbPath))
            {
                Logger.Warning($"W podanej lojakizacji {databaseDirectory} baza już istnieje!");
                Logger.Warning("Wybierz inną lokalizację lub usuń istniejącą bazę.");
                return;
            }

            string connStr = new FbConnectionStringBuilder
            {
                Database = dbPath,
                ServerType = FbServerType.Default,
                DataSource = "localhost",
                UserID = "SYSDBA",
                Password = "masterkey",
                ClientLibrary = "fbclient.dll"
            }.ToString();

            try
            {
                FbConnection.CreateDatabase(connStr, pageSize: 8192, forcedWrites: true);

                Logger.Info("Utworzono pustą bazę danych.");


                if (!Directory.Exists(scriptsDirectory))
                {
                    Logger.Error($"Ścieżka do skryptów nie istnieje: {scriptsDirectory}");
                    return;
                }

                var sqlFiles = Directory.GetFiles(scriptsDirectory, "*.sql")
                                        .OrderBy(f => f)
                                        .ToArray();

                if (sqlFiles.Length == 0)
                {
                    Logger.Error("Brak plików .sql w katalogu skryptów.");
                    return;
                }

                using var conn = new FbConnection(connStr);
                conn.Open();

                Logger.Info("Połączono z nowo utworzoną bazą.");

                foreach (var file in sqlFiles)
                {
                    try
                    {
                        Logger.Info($"Wykonywanie: {Path.GetFileName(file)}");

                        ExecuteSqlFile(conn, file);

                        Logger.Success($"Zakończono: {Path.GetFileName(file)}");
                    }
                    catch (Exception exFile)
                    {
                        Logger.Error($"Błąd podczas wykonywania pliku {Path.GetFileName(file)}: {exFile.Message}");
                        return;
                    }
                }

                Logger.Success("Proces budowania bazy zakończony pomyślnie.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Błąd krytyczny: {ex.Message}");
            }
        }

        /// <summary>
        /// Generuje skrypty metadanych z istniejącej bazy danych Firebird 5.0.
        /// </summary>
        public static void ExportScripts(string connectionString, string outputDirectory)
        {
            // TODO:
            // 1) Połącz się z bazą danych przy użyciu connectionString.
            // 2) Pobierz metadane domen, tabel (z kolumnami) i procedur.
            // 3) Wygeneruj pliki .sql / .json / .txt w outputDirectory.

            Directory.CreateDirectory(outputDirectory);
            Logger.Info("Utworzono ścieżkę dla eksportowanych skryptów.");

            using var conn = new FbConnection(connectionString);
            conn.Open();
            Logger.Info("Nawiązano połączenie z bazą danych");

            try
            {
                ExportDomains(conn, outputDirectory);
                Logger.Success("Pomyślnie eksportowano domeny.");
            }
            catch (Exception ex)
            {
                Logger.Error("Bład podczas eksportowania domen.");
                return ;
            }

            try
            {
                ExportTables(conn, outputDirectory);
                Logger.Success("Pomyślnie eksportowano tabele.");
            }
            catch (Exception ex)
            {
                Logger.Error("Bład podczas eksportowania tabel.");
                return;
            }

            try
            {
                ExportProcedures(conn, outputDirectory);
                Logger.Success("Pomyślnie eksportowano procedury.");
            }
            catch (Exception ex)
            {
                Logger.Error("Bład podczas eksportowania procedur.");
                return;
            }

            Logger.Success("Pomyślnie eksportowano skrypty.");
        }

        /// <summary>
        /// Aktualizuje istniejącą bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void UpdateDatabase(string connectionString, string scriptsDirectory)
        {
            // TODO:
            // 1) Połącz się z bazą danych przy użyciu connectionString.
            // 2) Wykonaj skrypty z katalogu scriptsDirectory (tylko obsługiwane elementy).
            // 3) Zadbaj o poprawną kolejność i bezpieczeństwo zmian.

            using var conn = new FbConnection(connectionString);
            conn.Open();
            Logger.Info("Nawiązano połączenie z bazą danych");

            var sqlFiles = Directory.GetFiles(scriptsDirectory, "*.sql")
                                    .OrderBy(f => f);

            foreach (var file in sqlFiles)
            {
                try
                {
                    Logger.Info($"Wykonywanie: {Path.GetFileName(file)}");

                    ExecuteSqlFile(conn, file);

                    Logger.Success($"Zakończono: {Path.GetFileName(file)}");
                }
                catch (Exception exFile)
                {
                    Logger.Error($"Błąd podczas wykonywania pliku {Path.GetFileName(file)}: {exFile.Message}");
                    return;
                }
            }

            Logger.Success("Aktualizacja zakończona sukcesem.");
        }

        private static void ExecuteSqlFile(FbConnection conn, string file)
        {
            ExecuteFirebirdScript(conn, file);
        }

        private static void ExecuteFirebirdScript(FbConnection conn, string sqlFile)
        {
            string text = File.ReadAllText(sqlFile);

            // Parse the script
            var script = new FbScript(text);
            script.Parse();

            using var cmd = new FbCommand("", conn);

            // Iterate over each parsed statement
            foreach (var result in script.Results)
            {
                string statement = result.Text.Trim();

                if (string.IsNullOrWhiteSpace(statement))
                    continue;

                try
                {
                    cmd.CommandText = statement;
                    cmd.ExecuteNonQuery();
                }
                catch (FbException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error executing statement:\n{statement}\n{ex.Message}");
                    Console.ResetColor();
                    throw; // Stop execution or handle as needed
                }
            }
        }

        private static void ExportDomains(FbConnection conn, string outputDir)
        {
            string file = Path.Combine(outputDir, "001_domains.sql");
            using var sw = new StreamWriter(file, false, Encoding.UTF8);

            string sql =
                @"SELECT DISTINCT f.RDB$FIELD_NAME, f.RDB$FIELD_TYPE, f.RDB$FIELD_LENGTH
                  FROM RDB$FIELDS f
                  JOIN RDB$RELATION_FIELDS rf ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
                  JOIN RDB$RELATIONS r ON rf.RDB$RELATION_NAME = r.RDB$RELATION_NAME
                  WHERE r.RDB$SYSTEM_FLAG = 0
                    AND f.RDB$SYSTEM_FLAG = 0
                    AND f.RDB$FIELD_NAME NOT LIKE 'RDB$%'";

            using var cmd = new FbCommand(sql, conn);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                string name = r.GetString(0).Trim();
                int type = r.GetInt16(1);
                int length = r.IsDBNull(2) ? 0 : r.GetInt32(2);

                sw.WriteLine($"CREATE DOMAIN {name} AS {MapFbType(type, length)};");
            }
        }

        private static void ExportTables(FbConnection conn, string outputDir)
        {
            string file = Path.Combine(outputDir, "002_tables.sql");
            using var sw = new StreamWriter(file, false, Encoding.UTF8);

            // All user tables (exclude system + views)
            string sql =
                @"SELECT RDB$RELATION_NAME 
                  FROM RDB$RELATIONS
                  WHERE RDB$SYSTEM_FLAG = 0
                    AND RDB$VIEW_BLR IS NULL";

            using var cmd = new FbCommand(sql, conn);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                string tableName = r.GetString(0).Trim();
                sw.WriteLine($"CREATE TABLE {tableName} (");

                var fields = GetTableFields(conn, tableName);

                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    string nullFlag = f.notNull ? "NOT NULL" : "";
                    string comma = (i < fields.Length - 1) ? "," : "";

                    sw.WriteLine($"  {f.name} {MapFbType(f.type, f.length)} {nullFlag}{(i < fields.Length - 1 ? "," : "")}");
                }

                sw.WriteLine(");");
                sw.WriteLine();
            }
        }

        private static (string name, int type, int length, bool notNull)[] GetTableFields(FbConnection conn, string tableName)
        {
            var result = new System.Collections.Generic.List<(string, int, int, bool)>();

            string sql =
                @"SELECT 
            f.RDB$FIELD_NAME,
            t.RDB$FIELD_TYPE,
            t.RDB$FIELD_LENGTH,
            f.RDB$NULL_FLAG
          FROM RDB$RELATION_FIELDS f
          JOIN RDB$FIELDS t ON f.RDB$FIELD_SOURCE = t.RDB$FIELD_NAME
          WHERE f.RDB$RELATION_NAME = @T";

            using var cmd = new FbCommand(sql, conn);
            cmd.Parameters.AddWithValue("@T", tableName);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string name = r.GetString(0).Trim();
                int type = r.GetInt32(1);
                int length = r.GetInt32(2);
                bool notNull = !r.IsDBNull(3) && r.GetInt16(3) == 1;

                result.Add((name, type, length, notNull));
            }

            return result.ToArray();
        }

        private static void ExportProcedures(FbConnection conn, string outputDir)
        {
            string file = Path.Combine(outputDir, "003_procedures.sql");
            using var sw = new StreamWriter(file, false, Encoding.UTF8);

            string sql =
                @"SELECT RDB$PROCEDURE_NAME, RDB$PROCEDURE_SOURCE
          FROM RDB$PROCEDURES
          WHERE RDB$SYSTEM_FLAG = 0";

            using var cmd = new FbCommand(sql, conn);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                string name = r.GetString(0).Trim();
                string source = r.IsDBNull(1) ? "" : r.GetString(1).Trim();

                // Pobranie parametrów procedury
                string paramSql = @"
                                    SELECT 
                                        P.RDB$PARAMETER_NAME, 
                                        P.RDB$PARAMETER_TYPE, -- 0 = input, 1 = output
                                        F.RDB$FIELD_TYPE,
                                        F.RDB$FIELD_LENGTH
                                    FROM RDB$PROCEDURE_PARAMETERS P
                                    JOIN RDB$FIELDS F ON F.RDB$FIELD_NAME = P.RDB$FIELD_SOURCE
                                    WHERE P.RDB$PROCEDURE_NAME = @procName
                                    ORDER BY P.RDB$PARAMETER_NUMBER";

                using var paramCmd = new FbCommand(paramSql, conn);
                paramCmd.Parameters.AddWithValue("@procName", name);
                using var paramR = paramCmd.ExecuteReader();

                var inputParams = new List<string>();
                var outputParams = new List<string>();

                while (paramR.Read())
                {
                    string paramName = paramR.GetString(0).Trim();
                    short paramType = paramR.GetInt16(1);
                    short fieldType = paramR.IsDBNull(2) ? (short)0 : paramR.GetInt16(2);
                    int fieldLength = paramR.IsDBNull(3) ? 0 : paramR.GetInt32(3);

                    string sqlType = MapFbType(fieldType, fieldLength);

                    if (paramType == 0)
                        inputParams.Add($"{paramName} {sqlType}");
                    else
                        outputParams.Add($"{paramName} {sqlType}");
                }

                // Ustawienie terminatora
                sw.WriteLine("SET TERM ^ ;");

                // Definicja procedury
                sw.Write($"CREATE PROCEDURE {name}");
                if (inputParams.Count > 0)
                    sw.Write($" ({string.Join(", ", inputParams)})");
                if (outputParams.Count > 0)
                    sw.Write($" RETURNS ({string.Join(", ", outputParams)})");
                sw.WriteLine();
                sw.WriteLine("AS");              // dodanie AS
                sw.WriteLine(source + "^");      // kończy procedurę terminatorem

                sw.WriteLine("SET TERM ; ^");    // przywrócenie terminatora
                sw.WriteLine();
            }
        }

        private static string MapFbType(int type, int length)
        {
            return type switch
            {
                7 => "SMALLINT",
                8 => "INTEGER",
                10 => "FLOAT",
                12 => "DATE",
                13 => "TIME",
                14 => $"CHAR({length})",
                16 => "BIGINT",
                27 => "DOUBLE PRECISION",
                35 => "TIMESTAMP",
                261 => "BLOB",
                _ => $"VARCHAR({length})"
            };
        }

        #region Logging

        private class Logger
        {
            private static void Log(string message, LogLevel logLevel) => Write(message, logLevel);
            public static void Info(string message) => Write(message, LogLevel.Log);
            public static void Warning(string message) => Write(message, LogLevel.Warning);
            public static void Success(string message) => Write(message, LogLevel.Success);
            public static void Error(string message) => Write(message, LogLevel.Error);

            private static void Write(string message, LogLevel logLevel)
            {
                try
                {
                    var consoleColor = logLevel switch
                    {
                        LogLevel.Log => ConsoleColor.Gray,
                        LogLevel.Warning => ConsoleColor.Yellow,
                        LogLevel.Success => ConsoleColor.Green,
                        LogLevel.Error => ConsoleColor.Red,
                        _ => ConsoleColor.White
                    };

                    var dateFormat = DateTime.Now.ToString("[HH:mm:ss]");
                    var messageFormat = $"{dateFormat} {logLevel}: {message}";

                    Console.ForegroundColor = consoleColor;
                    Console.WriteLine(messageFormat);
                }
                finally
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            private class LogEntry
            {
                public string? Text { get; set; }
                public ConsoleColor Color { get; set; }

                public LogEntry(string? text, ConsoleColor color = ConsoleColor.Gray)
                {
                    Text = text;
                    Color = color;
                }
            }

            private enum LogLevel
            {
                Log,
                Warning,
                Success,
                Error
            }
        }     
    }

        #endregion
}
