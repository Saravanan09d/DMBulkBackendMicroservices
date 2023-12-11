//using Npgsql;
//using System;
//using System.Collections.Generic;

//namespace DynamicTableCreation.Services
//{
//    public class ConnectionStringService
//    {
//        // List of table names to exclude during migration
//        private readonly List<string> TablesToExclude = new List<string>
//        {
//            "EntityColumnListMetadataModels",
//            "logChilds",
//            "UserRoleModel",
//            "UserTableModel",
//            "EntityListMetadataModels",
//            "logParents",
//            "__EFMigrationsHistory"
//        };

//        public string[] GetTableNames(string connectionString)
//        {
//            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
//            {
//                connection.Open();
//                var tableNames = GetTableNames(connection);
//                Console.WriteLine("Available User-Created Table Names:");
//                foreach (var tableName in tableNames)
//                {
//                    Console.WriteLine(tableName);
//                }
//                return tableNames;
//            }
//        }

//        private string[] GetTableNames(NpgsqlConnection connection)
//        {
//            using (var command = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema = 'public'", connection))
//            {
//                using (var reader = command.ExecuteReader())
//                {
//                    var tableNames = new List<string>();
//                    while (reader.Read())
//                    {
//                        string tableName = reader.GetString(0);

//                        // Exclude tables specified in TablesToExclude
//                        if (!TablesToExclude.Contains(tableName))
//                        {
//                            tableNames.Add(tableName);
//                        }
//                    }
//                    return tableNames.ToArray();
//                }
//            }
//        }

//        public Dictionary<string, string> GetTableColumnsAndTypes(string connectionString, string tableName)
//        {
//            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
//            {
//                connection.Open();
//                return GetTableColumnsAndTypes(connection, tableName);
//            }
//        }

//        private Dictionary<string, string> GetTableColumnsAndTypes(NpgsqlConnection connection, string tableName)
//        {
//            var columnsAndTypes = new Dictionary<string, string>();

//            using (var command = new NpgsqlCommand($"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = '{tableName}'", connection))
//            {
//                using (var reader = command.ExecuteReader())
//                {
//                    while (reader.Read())
//                    {
//                        string columnName = reader.GetString(0);
//                        string dataType = reader.GetString(1);
//                        columnsAndTypes.Add(columnName, dataType);
//                    }
//                }
//            }
//            return columnsAndTypes;
//        }
//    }
//}


using DynamicTableCreation.Models.DTO;
using Npgsql;
using System;
using System.Collections.Generic;

namespace DynamicTableCreation.Services
{
    public class ConnectionStringService
    {
        // List of table names to exclude during migration
        private readonly List<string> TablesToExclude = new List<string>
        {
            "EntityColumnListMetadataModels",
            "logChilds",
            "UserRoleModel",
            "UserTableModel",
            "EntityListMetadataModels",
            "logParents",
            "__EFMigrationsHistory"
        };

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
                        string tableName = reader.GetString(0);

                        // Exclude tables specified in TablesToExclude
                        if (!TablesToExclude.Contains(tableName))
                        {
                            tableNames.Add(tableName);
                        }
                    }
                    return tableNames.ToArray();
                }
            }
        }

        public Dictionary<string, List<ColumnInfoDTO>> GetTableDetails(string connectionString)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                return GetTableDetails(connection);
            }
        }

        private Dictionary<string, List<ColumnInfoDTO>> GetTableDetails(NpgsqlConnection connection)
        {
            var tableDetails = new Dictionary<string, List<ColumnInfoDTO>>();

            foreach (var tableName in GetTableNames(connection))
            {
                tableDetails[tableName] = GetTableColumnsAndTypes(connection, tableName);
            }

            return tableDetails;
        }

        private List<ColumnInfoDTO> GetTableColumnsAndTypes(NpgsqlConnection connection, string tableName)
        {
            using (var command = new NpgsqlCommand($"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = '{tableName}'", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    var columnsAndTypes = new List<ColumnInfoDTO>();
                    while (reader.Read())
                    {
                        string columnName = reader.GetString(0);
                        string dataType = reader.GetString(1);

                        columnsAndTypes.Add(new ColumnInfoDTO { Name = columnName, Type = dataType });
                    }
                    return columnsAndTypes;
                }
            }
        }

    }
}

