using PayMedia.ApplicationServices.HostManagement.ServiceContracts.DataContracts;
using PayMedia.ApplicationServices.IntegrationFramework.ServiceContracts.DataContracts;
using PayMedia.ApplicationServices.ServiceMediator;
using PayMedia.ApplicationServices.SharedContracts;
using PayMedia.ApplicationServices.TV2.IFComponents.DataContracts;
using PayMedia.Security.Authentication.AuthenticationBase;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace PayMedia.ApplicationServices.TV2.IFComponents.Token
{
    public class TV2SumoTokenManager
    {
        static readonly MemoryCache tokenCache = new MemoryCache("TV2SumoTokenCache");
        private static readonly object cacheLock = new object();

        public static string GetToken()
        {
            Host host = ServiceGateway.HostManagement.CachedConfig.GetHosts(new BaseQueryRequest { FilterCriteria = Op.Eq("Name", "TV2_TOKEN_MANAGER") }).FirstOrDefault();

            host.UserName = "HtRwyKuaiGVbPyMX8COTFvnB0EcLnxPN";
            host.Password = "b950cyx-JpaX5zMSQalPvewkfhE3HU_WdoubnTetjM9zyVBCjqINYAwr9BZ504ue";

            return GetToken(host.Url, host.Password, host.UserName);
        }

        public static string GetToken(string url, string secret, string clientId)
        {
            lock (cacheLock)
            {
                if (tokenCache.Contains(secret))
                {
                    return tokenCache.GetCacheItem(secret).Value.ToString();
                }

                TokenResponse tokenResponse = new TokenResponse();

                TokenRequest tokenRequest = new TokenRequest { ClientId = clientId, ClientSecret = secret };
                string tokenString = tokenRequest.ToString();

                byte[] data = Encoding.UTF8.GetBytes(tokenString);

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = null;
                request.Method = "POST";
                request.Timeout = 60000;
                request.ContentLength = data.Length;
                request.ContentType = "application/json";

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);

                    try
                    {
                        using (var response = (HttpWebResponse)request.GetResponse())
                        {
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                string json = reader.ReadToEnd();

                                if (response.StatusCode == HttpStatusCode.OK)
                                {
                                    tokenResponse = TokenResponse.GetTokenResponseFromJson(json);
                                    // TODO: this expires in is wrong
                                    tokenCache.Set(secret, tokenResponse.AccessToken, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(tokenResponse.ExpiresIn - 60) });

                                    WriteLegacyLogEntry("New token retrieved. See AdditionalInformation for more details.", json);

                                    return tokenResponse.AccessToken;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"Url: {url}");
                        sb.AppendLine($"client_id: {clientId}");
                        sb.AppendLine($"client_secret: {secret}");
                        sb.AppendLine($"Exception: {ex}");
                        WriteLegacyLogEntry("Failed to get token. See AdditionalInformation for more details.", sb.ToString(), true);
                    }
                }
            }

            return null;
        }

        private static async Task<string> PostFormUrlEncoded<TResult>(string url, string secret, string clientId)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
                {
                    TokenRequest tokenRequest = new TokenRequest { ClientId = clientId, ClientSecret = secret };
                    request.Content = new StringContent(tokenRequest.ToString());
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    HttpResponseMessage response = await httpClient.SendAsync(request);

                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        private static void WriteLegacyLogEntry(string message, string additionalInformation, bool isError = false)
        {
            var log = new LogEntry
            {
                ICCCustomerId = "1",
                ICCHistoryId = 1,
                LogEntryUTC = DateTime.Now,
                ICCMessageUTC = DateTime.Now,
                LogType = isError ? "E" : "I",
                ICCDSN = BusinessIdentity.CurrentIdentity.Dsn,
                MessageSource = "TV2SumoTokenManager",
                IFComponent = "PayMedia.ApplicationServices.TV2.TV2SumoTokenManager",
                IFPipeline = "TV2_TOKEN",
                MessageText = message,
                AdditionalInformation = additionalInformation,
                ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString(),
                Server = Environment.MachineName
            };

            ServiceGateway.IntegrationFramework.IntegrationFrameworkService.CreateLogEntry(log);

        }

    }
}
