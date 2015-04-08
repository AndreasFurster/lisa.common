using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Lisa.Common.Access
{
    public class Proxy<T> where T : class 
    {
        public Proxy(string resourceUrl)
        {
            _proxyResourceUrl = new Uri(resourceUrl.Trim('/'));
            _httpClient = new HttpClient();

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
        }

        public Proxy(string resourceUrl, string tokenType, string token)
            : this(resourceUrl)
        {
            if (token != null && tokenType != null)
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", String.Format("{0} {1}", tokenType, token));
            }
        }

        public async Task<IEnumerable<T>> GetAsync(Uri uri = null, List<Uri> redirectUriList = null)
        {
            if (redirectUriList != null)
            {
                if (redirectUriList.Contains(uri))
                {
                    throw new Exception("Endless redirect loop");
                }
            }
            else
            {
                redirectUriList = new List<Uri>();
            }

            var result = await _httpClient.GetAsync(uri ?? _proxyResourceUrl);

            switch (result.StatusCode)
            {
                case HttpStatusCode.OK:
                    return await DeserializeList(result);

                case HttpStatusCode.TemporaryRedirect:
                    if (result.Headers.Location != null)
                    {
                        redirectUriList.Add(result.Headers.Location);
                        return await GetAsync(result.Headers.Location, redirectUriList);
                    }
                    break;

                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new Exception("Unauthorized");

                case HttpStatusCode.NotFound:
                case HttpStatusCode.Gone:
                    return null;
            }

            throw new Exception("Unexpected statuscode");
        }


        public async Task<T> GetAsync(int id, Uri uri = null, List<Uri> redirectUriList = null)
        {
            if (redirectUriList != null)
            {
                if (redirectUriList.Contains(uri))
                {
                    throw new Exception("Endless redirect loop");
                }
            }
            else
            {
                redirectUriList = new List<Uri>();
            }

            var result = await _httpClient.GetAsync(uri ?? new Uri(string.Format("{0}/{1}", _proxyResourceUrl, id)));

            switch (result.StatusCode)
            {
                case HttpStatusCode.OK:
                    return await DeserializeSingle(result);

                case HttpStatusCode.TemporaryRedirect:
                case HttpStatusCode.Redirect:
                case HttpStatusCode.RedirectMethod:
                    if (result.Headers.Location != null)
                    {
                        redirectUriList.Add(result.Headers.Location);
                        return await GetAsync(id, result.Headers.Location, redirectUriList);
                    }
                    break;

                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new Exception("Unauthorized");

                case HttpStatusCode.NotFound:
                case HttpStatusCode.Gone:
                    return null;
            }

            throw new Exception("Unexpected statuscode");
        }

        public async Task<T> PostAsync(T model)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = _proxyResourceUrl,
                Content = new StringContent(JsonConvert.SerializeObject(model, _jsonSerializerSettings), Encoding.UTF8, "Application/json")
            };

            var result = await _httpClient.SendAsync(request);

            switch (result.StatusCode)
            {
                
            }

            return await DeserializeSingle(result);
        }

        public async Task<T> PatchAsync(int id, T model)
        {
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(String.Format("{0}/{1}", _proxyResourceUrl, id)),
                Content = new StringContent(JsonConvert.SerializeObject(model, _jsonSerializerSettings), Encoding.UTF8, "application/json")
            };

            var result = await _httpClient.SendAsync(request);
            return await DeserializeSingle(result);
        }

        public async Task<T> DeleteAsync(int id, T model)
        {
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(String.Format("{0}/{1}", _proxyResourceUrl, id)),
                Content = new StringContent(JsonConvert.SerializeObject(model, _jsonSerializerSettings), Encoding.UTF8, "application/json")
            };

            var result = await _httpClient.SendAsync(request);
            return await DeserializeSingle(result);
        }

        private async Task<T> DeserializeSingle(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        }

        private async Task<IEnumerable<T>> DeserializeList(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<IEnumerable<T>>(json, _jsonSerializerSettings);
        }

        private readonly HttpClient _httpClient;
        private readonly Uri _proxyResourceUrl;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
    }


}