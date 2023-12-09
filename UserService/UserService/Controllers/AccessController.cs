using Microsoft.AspNetCore.Mvc;
using UserService.Models.DTO;
using UserService.Services;

namespace UserService.Controllers
{
    public class AccessController : Controller
    {

            private readonly AccessService _accessService;

            public AccessController(AccessService accessService)
            {
                _accessService = accessService;
            }

            [HttpPost("login")]
            public async Task<IActionResult> Login([FromBody] LoginViewModelDTO model)
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var isAuthenticated = await _accessService.AuthenticateAsync(model.Name, model.Password);

                if (isAuthenticated)
                {
                    return Ok();
                }
                else
                {
                    // Authentication failed
                    return BadRequest("Invalid credentials");
                }
            }
    }
}
