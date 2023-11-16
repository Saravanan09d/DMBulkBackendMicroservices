using DynamicTableCreation.Data;
using DynamicTableCreation.Models;
using DynamicTableCreation.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
                            createTableSql += $"varchar";
                            if (column.MaxLength > 0)
                            {
                                createTableSql += $"({column.MaxLength})";
                            }
                            else
                            {
                                createTableSql += "(255)";
                            }
                            break;
                        case "char":
                            createTableSql += $"char";
                            if (column.Length == 1)
                            {
                                createTableSql += $"({column.Length})";
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

        public void UpdateEntityColumn(int entityId, string newEntityName, List<EntityColumnProperties> newEntityColumns)
        {

            string oldEntityName = GetOldEntityName(entityId);
            DropTable(oldEntityName);
            var existingEntity = _dbContext.EntityListMetadataModels
                .Include(e => e.EntityColumns)
                .FirstOrDefault(e => e.Id == entityId);

            if (existingEntity == null)
            {
                return;
            }

            existingEntity.EntityName = newEntityName;

            foreach (var newColumn in newEntityColumns)
            {
                var existingColumn = existingEntity.EntityColumns
                    .FirstOrDefault(c => c.EntityColumnName == newColumn.EntityColumnName);

                if (existingColumn != null)
                {
                    existingColumn.EntityColumnName = newColumn.EntityColumnName;
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
                else
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
                        EntityId = entityId // Set the reference to the parent entity
                    };
                    existingEntity.EntityColumns.Add(entityColumn);
                }
            }
            _dbContext.SaveChanges();
            var createTableSql = GenerateCreateTableSql(new TableCreationRequest
            {
                TableName = newEntityName,
                Columns = newEntityColumns.Select(ConvertToColumnDefinition).ToList()
            });
            _dbContext.Database.ExecuteSqlRaw(createTableSql);
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
                // Map other properties as needed...
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
        public async Task<Dictionary<string, bool>> CheckTablesHasValuesAsync(List<string> tableNames)
        {
            var result = new Dictionary<string, bool>();

            foreach (var tableName in tableNames)
            {
                var tableHasValues = await TableHasValuesAsync(tableName);
                result.Add(tableName, tableHasValues);
            }

            return result;
        }

        public async Task<bool> TableHasValuesAsync(string tableName)
        {
            try
            {
                var lowerCaseTableName = tableName.ToLower();
                var existingEntity = await _dbContext.EntityListMetadataModels
                    .AnyAsync(e => e.EntityName.ToLower() == lowerCaseTableName);

                if (!existingEntity)
                {
                    // Table doesn't exist, consider it doesn't have values
                    return false;
                }

                var tableHasValues = await _dbContext.Database
                    .ExecuteSqlRawAsync($"SELECT 1 FROM \"{tableName}\" LIMIT 1");
                return tableHasValues > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while checking if table '{tableName}' has values: {ex.Message}");
                return false;
            }
        }

    }
}
