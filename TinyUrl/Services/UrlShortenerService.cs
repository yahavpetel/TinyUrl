using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Reflection.Metadata.Ecma335;
using System.Web;
using System.Xml.Serialization;
using TinyUrl.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;


namespace TinyUrl.Services
{
    public interface IUrlShortenerService
    {
        Task<string> GetShortenUrl(string longUrl);
        Task<string?> GetLongUrl(string shortUrl);
    }

    public class UrlShortenerService : IUrlShortenerService
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoCollection<UrlMapping> _urlMappings;
        private readonly LRUCache<string, string> _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Semaphore for controlling access to the cache


        public UrlShortenerService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;

            // getting configuration
            int cacheCapacity = int.Parse(configuration["Cache:Capacity"]);
            string connectionString = configuration["MongoDB:ConnectionString"];
            string databaseName = configuration["MongoDB:DatabaseName"];

            // initializing db connection and cache
            _mongoClient = new MongoClient(connectionString);
            var database = _mongoClient.GetDatabase(databaseName);
            _urlMappings = database.GetCollection<UrlMapping>("UrlMappings");
            _cache = new LRUCache<string, string>(cacheCapacity);
        }

        public string GetCurrentServerAddressAsync()
        {
            var request = _httpContextAccessor.HttpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }


        public async Task<string> GetShortenUrl(string longUrl)
        {
            string uuid = await GetShortenUrlUUIDAsync(longUrl);
            return GetCurrentServerAddressAsync() + "/Url/" + uuid;
        }

        public async Task<string> GetShortenUrlUUIDAsync(string longUrl)
        {
            string shortUrl = string.Empty;

            // preventing multiple requests from accessing this function simultaneously
            // to prevent creating the same UUID.
            await _semaphore.WaitAsync();

            try
            {
                // Check again after acquiring the semaphore to handle concurrent updates
                if (_cache.TryGetValue(longUrl, out var shortUrlFromCache))
                {
                    return shortUrlFromCache;
                }

                var existingMapping = await _urlMappings.Find(mapping => mapping.LongUrl == longUrl).FirstOrDefaultAsync();
                if (existingMapping != null)
                {
                    shortUrl = existingMapping.ShortUrl;
                    _cache.Add(longUrl, shortUrl);
                    return shortUrl;
                }

                shortUrl = await GenerateShortUrlAsync(longUrl);

                await _urlMappings.InsertOneAsync(new UrlMapping { LongUrl = longUrl, ShortUrl = shortUrl });

                _cache.Add(longUrl, shortUrl);
            }
            finally
            {
                _semaphore.Release();
            }

            return shortUrl;
        }

        public async Task<string?> GetLongUrl(string uuid)
        {
            // Check if the short URL is already in the cache
            if (_cache.TryGetValue(uuid, out var longUrlFromCache))
            {
                return longUrlFromCache;
            }

            var mapping = await _urlMappings.Find(mapping => mapping.ShortUrl == uuid).FirstOrDefaultAsync();
            if (mapping != null)
            {
                string longUrl = mapping.LongUrl;
                _cache.Add(uuid, longUrl);
                return longUrl;
            }
            return null; 
        }

        private async Task<string> GenerateShortUrlAsync(string longUrl)
        {
            while (true)
            {
                Guid uuid = Guid.NewGuid();

                string uuidString = uuid.ToString("N").Substring(0, 6);
                string shortUrl = longUrl.GetHashCode().ToString("x") + uuidString;
                if (!await ShortUrlExistsInDatabaseAsync(shortUrl))
                {
                    return shortUrl;
                }
            }
        }

        private async Task<bool> ShortUrlExistsInDatabaseAsync(string shortUrl)
        {
            var filter = Builders<UrlMapping>.Filter.Eq(mapping => mapping.ShortUrl, shortUrl);
            var mapping = await _urlMappings.Find(filter).FirstOrDefaultAsync();

            return mapping != null;
        }
    }
}
