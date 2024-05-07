using Microsoft.AspNetCore.Mvc;
using TinyUrl.Services;


namespace TinyUrl.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UrlController : ControllerBase
    {
        private readonly IUrlShortenerService _urlShortenerService;

        public UrlController(IUrlShortenerService urlShortenerService)
        {
            _urlShortenerService = urlShortenerService;
        }

        [HttpPost]
        public IActionResult ShortenUrl([FromBody] string longUrl)
        {
            string shortUrl = _urlShortenerService.GetShortenUrl(longUrl);
            return Ok(shortUrl);
        }



        [HttpGet("{shortUrl}")]
        public IActionResult RedirectUrl(string shortUrl)
        {
            string longUrl = _urlShortenerService.GetLongUrl(shortUrl);
            if (longUrl == null)
                return NotFound();

            return Redirect(longUrl);
        }
    }
}
