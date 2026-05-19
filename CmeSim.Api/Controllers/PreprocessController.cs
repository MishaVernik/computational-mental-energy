using CmeSim.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CmeSim.Api.Controllers;

/// <summary>
/// Minimal API endpoint for preprocessing service (Architecture B).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PreprocessController : ControllerBase
{
    private readonly PreprocessService _preprocessService;
    private readonly ILogger<PreprocessController> _logger;

    public PreprocessController(
        PreprocessService preprocessService,
        ILogger<PreprocessController> logger)
    {
        _preprocessService = preprocessService;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Preprocess([FromBody] PreprocessRequest request)
    {
        try
        {
            var preprocessed = _preprocessService.Preprocess(request.Features);
            return Ok(new PreprocessResponse { Features = preprocessed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preprocessing failed");
            return StatusCode(500, "Preprocessing failed");
        }
    }
}

public class PreprocessRequest
{
    public double[] Features { get; set; } = Array.Empty<double>();
}

public class PreprocessResponse
{
    public double[] Features { get; set; } = Array.Empty<double>();
}

