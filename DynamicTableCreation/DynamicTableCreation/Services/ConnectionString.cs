using System;
using System.Collections.Generic;
using Npgsql;

namespace DynamicTableCreation.Services
{
    public class ConnectionStringService
    {
        public string[] GetTableNames(string connectionString)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                var tableNames = GetTableNames(connection);
                Console.WriteLine("Available User-Created Table Names:");
                foreach (var tableName in tableNames)
                {
                    Console.WriteLine(tableName);
                }
                return tableNames;
            }
        }
        private string[] GetTableNames(NpgsqlConnection connection)
        {
            using (var command = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema = 'public'", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    var tableNames = new List<string>();
                    while (reader.Read())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                    return tableNames.ToArray();
                }
            }
        }
    }
}
