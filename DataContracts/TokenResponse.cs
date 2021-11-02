using Newtonsoft.Json;

namespace PayMedia.ApplicationServices.TV2.IFComponents.DataContracts
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken;

        [JsonProperty("scope")]
        public string Scope;

        [JsonProperty("expires_in")]
        public int ExpiresIn;

        [JsonProperty("token_type")]
        public string TokenType;

        public static TokenResponse GetTokenResponseFromJson(string json)
        {
            return JsonConvert.DeserializeObject<TokenResponse>(json); ;
        }
    }
}
