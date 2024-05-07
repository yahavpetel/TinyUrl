using Microsoft.AspNetCore.Mvc;
using TinyUrl.Services;



[ApiController]
[Route("[controller]")]
public class UrlController : ControllerBase
{
    private readonly IUrlShortenerService _urlShortenerService;

    public UrlController(IUrlShortenerService urlShortenerService)
    {
        _urlShortenerService = urlShortenerService;
    }

    [HttpPost("ShortenUrl")]
    public async Task<IActionResult> ShortenUrl(string longUrl)
    {
        if (string.IsNullOrEmpty(longUrl))
        {
            return BadRequest("The 'longUrl' parameter cannot be null or empty.");
        }

        try
        {
            string shortUrl = await _urlShortenerService.GetShortenUrl(longUrl);
            return Ok(new { url = shortUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while shortening the URL.");
        }
    }

    [HttpGet("{shortUrl}")]
    public async Task<IActionResult> RedirectUrl(string shortUrl)
    {
        if (string.IsNullOrEmpty(shortUrl))
        {
            return BadRequest("The 'shortUrl' parameter cannot be null or empty.");
        }

        try
        {
            string? longUrl = await _urlShortenerService.GetLongUrl(shortUrl);
            if (longUrl == null)
            {
                return NotFound();
            }

            return Redirect(longUrl);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while redirecting.");
        }
    }
}


