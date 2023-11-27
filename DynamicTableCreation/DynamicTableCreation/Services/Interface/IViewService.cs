using DynamicTableCreation.Models.DTO;

namespace DynamicTableCreation.Services.Interface
{
    public interface IViewService
    {
        public IEnumerable<EntityColumnDTO> GetColumnsForEntity(string entityName);
        public Task<(string TableName, List<dynamic> Rows)> GetTableDataByListEntityId(int listEntityId);
    }
}
