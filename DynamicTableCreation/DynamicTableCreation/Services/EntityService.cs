using DynamicTableCreation.Data;
using DynamicTableCreation.Models;
using DynamicTableCreation.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace DynamicTableCreation.Services
{
    public class EntityService
    {
        private readonly ApplicationDbContext _dbContext;
        public EntityService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                var lowerCaseTableName = tableName.ToLower();
                var existingEntity = await _dbContext.EntityListMetadataModels
                    .AnyAsync(e => e.EntityName.ToLower() == lowerCaseTableName);
                return existingEntity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while checking if table '{tableName}' exists: {ex.Message}");
                return false; 
            }
        }

        public TableCreationRequest MapToModel(TableCreationRequestDTO dto)
        {
            try
            {
                return new TableCreationRequest
                {
                    TableName = dto.TableName,
                    Columns = dto.Columns.Select(columnDto => new ColumnDefinition
                    {
                        EntityColumnName = columnDto.EntityColumnName,
                        DataType = columnDto.DataType,
                        Length = columnDto.Length,
                        MinLength = columnDto.MinLength,
                        MaxLength = columnDto.MaxLength,
                        MaxRange = columnDto.MaxRange,
                        MinRange = columnDto.MinRange,
                        DateMaxValue = columnDto.DateMaxValue,
                        DateMinValue = columnDto.DateMinValue,
                        Description = columnDto.Description,
                        ListEntityId = columnDto.ListEntityId,
                        ListEntityKey = columnDto.ListEntityKey,
                        ListEntityValue = columnDto.ListEntityValue,
                        True = columnDto.True,
                        False = columnDto.False,
                        IsNullable = columnDto.IsNullable,
                        DefaultValue = columnDto.DefaultValue,
                        ColumnPrimaryKey = columnDto.ColumnPrimaryKey
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in MapToModel: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CreateDynamicTableAsync(TableCreationRequest request)
        {
            try
            {
                var entityList = await CreateTableMetadataAsync(request);
                if (entityList == null)
                {
                    return false;
                }
                await BindColumnMetadataAsync(request, entityList);
                var createTableSql = GenerateCreateTableSql(request);
                await _dbContext.Database.ExecuteSqlRawAsync(createTableSql);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<EntityListMetadataModel> CreateTableMetadataAsync(TableCreationRequest request)
        {
            var lowerCaseTableName = request.TableName.ToLower();
            var existingEntity = await _dbContext.EntityListMetadataModels
                .FirstOrDefaultAsync(e => e.EntityName.ToLower() == lowerCaseTableName);
            if (existingEntity != null)
            {
                return existingEntity;
            }
            var entityList = new EntityListMetadataModel
            {
                EntityName = request.TableName,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
            };
            try
            {
                _dbContext.EntityListMetadataModels.Add(entityList);
                await _dbContext.SaveChangesAsync();
                return entityList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private async Task BindColumnMetadataAsync(TableCreationRequest request, EntityListMetadataModel entityList)
        {
            try
            {
                foreach (var column in request.Columns)
                {
                    var existingColumn = await _dbContext.EntityColumnListMetadataModels
                        .FirstOrDefaultAsync(c => c.EntityColumnName.ToLower() == column.EntityColumnName.ToLower() && c.EntityId == entityList.Id);

                    if (existingColumn != null)
                    {
                        continue;
                    }
                    var entityColumn = new EntityColumnListMetadataModel
                    {
                        EntityColumnName = column.EntityColumnName,
                        Datatype = column.DataType,
                        Length = column.Length,
                        MinLength = column.MinLength,
                        MaxLength = column.MaxLength,
                        MinRange = column.MinRange,
                        MaxRange = column.MaxRange,
                        DateMinValue = column.DateMinValue,
                        DateMaxValue = column.DateMaxValue,
                        Description = column.Description,
                        IsNullable = column.IsNullable,
                        DefaultValue = column.DefaultValue,
                        ListEntityId = column.ListEntityId,
                        ListEntityKey = column.ListEntityKey,
                        ListEntityValue = column.ListEntityValue,
                        True = column.True,
                        False = column.False,
                        ColumnPrimaryKey = column.ColumnPrimaryKey,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        EntityId = entityList.Id
                    };

                    _dbContext.EntityColumnListMetadataModels.Add(entityColumn);
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string logFilePath = "error.log";
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"[{DateTime.UtcNow}] An error occurred: {ex.Message}");
                    writer.WriteLine($"Stack Trace: {ex.StackTrace}");
                    writer.WriteLine();
                }
                throw;
            }
        }

        private string GetTableNameForListEntityId(int entityId)
        {
            try
            {
                // Assuming EntityListMetadataModels is the DbSet in your DbContext
                var entity = _dbContext.EntityListMetadataModels
                    .FirstOrDefault(e => e.Id == entityId);

                if (entity != null)
                {
                    // Check if EntityName is not null or empty before returning
                    if (!string.IsNullOrEmpty(entity.EntityName))
                    {
                        return entity.EntityName;
                    }
                }

                return "TableNotFound";
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine($"An error occurred while getting table name: {ex.Message}");
                return "TableNotFound";
            }
        }
        private string GetColumnNameForListKeyId(int listEntityKey)
        {
            try
            {
                // Assuming EntityColumnListMetadataModels is the DbSet in your DbContext
                var column = _dbContext.EntityColumnListMetadataModels
                    .FirstOrDefault(e => e.ListEntityKey == listEntityKey);

                if (column != null)
                {
                    // Check if EntityColumnName is not null or empty before returning
                    if (!string.IsNullOrEmpty(column.EntityColumnName))
                    {
                        return column.EntityColumnName;
                    }
                }

                return "ColumnNotFound";
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine($"An error occurred while getting column name: {ex.Message}");
                return "ColumnNotFound";
            }
        }
        private string GetDatatypeForListEntityKey(int listEntityKey)
        {
            try
            {
                // Assuming EntityColumnListMetadataModels is the DbSet in your DbContext
                var column = _dbContext.EntityColumnListMetadataModels
                    .FirstOrDefault(e => e.ListEntityKey == listEntityKey);

                if (column != null)
                {
                    // Check if Datatype is not null or empty before returning
                    if (!string.IsNullOrEmpty(column.Datatype))
                    {
                        // Convert the datatype to a standardized form
                        return ConvertToStandardDatatype(column.Datatype);
                    }
                }

                return "DatatypeNotFound";
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine($"An error occurred while getting datatype: {ex.Message}");
                return "DatatypeNotFound";
            }
        }

        private string ConvertToStandardDatatype(string originalDatatype)
        {
            switch (originalDatatype.ToLower())
            {
                case "int":
                case "integer":
                    return "integer";

                case "string":
                case "varchar":
                    return "varchar";

                // Add more cases as needed for other datatypes

                default:
                    return "UnknownDatatype";
            }
        }



        private string GenerateCreateTableSql(TableCreationRequest request)
        {
            try
            {
                var createTableSql = $"CREATE TABLE \"{request.TableName}\" (";
                bool hasColumns = false;

                foreach (var column in request.Columns)
                {
                    if (hasColumns)
                    {
                        createTableSql += ",";
                    }

                    createTableSql += $"\"{column.EntityColumnName}\" ";

                    switch (column.DataType.ToLower())
                    {
                        case "int":
                            createTableSql += "integer";
                            break;
                        case "date":
                            createTableSql += "date";
                            break;
                        case "string":
                            createTableSql += $"varchar({(column.MaxLength > 0 ? column.MaxLength : 255)})";
                            break;
                        case "char":
                            createTableSql += $"char({(column.Length == 1 ? column.Length : 255)})";
                            break;
                        case "listofvalue":
                            var referencedTableName = GetTableNameForListEntityId(column.ListEntityId);
                            var referanceKeyName = GetColumnNameForListKeyId(column.ListEntityKey);
                            var referanceEntityValue = GetDatatypeForListEntityKey(column.ListEntityKey);
                            if (!string.IsNullOrEmpty(referencedTableName))
                            {
                                createTableSql += $"{referanceEntityValue} REFERENCES \"{referencedTableName}\"(\"{referanceKeyName}\") NOT NULL";
                            }
                            else
                            {
                                createTableSql += "varchar";
                            }
                            break;
                        case "boolean":
                            createTableSql += "boolean";
                            break;
                        case "time":
                            createTableSql += "time";
                            break;
                        case "timestamp":
                            createTableSql += "timestamp";
                            break;
                        default:
                            createTableSql += "varchar";
                            break;
                    }

                    if (!column.IsNullable)
                    {
                        createTableSql += " NOT NULL";
                    }

                    if (!string.IsNullOrEmpty(column.DefaultValue))
                    {
                        createTableSql += $" DEFAULT '{column.DefaultValue}'";
                    }

                    if (column.ColumnPrimaryKey)
                    {
                        createTableSql += " PRIMARY KEY";
                    }

                    hasColumns = true;
                }

                createTableSql += hasColumns ? "," : "";
                createTableSql += "\"createddate\" timestamp DEFAULT CURRENT_TIMESTAMP";
                createTableSql += ");";

                return createTableSql;
            }
            catch (Exception ex)
            {
                string logFilePath = "error.log";
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"[{DateTime.UtcNow}] An error occurred: {ex.Message}");
                    writer.WriteLine($"Stack Trace: {ex.StackTrace}");
                    writer.WriteLine();
                }
                throw;
            }
        }

        public string GetOldEntityName(int entityId)
        {
            var oldEntityName = _dbContext.EntityListMetadataModels
                .Where(e => e.Id == entityId)
                .Select(e => e.EntityName)
                .FirstOrDefault();

            return oldEntityName;
        }

        public int GetEntityIdForTableName(string entityName)
        {
            var entity = _dbContext.EntityListMetadataModels
                   .FirstOrDefault(e => e.EntityName == entityName);

            return entity?.Id ?? 0;
        }
        public void UpdateEntityColumn(int entityId, string newEntityName, List<EntityColumnProperties> newEntityColumns)
        {
            // Get the old entity name
            string oldEntityName = GetOldEntityName(entityId);

            // Drop the existing table
            DropTable(oldEntityName);

            // Get the existing entity with columns
            var existingEntity = _dbContext.EntityListMetadataModels
                .Include(e => e.EntityColumns)
                .FirstOrDefault(e => e.Id == entityId);

            if (existingEntity == null)
            {
                return;
            }
            // Update entity name
            existingEntity.EntityName = newEntityName;
            // Delete old values for the given entityId
            DeleteOldValues(entityId);
            // Update existing columns and add new columns
            foreach (var newColumn in newEntityColumns)
            {
                var existingColumn = existingEntity.EntityColumns
                    .FirstOrDefault(c => c.EntityColumnName == newColumn.EntityColumnName);

                if (existingColumn != null)
                {
                    // Update existing column
                    UpdateExistingColumn(existingColumn, newColumn);
                }
                else
                {
                    // Add new column
                    AddNewColumn(existingEntity, newColumn);
                }
            }

            // Save changes to the database
            _dbContext.SaveChanges();

            // Generate SQL to create the new table with updated columns
            var createTableSql = GenerateCreateTableSql(new TableCreationRequest
            {
                TableName = newEntityName,
                Columns = newEntityColumns.Select(ConvertToColumnDefinition).ToList()
            });

            // Execute the SQL to create the new table
            _dbContext.Database.ExecuteSqlRaw(createTableSql);
        }
        private void DeleteOldValues(int entityId)
        {
            try
            {
                var recordsToDelete = _dbContext.EntityColumnListMetadataModels
                    .Where(e => e.EntityId == entityId)
                    .ToList();
                _dbContext.EntityColumnListMetadataModels.RemoveRange(recordsToDelete);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while deleting old values: {ex.Message}");
            }
        }


        private void UpdateExistingColumn(EntityColumnListMetadataModel existingColumn, EntityColumnProperties newColumn)
        {
            existingColumn.Datatype = newColumn.Datatype;
            existingColumn.Length = newColumn.Length;
            existingColumn.MinLength = newColumn.MinLength;
            existingColumn.MaxLength = newColumn.MaxLength;
            existingColumn.MaxRange = newColumn.MaxRange;
            existingColumn.MinRange = newColumn.MinRange;
            existingColumn.DateMinValue = newColumn.DateMinValue;
            existingColumn.DateMaxValue = newColumn.DateMaxValue;
            existingColumn.Description = newColumn.Description;
            existingColumn.IsNullable = newColumn.IsNullable;
            existingColumn.DefaultValue = newColumn.DefaultValue;
            existingColumn.ListEntityId = newColumn.ListEntityId;
            existingColumn.ListEntityKey = newColumn.ListEntityKey;
            existingColumn.ListEntityValue = newColumn.ListEntityValue;
            existingColumn.True = newColumn.True;
            existingColumn.False = newColumn.False;
            existingColumn.ColumnPrimaryKey = newColumn.ColumnPrimaryKey;
            existingColumn.UpdatedDate = DateTime.UtcNow;
        }

        private void AddNewColumn(EntityListMetadataModel existingEntity, EntityColumnProperties newColumn)
        {
            var entityColumn = new EntityColumnListMetadataModel
            {
                EntityColumnName = newColumn.EntityColumnName,
                Datatype = newColumn.Datatype,
                Length = newColumn.Length,
                MinLength = newColumn.MinLength,
                MaxLength = newColumn.MaxLength,
                MaxRange = newColumn.MaxRange,
                MinRange = newColumn.MinRange,
                DateMinValue = newColumn.DateMinValue,
                DateMaxValue = newColumn.DateMaxValue,
                Description = newColumn.Description,
                IsNullable = newColumn.IsNullable,
                DefaultValue = newColumn.DefaultValue,
                ListEntityId = newColumn.ListEntityId,
                ListEntityKey = newColumn.ListEntityKey,
                ListEntityValue = newColumn.ListEntityValue,
                True = newColumn.True,
                False = newColumn.False,
                ColumnPrimaryKey = newColumn.ColumnPrimaryKey,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
            };
            existingEntity.EntityColumns.Add(entityColumn);
        }
        private ColumnDefinition ConvertToColumnDefinition(EntityColumnProperties entityColumn)
        {
            return new ColumnDefinition
            {
                EntityColumnName = entityColumn.EntityColumnName,
                DataType = entityColumn.Datatype,
                Length = entityColumn.Length,
                MinLength = entityColumn.MinLength,
                MaxLength = entityColumn.MaxLength,
                MaxRange = entityColumn.MaxRange,
                MinRange = entityColumn.MinRange,
                DateMinValue = entityColumn.DateMinValue,
                DateMaxValue = entityColumn.DateMaxValue,
                Description = entityColumn.Description,
                IsNullable = entityColumn.IsNullable,
                DefaultValue = entityColumn.DefaultValue,
                ListEntityId = entityColumn.ListEntityId,
                ListEntityKey = entityColumn.ListEntityKey,
                ListEntityValue = entityColumn.ListEntityValue,
                True = entityColumn.True,
                False = entityColumn.False,
                ColumnPrimaryKey = entityColumn.ColumnPrimaryKey,
            };
        }

        public void DropTable(string oldEntityName)
        {
            var tableExists = _dbContext.EntityListMetadataModels.FromSqlRaw("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}", oldEntityName).Any();
            if (tableExists)
            {
                var dropTableSql = $"DROP TABLE \"{oldEntityName}\"";
                _dbContext.Database.ExecuteSqlRaw(dropTableSql);
            }
            else
            {
                // Log or handle the case where the table doesn't exist
            }
        }

        //private void DropTables(string oldEntityName)
        //{
        //    try
        //    {
        //        // Assuming _dbContext is your DbContext instance
        //        var tableExists = _dbContext.EntityListMetadataModels
        //            .FromSqlRaw("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}", oldEntityName)
        //            .Any();

        //        // Check if the table exists before attempting to drop it
        //        if (tableExists)
        //        {
        //            // Execute raw SQL command to drop the table
        //            _dbContext.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS {oldEntityName} CASCADE;");
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Table '{oldEntityName}' does not exist.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle or log the exception as needed
        //        Console.WriteLine($"An error occurred while dropping the table: {ex.Message}");
        //    }
        //}


        public async Task<IDictionary<string, bool>> TablesHaveValuesAsync(List<string> tableNames)
        {
            var tablesWithValues = new Dictionary<string, bool>();

            try
            {
                foreach (var tableName in tableNames)
                {
                    var tableExists = await TableExistsAsync(tableName);
                    if (!tableExists)
                    {
                        tablesWithValues.Add(tableName, false);
                        continue;
                    }

                    var sql = $"SELECT 1 FROM \"{tableName}\" LIMIT 1";
                    var tableHasValues = await _dbContext.EntityListMetadataModels
                        .FromSqlRaw(sql)
                        .AnyAsync();
                    tablesWithValues.Add(tableName, tableHasValues);
                }

                return tablesWithValues;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while checking if tables have values: {ex.Message}");
                return new Dictionary<string, bool>();
            }
        }


        public async Task<(string EntityName, string EntityKeyColumnName, string EntityValueColumnName)> GetEntityData(int ListEntityId, int ListEntityKey, int ListEntityValue)
        {
            try
            {
                var entityName = await _dbContext.EntityListMetadataModels
                    .Where(entity => entity.Id == ListEntityId)
                    .Select(entity => entity.EntityName)
                    .FirstOrDefaultAsync();
                var entityKeyColumnName = await _dbContext.EntityColumnListMetadataModels
                    .Where(column => column.Id == ListEntityKey)
                    .Select(column => column.EntityColumnName)
                    .FirstOrDefaultAsync();
                var entityValueColumnName = await _dbContext.EntityColumnListMetadataModels
                    .Where(column => column.Id == ListEntityValue)
                    .Select(column => column.EntityColumnName)
                    .FirstOrDefaultAsync();
                if (entityName != null && entityKeyColumnName != null && entityValueColumnName != null)
                {
                    return (entityName, entityKeyColumnName, entityValueColumnName);
                }
                return (string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching entity data: {ex.Message}");
                return (string.Empty, string.Empty, string.Empty);
            }
        }

    }
}
