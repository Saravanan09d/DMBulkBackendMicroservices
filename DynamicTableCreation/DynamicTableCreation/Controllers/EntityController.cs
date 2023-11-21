using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using DynamicTableCreation.Services.Interface;
using System.Net;
using DynamicTableCreation.Models;
using DynamicTableCreation.Services;
using DynamicTableCreation.Models.DTO;



namespace ExcelGeneration.Controllers
{
   
    [ApiController]
    [EnableCors("AllowAngularDev")]
    public class EntityController : ControllerBase
    {
        private readonly EntityService _dynamicDbService;
        private readonly IEntitylistService _entitylistService;
        protected APIResponse _response;
        private readonly ViewService _viewService;
        public EntityController(EntityService dynamicDbService, IEntitylistService entitylistService, ViewService viewService)
        {
            _dynamicDbService = dynamicDbService;
            _entitylistService = entitylistService;
            _viewService = viewService;
            _response = new();
        }

        [HttpPost("create-table")]
        [EnableCors("AllowAngularDev")]
        public async Task<ActionResult> CreateTable([FromBody] TableCreationRequestDTO request)
        {
            try
            {
                if (request == null)
                {
                    var response = new APIResponse
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false,
                        ErrorMessage = new List<string> { "Invalid request data." },
                        Result = null
                    };
                    return BadRequest(response);
                }
                var existingTable = await _dynamicDbService.TableExistsAsync(request.TableName);
                if (existingTable)
                {
                    var response = new APIResponse
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false,
                        ErrorMessage = new List<string> { $"Table '{request.TableName}' already exists." },
                        Result = null
                    };
                    return BadRequest(response);
                }
                var tableCreationRequest = _dynamicDbService.MapToModel(request);
                bool tableCreated = await _dynamicDbService.CreateDynamicTableAsync(tableCreationRequest);

                if (tableCreated)
                {
                    var response = new APIResponse
                    {
                        StatusCode = HttpStatusCode.OK,
                        IsSuccess = true,
                        ErrorMessage = new List<string>(),
                        Result = $"Table '{request.TableName}' created successfully."
                    };
                    return Ok(response);
                }
                else
                {
                    var response = new APIResponse
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        IsSuccess = false,
                        ErrorMessage = new List<string> { $"An error occurred while creating the table '{request.TableName}'." },
                        Result = null
                    };
                    return StatusCode((int)HttpStatusCode.InternalServerError, response);
                }
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    IsSuccess = false,
                    ErrorMessage = new List<string> { ex.Message },
                    Result = null
                };
                Console.WriteLine(ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, response);
            }
        }

        //EntitylistService
        [HttpGet]
        [ProducesResponseType(200)]
        [Route("api/entitylist")]
        [EnableCors("AllowAngularDev")]
        public ActionResult<IEnumerable<EntityListDto>> Get()
        {
            try
            {
                var tablename = _entitylistService.GetEntityList();

                if (tablename == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessage.Add("No Data Available");
                    return BadRequest(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = tablename;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessage.Add($"An error occurred while processing the request: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        //ViewService
        [HttpGet("{entityName}/columns")]
        [EnableCors("AllowAngularDev")]
        public IActionResult GetColumnsForEntity(string entityName)
        {
            try
            {
                var columnsDTO = _viewService.GetColumnsForEntity(entityName);
                // Assuming you have a ListEntityId in your columns
                int listEntityId = columnsDTO.FirstOrDefault()?.ListEntityId ?? 0;
                // Retrieve data from the database using the service method
                var result = _viewService.GetTableDataByListEntityId(listEntityId).Result; // Use .Result to block until completion
                if (columnsDTO == null)
                {
                    return NotFound(new APIResponse
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        IsSuccess = false,
                        ErrorMessage = new List<string> { "Table not found" },
                        Result = null
                    });
                }
                return Ok(new APIResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccess = true,
                    Result = columnsDTO
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new APIResponse
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    IsSuccess = false,
                    ErrorMessage = new List<string> {$"An error occurred while processing the request: {ex.Message} " },
                    Result = null
                });
            }
        }

        [HttpPost("updateEntityColumn")]
        public IActionResult UpdateEntityColumn([FromBody] UpdateEntityColumnRequestModel request)
        {
            try
            {
                if (request?.Update?.PropertiesList == null)
                {
                    return BadRequest("Invalid request data. Update.PropertiesList cannot be null.");
                }


                _dynamicDbService.UpdateEntityColumn(request.EntityId, request.EntityName, request.Update.PropertiesList);

                return Ok("Entity column updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the entity column: {ex.Message}");
            }
        }

        [HttpPost("api/entity/has-values")]
        [EnableCors("AllowAngularDev")]
        public async Task<ActionResult<IDictionary<string, bool>>> CheckTablesHaveValues([FromBody] List<string> tableNames)
        {
            try
            {
                var tablesWithValues = new Dictionary<string, bool>();
                foreach (var tableName in tableNames)
                {
                    var tableHasValues = await _dynamicDbService.TableHasValuesAsync(tableName);
                    tablesWithValues.Add(tableName, tableHasValues);
                }
                return Ok(tablesWithValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while checking if tables have values: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, new APIResponse
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    IsSuccess = false,
                    ErrorMessage = new List<string> { $"An error occurred while checking if tables have values: {ex.Message}" },
                    Result = null
                });
            }
        }
    }
}

