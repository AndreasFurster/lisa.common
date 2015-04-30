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
    /// <summary>
    /// Proxy or/and access layer which communicates with an JSON API.
    /// </summary>
    /// <typeparam name="T">Resource type of the proxy. Use the same modal as the API returns</typeparam>
    public class Proxy<T> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Proxy{T}"/>.
        /// </summary>
        /// <param name="resourceUrl">The URL to the resource of the api. Example: http://example.com/<see cref="T"/></param>
        /// <param name="jsonSerializerSettings">Optional: JSON serializer settings if you want to override the defaults.</param>
        public Proxy(string resourceUrl, JsonSerializerSettings jsonSerializerSettings = null)
        {
            _proxyResourceUrl = new Uri(resourceUrl.Trim('/'));
            _httpClient = new HttpClient();

            _jsonSerializerSettings = jsonSerializerSettings ?? new JsonSerializerSettings
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

        /// <summary>
        /// Gets or sets the token which will be used if available in the Authorization HTTP header.
        /// </summary>
        public Token Token { get; set; }

        /// <summary>
        /// Gets a list of all resources from the API
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="redirectUriList">The redirect URI list.</param>
        /// <returns>Task&lt;IEnumerable&lt;T&gt;&gt;.</returns>
        /// <exception cref="WebApiException">
        /// Redirect without Location provided
        /// or
        /// Unexpected status code
        /// </exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public async Task<IEnumerable<T>> GetAsync()
        {
            return await GetAsync(null, null);
        }

        public async Task<T> GetAsync(int id)
        {
            return await GetAsync(id, null, null);
        }

        public async Task<T> PostAsync()
        {
            return await PostAsync(null, null);
        }

        public async Task<IEnumerable<T>> GetAsync()
        {
            return await GetAsync(null, null);
        }

        public async Task<IEnumerable<T>> GetAsync()
        {
            return await GetAsync(null, null);
        }
        



        private async Task<T> GetAsync(int id, Uri uri = null, List<Uri> redirectUriList = null)
        {
            CheckRedirectLoop(uri, ref redirectUriList);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = uri ?? new Uri(string.Format("{0}/{1}", _proxyResourceUrl, id))
            };

            AddAuthorizationHeader(ref request);

            var result = await _httpClient.SendAsync(request);

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
                    throw new WebApiException("Redirect without Location provided", result.StatusCode);

                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new UnauthorizedAccessException();

                case HttpStatusCode.NotFound:
                case HttpStatusCode.Gone:
                    return null;
            }

            throw new WebApiException("Unexpected status code", result.StatusCode);
        }

        /// <summary>
        /// post as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="redirectUriList">The redirect URI list.</param>
        /// <returns>Task&lt;T&gt;.</returns>
        /// <exception cref="WebApiException">
        /// Redirect without Location provided
        /// or
        /// Unexpected status code
        /// </exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        private async Task<T> PostAsync(T model, Uri uri = null, List<Uri> redirectUriList = null)
        {
            CheckRedirectLoop(uri, ref redirectUriList);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = _proxyResourceUrl,
                Content = new StringContent(JsonConvert.SerializeObject(model, _jsonSerializerSettings), Encoding.UTF8, "Application/json")
            };

            AddAuthorizationHeader(ref request);

            var result = await _httpClient.SendAsync(request);

            switch (result.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                case HttpStatusCode.Accepted:
                case HttpStatusCode.BadRequest:
                    return await DeserializeSingle(result);

                case HttpStatusCode.TemporaryRedirect:
                case HttpStatusCode.Redirect:
                case HttpStatusCode.RedirectMethod:
                    if (result.Headers.Location != null)
                    {
                        redirectUriList.Add(result.Headers.Location);
                        return await PostAsync(model, result.Headers.Location, redirectUriList);
                    }
                    throw new WebApiException("Redirect without Location provided", result.StatusCode);

                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new UnauthorizedAccessException();
            }

            throw new WebApiException("Unexpected status code", result.StatusCode);
        }

        /// <summary>
        /// patch as an asynchronous operation.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="model">The model.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="redirectUriList">The redirect URI list.</param>
        /// <returns>Task&lt;T&gt;.</returns>
        /// <exception cref="WebApiException">
        /// Redirect without Location provided
        /// or
        /// Unexpected status code
        /// </exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        private async Task<T> PatchAsync(int id, T model, Uri uri = null, List<Uri> redirectUriList = null)
        {
            CheckRedirectLoop(uri, ref redirectUriList);

            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(String.Format("{0}/{1}", _proxyResourceUrl, id)),
                Content = new StringContent(JsonConvert.SerializeObject(model, _jsonSerializerSettings), Encoding.UTF8, "Application/json")
            };

            AddAuthorizationHeader(ref request);

            var result = await _httpClient.SendAsync(request);

            switch (result.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Accepted:
                case HttpStatusCode.NoContent:
                case HttpStatusCode.BadRequest:
                    return await DeserializeSingle(result);

                case HttpStatusCode.TemporaryRedirect:
                case HttpStatusCode.Redirect:
                case HttpStatusCode.RedirectMethod:
                    if (result.Headers.Location != null)
                    {
                        redirectUriList.Add(result.Headers.Location);
                        return await PostAsync(model, result.Headers.Location, redirectUriList);
                    }
                    throw new WebApiException("Redirect without Location provided", result.StatusCode);

                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new UnauthorizedAccessException();
            }

            throw new WebApiException("Unexpected status code", result.StatusCode);
        }

        /// <summary>
        /// delete as an asynchronous operation.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="redirectUriList">The redirect URI list.</param>
        /// <returns>Task.</returns>
        /// <exception cref="WebApiException">
        /// Redirect without Location provided
        /// or
        /// Unexpected status code
        /// </exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        private async Task DeleteAsync(int id, Uri uri = null, List<Uri> redirectUriList = null)
        {
            CheckRedirectLoop(uri, ref redirectUriList);

            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("DELETE"),
                RequestUri = new Uri(String.Format("{0}/{1}", _proxyResourceUrl, id))
            };

            AddAuthorizationHeader(ref request);

            var result = await _httpClient.SendAsync(request);

            switch (result.StatusCode)
            {
                case HttpStatusCode.Accepted:
                case HttpStatusCode.NoContent:
                    return;

                case HttpStatusCode.TemporaryRedirect:
                case HttpStatusCode.Redirect:
                case HttpStatusCode.RedirectMethod:
                    if (result.Headers.Location != null)
                    {
                        redirectUriList.Add(result.Headers.Location);
                        await DeleteAsync(id, result.Headers.Location, redirectUriList);
                        return;
                    }
                    throw new WebApiException("Redirect without Location provided", result.StatusCode);

                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new UnauthorizedAccessException();
            }

            throw new WebApiException("Unexpected status code", result.StatusCode);
        }

        private async Task<IEnumerable<T>> GetAsync(Uri uri, List<Uri> redirectUriList)
        {
            CheckRedirectLoop(uri, ref redirectUriList);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = uri ?? _proxyResourceUrl
            };

            AddAuthorizationHeader(ref request);

            var result = await _httpClient.SendAsync(request);

            switch (result.StatusCode)
            {
                case HttpStatusCode.OK:
                    return await DeserializeList(result);

                case HttpStatusCode.TemporaryRedirect:
                case HttpStatusCode.Redirect:
                case HttpStatusCode.RedirectMethod:
                    if (result.Headers.Location != null)
                    {
                        redirectUriList.Add(result.Headers.Location);
                        return await GetAsync(result.Headers.Location, redirectUriList);
                    }
                    throw new WebApiException("Redirect without Location provided", result.StatusCode);

                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new UnauthorizedAccessException();

                case HttpStatusCode.NotFound:
                case HttpStatusCode.Gone:
                    return null;
            }

            throw new WebApiException("Unexpected status code", result.StatusCode);
        }

        private void CheckRedirectLoop(Uri uri, ref List<Uri> redirectUriList)
        {
            if (redirectUriList != null && redirectUriList.Contains(uri))
            {
                throw new WebApiException("Endless redirect loop", HttpStatusCode.Redirect);
            }

            redirectUriList = new List<Uri>();
        }

        private void AddAuthorizationHeader(ref HttpRequestMessage request)
        {
            if (Token != null && !string.IsNullOrEmpty(Token.Value))
            {
                request.Headers.Add("Authorization", String.Format("{0} {1}", Token.Type, Token.Value));
            }
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