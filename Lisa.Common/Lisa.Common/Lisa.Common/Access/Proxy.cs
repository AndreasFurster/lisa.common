using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Lisa.Common.Access
{
    public class Proxy<T>
    {
        public Proxy(string baseUrl, string resourceUrl) : this(baseUrl, resourceUrl, null, null)
        {
        }

        public Proxy(string baseUrl, string resourceUrl, string token, string tokenType)
        {
            _apiBaseUrl = baseUrl.Trim('/');
            _proxyResourceUrl = resourceUrl.Trim('/');
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_apiBaseUrl)
            };
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter
                    {
                        CamelCaseText = true
                    }
                }
            };

            Authorize(_httpClient, token, tokenType);
        }

        public async Task<IEnumerable<T>> GetAsync()
        {
            var result = await _httpClient.GetAsync(_proxyResourceUrl);

            var json = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<T>>(json, _jsonSerializerSettings);
        }

        public async Task<T> GetAsync(int id)
        {
            var url = String.Format("{0}/{1}", _proxyResourceUrl, id);
            var result = await _httpClient.GetAsync(url);

            var json = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        }

        public async Task<T> PostAsync(T model)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(string.Format("{0}/{1}", _apiBaseUrl, _proxyResourceUrl)),
                Content = new StringContent(JsonConvert.SerializeObject(model, _jsonSerializerSettings), Encoding.UTF8, "Application/json")
            };

            var result = await _httpClient.SendAsync(request);
            var json = await result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        }

        public async Task<T> PatchAsync(int id, T model)
        {
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(String.Format("{0}/{1}/{2}", _apiBaseUrl, _proxyResourceUrl, id)),
                Content = new StringContent(JsonConvert.SerializeObject(model, _jsonSerializerSettings), Encoding.UTF8, "application/json")
            };

            var result = await _httpClient.SendAsync(request);
            var json = await result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        }

        private void Authorize(HttpClient client, string token, string tokenType)
        {
            if (token != null && tokenType != null)
            {
                client.DefaultRequestHeaders.Add("Authorization", String.Format("{0} {1}", tokenType, token));
            }
        }

        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _proxyResourceUrl;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
    }
}