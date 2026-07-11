namespace Jellyfin.Plugin.Hardcover.Api;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("Hardcover")]
[Authorize(Policy = "RequiresElevation")]
public class HardcoverApiController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HardcoverApiController> _logger;

    public HardcoverApiController(IHttpClientFactory httpClientFactory, ILogger<HardcoverApiController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("TestApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> TestApiKey([FromQuery] string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BadRequest("API key cannot be blank.");
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.hardcover.app/v1/graphql");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.UserAgent.ParseAdd("Jellyfin-Hardcover-Plugin/1.0");

            // A minimal query asking for information about the bearer token owner
            request.Content = new StringContent(
                "{\"query\": \"query { me { id username } }\"}",
                System.Text.Encoding.UTF8,
                MediaTypeNames.Application.Json);

            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            var contentString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode && !contentString.Contains("\"errors\":"))
            {
                return Ok("Successfully authenticated with Hardcover.");
            }

            _logger.LogWarning("Hardcover API key validation failed. Response: {Response}", contentString);
            return Unauthorized("The API key was rejected by Hardcover.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error validating Hardcover API key.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to connect to the Hardcover API.");
        }
    }
}
