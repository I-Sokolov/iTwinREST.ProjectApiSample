/*--------------------------------------------------------------------------------------+
|
| Copyright (c) Bentley Systems, Incorporated. All rights reserved.
| See LICENSE.md in the project root for license terms and full copyright notice.
|
+--------------------------------------------------------------------------------------*/

using ItwinProjectSampleApp.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ItwinProjectSampleApp
    {
    internal class EndpointManager
        {
        private const string API_BASE_URL = "https://api.bentley.com";

        private static readonly HttpClient _client = new();

        private string _token = null;

        internal async Task<bool> Login()
            {
            //https://developer.bentley.com/apis/overview/authorization/#authorizing-service-machine-to-machine
            //curl https://ims.bentley.com/connect/_token -X POST
            //  --data-urlencode grant_type=client_credentials
            //  --data-urlencode client_id=<client_id>
            //  --data-urlencode client_secret=<client_secret>
            //  --data-urlencode scope=<scope>

            const string LOGIN_URL = "https://ims.bentley.com/connect/token";
            const string CLIENT_ID = "service-i21UlBzAs1owjbwO9J9vdc5nD";
            const string CLIENT_SECRET = "n2SRRG8o4EUV1O4ugWqM8JxDhh4Y2BIK1ebGReHjg0dLgEi13cc6ByKpuysoy5KPBSoxshkD7225X3M64rrhwA==";

            var args = new Dictionary<string, string>();
            args.Add("grant_type", "client_credentials");
            args.Add("client_id", CLIENT_ID);
            args.Add("client_secret", CLIENT_SECRET);
            args.Add("scope", "imodels:read");

            HttpContent body = new FormUrlEncodedContent(args);

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Accept", "application/vnd.bentley.itwin-platform.v2+json");

            using (var response = await _client.PostAsync(LOGIN_URL, body))
                {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                    Console.WriteLine("You should implement retry logic for TooManyRequests (429) errors and possibly others like GatewayTimeout or ServiceUnavailable");
                    return false;
                    }

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseContent))
                    {
                    var responsePayload = JObject.Parse(responseContent);
                    if (response.StatusCode == HttpStatusCode.OK)
                        {
                        // Successful response. Deserialize the object returned. This is the full representation
                        // of the new instance that was just created. It will contain the new instance Id.
                        var type = responsePayload["token_type"].ToString();
                        var token = responsePayload["access_token"].ToString();
                        if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(token))
                            {
                            _token = $"{type} {token}";
                            return true;
                            }
                        }
                    else
                        {
                        // There was an error. Deserialize the error details and return.
                        var error = responsePayload["error"]?.ToObject<ErrorDetails>();
                        Console.WriteLine(error);
                        }
                    }
                else
                    {
                    var error = "emppty loning responce";
                    Console.WriteLine(error);
                    }
                }

            throw new ApplicationException("Authorization failed");
            }


        internal async Task<HttpGetResponseMessage<T>> MakeGetCall<T> (string relativeUrl, Dictionary<string, string> customHeaders = null)
            {
            // Construct full url and then make the GET call
            var request = $"{API_BASE_URL}{relativeUrl}";

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", _token);
            _client.DefaultRequestHeaders.Add("Accept", "application/vnd.bentley.itwin-platform.v2+json");
            AddCustomHeaders(_client, customHeaders);

            using var response = await _client.GetAsync(request);

            if ( response.StatusCode == HttpStatusCode.TooManyRequests )
                {
                // You should implement retry logic for TooManyRequests (429) errors and possibly others like GatewayTimeout or ServiceUnavailable
                }

            // Copy/Deserialize the response into custom HttpGetResponseMessage.
            HttpGetResponseMessage<T> responseMsg = new HttpGetResponseMessage<T>();
            responseMsg.Status = response.StatusCode;
            responseMsg.Content = await response.Content.ReadAsStringAsync();
            var responsePayload = JObject.Parse(responseMsg.Content);
            if ( response.StatusCode == HttpStatusCode.OK )
                {
                // Successful response. Deserialize the list of objects returned.
                var containerName = $"{typeof(T).Name}s"; // The container is plural for lists
                var instances = responsePayload[containerName];
                responseMsg.Instances = new List<T>();
                foreach ( var inst in instances )
                    {
                    responseMsg.Instances.Add(inst.ToObject<T>());
                    }
                }
            else
                {
                // There was an error. Deserialize the error details and return.
                responseMsg.ErrorDetails = responsePayload["error"]?.ToObject<ErrorDetails>();
                }
            return responseMsg;
            }

        internal async Task<HttpGetSingleResponseMessage<T>> MakeGetSingleCall<T> (string url, Dictionary<string, string> customHeaders = null)
            {
            // Construct full url and then make the GET call
            var request = url.StartsWith("http") ? url : $"{API_BASE_URL}{url}";

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", _token);
            _client.DefaultRequestHeaders.Add("Accept", "application/vnd.bentley.itwin-platform.v2+json");
            AddCustomHeaders(_client, customHeaders);

            using var response = await _client.GetAsync(request);

            if ( response.StatusCode == HttpStatusCode.TooManyRequests )
                {
                // You should implement retry logic for TooManyRequests (429) errors and possibly others like GatewayTimeout or ServiceUnavailable
                }

            // Copy/Deserialize the response into custom HttpGetSingleResponseMessage.
            HttpGetSingleResponseMessage<T> responseMsg = new HttpGetSingleResponseMessage<T>();
            responseMsg.Status = response.StatusCode;
            responseMsg.Content = await response.Content.ReadAsStringAsync();
            var responsePayload = JObject.Parse(responseMsg.Content);
            if ( response.StatusCode == HttpStatusCode.OK )
                {
                // Successful response. Deserialize the object returned.
                var containerName = typeof(T).Name;
                responseMsg.Instance = responsePayload[containerName].ToObject<T>();
                }
            else
                {
                // There was an error. Deserialize the error details and return.
                responseMsg.ErrorDetails = responsePayload["error"]?.ToObject<ErrorDetails>();
                }
            return responseMsg;
            }

        internal async Task<HttpPostResponseMessage<T>> MakePostCall<T> (string relativeUrl, T propertyModel, Dictionary<string, string> customHeaders = null)
            {
            // Add any additional headers if applicable
            AddCustomHeaders(_client, customHeaders);

            var body = new StringContent(JsonSerializer.Serialize(propertyModel, JsonSerializerOptions), Encoding.UTF8, "application/json");
            HttpPostResponseMessage<T> responseMsg = new HttpPostResponseMessage<T>();

            // Construct full url and then make the POST call
            using (var response = await _client.PostAsync($"{API_BASE_URL}{relativeUrl}", body))
                {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                    // You should implement retry logic for TooManyRequests (429) errors and possibly others like GatewayTimeout or ServiceUnavailable
                    }

                // Copy/Deserialize the response into custom HttpPostResponseMessage.

                responseMsg.Status = response.StatusCode;
                responseMsg.Content = await response.Content.ReadAsStringAsync();
                }

            if (!string.IsNullOrEmpty(responseMsg.Content))
                {
                var responsePayload = JObject.Parse(responseMsg.Content);
                if (responseMsg.Status == HttpStatusCode.Created)
                    {
                    // Successful response. Deserialize the object returned. This is the full representation
                    // of the new instance that was just created. It will contain the new instance Id.
                    var containerName = typeof(T).Name.ToLower();

                    responseMsg.NewInstance = responsePayload[containerName].ToObject<T>();
                    }
                else
                    {
                    // There was an error. Deserialize the error details and return.
                    responseMsg.ErrorDetails = responsePayload["error"]?.ToObject<ErrorDetails>();
                    }
                }
            else
                responseMsg.NewInstance = propertyModel;


            return responseMsg;
            }

        internal async Task<HttpPostResponseMessage<T>> MakePostCall<T> (string relativeUrl, Dictionary<string, string> customHeaders = null)
            {
            // Add any additional headers if applicable
            AddCustomHeaders(_client, customHeaders);

            // Construct full url and then make the POST call
            using var response = await _client.PostAsync($"{API_BASE_URL}{relativeUrl}", null);

            if ( response.StatusCode == HttpStatusCode.TooManyRequests )
                {
                // You should implement retry logic for TooManyRequests (429) errors and possibly others like GatewayTimeout or ServiceUnavailable
                }

            // Copy/Deserialize the response into custom HttpPostResponseMessage.
            HttpPostResponseMessage<T> responseMsg = new HttpPostResponseMessage<T>();
            responseMsg.Status = response.StatusCode;

            if ( response.StatusCode == HttpStatusCode.OK )
                {
                // There was no payload and no expected response to return.
                responseMsg.NewInstance = default(T);
                }
            else
                {
                // There was an error. Deserialize the error details and return.
                responseMsg.Content = await response.Content.ReadAsStringAsync();
                var responsePayload = JObject.Parse(responseMsg.Content);
                responseMsg.ErrorDetails = responsePayload["error"]?.ToObject<ErrorDetails>();
                }
            return responseMsg;
            }

        internal async Task<HttpPatchResponseMessage<T>> MakePatchCall<T> (string relativeUrl, object patchedObject, Dictionary<string, string> customHeaders = null)
            {
            // Add any additional headers if applicable
            AddCustomHeaders(_client, customHeaders);

            // Construct full url and then make the PATCH call
            using var response = await _client.PatchAsync($"{API_BASE_URL}{relativeUrl}",
                new StringContent(JsonSerializer.Serialize(patchedObject, JsonSerializerOptions), Encoding.UTF8, "application/json-patch+json"));
            if ( response.StatusCode == HttpStatusCode.TooManyRequests )
                {
                // You should implement retry logic for TooManyRequests (429) errors and possibly others like GatewayTimeout or ServiceUnavailable
                }

            // Copy/Deserialize the response into custom HttpPatchResponseMessage.
            HttpPatchResponseMessage<T> responseMsg = new HttpPatchResponseMessage<T>();
            responseMsg.Status = response.StatusCode;
            responseMsg.Content = await response.Content.ReadAsStringAsync();
            var responsePayload = JObject.Parse(responseMsg.Content);
            if ( response.StatusCode == HttpStatusCode.OK )
                {
                // Successful response. Deserialize the object returned. This is the full representation
                // of the instance that was just updated, including the updated values.
                var containerName = typeof(T).Name.ToLower();
                responseMsg.UpdatedInstance = responsePayload[containerName].ToObject<T>();
                }
            else
                {
                // There was an error. Deserialize the error details and return.
                responseMsg.ErrorDetails = responsePayload["error"]?.ToObject<ErrorDetails>();
                }
            return responseMsg;
            }

        internal async Task<HttpResponseMessage<T>> MakeDeleteCall<T> (string relativeUrl, Dictionary<string, string> customHeaders = null)
            {
            // Add any additional headers if applicable
            AddCustomHeaders(_client, customHeaders);

            // Construct full url and then make the POST call
            using var response = await _client.DeleteAsync($"{API_BASE_URL}{relativeUrl}");
            if ( response.StatusCode == HttpStatusCode.TooManyRequests )
                {
                // You should implement retry logic for TooManyRequests (429) errors and possibly others like GatewayTimeout or ServiceUnavailable
                }

            // Copy/Deserialize the response into custom HttpResponseMessage.
            HttpResponseMessage<T> responseMsg = new HttpResponseMessage<T>();
            responseMsg.Status = response.StatusCode;
            if ( response.StatusCode != HttpStatusCode.NoContent )
                {
                // There was an error. Deserialize the error details and return.
                responseMsg.Content = await response.Content.ReadAsStringAsync();
                var responsePayload = JObject.Parse(responseMsg.Content);
                responseMsg.ErrorDetails = responsePayload["error"]?.ToObject<ErrorDetails>();
                }
            return responseMsg;
            }


        internal void DownloadFile(string fileUrl, string localFilePath)
            {
            Console.WriteLine("");
            Console.WriteLine($"Downloading {fileUrl}");
            /*
            //Console.WriteLine($"Authorization {_token.Substring(0, 50)}...");
            Console.WriteLine("");

            //HttpClient _client
            _client.DefaultRequestHeaders.Clear();
            //_client.DefaultRequestHeaders.Add("Authorization", _token);

            using var response = await _client.GetAsync(fileUrl);

            Console.WriteLine(response.ToString());


            return false;
            */
            using (var webClient = new WebClient())
                {
                webClient.DownloadFile(fileUrl, localFilePath);
                }
           
            }

        #region Private Methods

        private void AddCustomHeaders (HttpClient client, Dictionary<string, string> customHeaders = null)
            {
            if ( customHeaders != null )
                {
                foreach ( var ch in customHeaders )
                    {
                    client.DefaultRequestHeaders.Add(ch.Key, ch.Value);
                    }
                }
            }
        private static JsonSerializerOptions JsonSerializerOptions
            {
            get
                {
                var options = new JsonSerializerOptions
                    {
                    IgnoreNullValues = true,
                    WriteIndented = true,
                    AllowTrailingCommas = false,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    Converters = {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                        }
                    };
                return options;
                }
            }
        #endregion
        }

    #region Supporting Classes
    internal class HttpResponseMessage<T>
        {
        public HttpStatusCode Status
            {
            get; set;
            }
        public string Content
            {
            get; set;
            }
        public ErrorDetails ErrorDetails
            {
            get; set;
            }
        }

    internal class HttpPostResponseMessage<T> : HttpResponseMessage<T>
        {
        public T NewInstance
            {
            get; set;
            }
        }
    internal class HttpPatchResponseMessage<T> : HttpResponseMessage<T>
        {
        public T UpdatedInstance
            {
            get; set;
            }
        }
    internal class HttpGetResponseMessage<T> : HttpResponseMessage<T>
        {
        public List<T> Instances
            {
            get; set;
            }
        }
    internal class HttpGetSingleResponseMessage<T> : HttpResponseMessage<T>
        {
        public T Instance
            {
            get; set;
            }
        }
    #endregion
    }
