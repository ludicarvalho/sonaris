using Microsoft.AspNetCore.Mvc;

namespace Sonaris.Controllers;

using Sonaris.Domain.Infrastructure.Response;

/// <summary>
/// Base Controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BaseController : ControllerBase
{
    /// <summary>
    /// Monta o IActionResult de acordo com a resposta. (<see cref="BaseResponseAbstract.Success"/>)
    /// </summary>
    protected IActionResult Result(BaseResponseAbstract response)
    {
        if (response == null)
        {
            return BadRequest(new BaseResponse<string>
            {
                Message = "Não foi possível montar os dados para a sua requisição.",
                ErrorDetails = "API Controller Error. No Response."
            });
        }

        if (response.StatusCode.HasValue)
            return StatusCode((int)response.StatusCode.Value, response);

        if (response.Success)
            return Ok(response);
        else
            return BadRequest(response);
    }
}
