using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Reflection.Metadata.Ecma335;
using System.Web;
using System.Xml.Serialization;
using TinyUrl.Models;
using Microsoft.AspNetCore.Http;


namespace TinyUrl.Services
{
    public interface IUrlShortenerService
    {
        string GetShortenUrl(string longUrl);
        string GetLongUrl(string shortUrl);
    }

    public class UrlShortenerService : IUrlShortenerService
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoCollection<UrlMapping> _urlMappings;
        private readonly LRUCache<string, string> _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public UrlShortenerService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;

            // todo complete - move to config
            int cacheCapacity = 10;

            string connectionString = configuration["MongoDB:ConnectionString"];
            string databaseName = configuration["MongoDB:DatabaseName"];

            _mongoClient = new MongoClient(connectionString);
            var database = _mongoClient.GetDatabase(databaseName);
            _urlMappings = database.GetCollection<UrlMapping>("UrlMappings");
            _cache = new LRUCache<string, string>(cacheCapacity);

        }

        public string GetCurrentServerAddress()
        {
            var request = _httpContextAccessor.HttpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }

        public string GetShortenUrl(string longUrl)
        {
            String uuid = GetShortenUrlUUID(longUrl);
            return GetCurrentServerAddress() + "/Url/" + uuid;
        }

        public string GetShortenUrlUUID(string longUrl)
        {
            // Check if the long URL is already in the cache
            if (_cache.TryGetValue(longUrl, out var shortUrlFromCache))
            {
                return shortUrlFromCache;
            }

            string shortUrl = string.Empty;
            var existingMapping = _urlMappings.Find(mapping => mapping.LongUrl == longUrl).FirstOrDefault();
            if (existingMapping != null)
            {
                shortUrl = existingMapping.ShortUrl; // Access ShortUrl property directly
                                                            // Add mapping to the cache
                _cache.Add(longUrl, shortUrl);
                return shortUrl;
            }

            // Generate short URL
            shortUrl = GenerateShortUrl(longUrl);

            // Store mapping in the database
            _urlMappings.InsertOne(new UrlMapping { LongUrl = longUrl, ShortUrl = shortUrl });

            // Add mapping to the cache
            _cache.Add(longUrl, shortUrl);

            return shortUrl;
        }

        public string GetLongUrl(string shortUrl)
        {
            string decodeShortUrl = HttpUtility.UrlDecode(shortUrl);
            // Check if the short URL is already in the cache
            if (_cache.TryGetValue(decodeShortUrl, out var longUrlFromCache))
            {
                return longUrlFromCache;
            }

            // Retrieve long URL from the database
            var mapping = _urlMappings.Find(mapping => mapping.ShortUrl == decodeShortUrl).FirstOrDefault();
            if (mapping != null)
            {
                string longUrl = mapping.LongUrl;
                // Add mapping to the cache
                _cache.Add(decodeShortUrl, longUrl);
                return longUrl;
            }
            return null; // Short URL not found
        }


        private string GenerateShortUrl(string longUrl)
        {
            while (true)
            {
                // Generate a random UUID
                Guid uuid = Guid.NewGuid();

                // Convert the UUID to a string and remove hyphens
                string uuidString = uuid.ToString("N").Substring(0, 6);

                // Concatenate the longUrl with the UUID to ensure uniqueness
                string shortUrl = longUrl.GetHashCode().ToString("x") + uuidString;

                // Check if the short URL already exists in the database
                if (!ShortUrlExistsInDatabase(shortUrl))
                {
                    return shortUrl;
                }
            }
        }

        private bool ShortUrlExistsInDatabase(string shortUrl)
        {
            var filter = Builders<UrlMapping>.Filter.Eq(mapping => mapping.ShortUrl, shortUrl);
            var mapping = _urlMappings.Find(filter).FirstOrDefault();

            if(mapping != null)
            {
                return true;
            }
            return false;
        }

        private string RetrieveLongUrlFromDatabase(string shortUrl)
        {
            return "";
            // Retrieve long URL from the database logic (omitted for brevity)
        }
        
    }
}
