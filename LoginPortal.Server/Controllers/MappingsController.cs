using LoginPortal.Server.Services;
using LoginPortal.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LoginPortal.Server.Controllers
{
    [ApiController]
    [Route("v1/mappings")]
    public class MappingsController : ControllerBase
    {
        private readonly IMappingService _mappingService;
        private readonly ILogger<MappingsController> _logger;

        public MappingsController(IMappingService mappingService, ILogger<MappingsController> logger)
        {
            _mappingService = mappingService;
            _logger = logger;
        }

        // GET /v1/mappings?type={company | module | all}...
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMappings([FromQuery] string? type = "all")
        {
            try
            {
                switch (type?.ToLowerInvariant())
                {
                    case "company":
                        return Ok(await _mappingService.GetCompaniesAsync());

                    case "module":
                        return Ok(await _mappingService.GetModulesAsync());

                    case "all":
                    case null:
                        IDictionary<string,string> companies = await _mappingService.GetCompaniesAsync();
                        IDictionary<string,string> modules = await _mappingService.GetModulesAsync();

                        if (companies.Count + modules.Count > 3500)
                        {
                            _logger.LogWarning("Cookie mapping data is getting large!!! Consider local/session caching...");
                        }

                        Response.Cookies.Append("company_mapping", JsonSerializer.Serialize(companies), CookieService.AccessOptions());
                        Response.Cookies.Append("module_mapping", JsonSerializer.Serialize(modules), CookieService.AccessOptions());

                        return Ok(new { companies, modules });

                    default:
                        return BadRequest(new { message = "type must be company, module or all" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mappings");
                return StatusCode(500, new { message = "Server error while retrieving mappings." });
            }
        }
    }
}
