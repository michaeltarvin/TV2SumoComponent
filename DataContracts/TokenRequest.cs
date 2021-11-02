using Newtonsoft.Json;

namespace PayMedia.ApplicationServices.TV2.IFComponents.DataContracts
{
    /*
        {
	        "client_id": "HtRwyKuaiGVbPyMX8COTFvnB0EcLnxPN",
	        "client_secret": "b950cyx-JpaX5zMSQalPvewkfhE3HU_WdoubnTetjM9zyVBCjqINYAwr9BZ504ue",
	        "audience": "https://api.sumo.tv2.no",
	        "grant_type": "client_credentials"
        } 
     */
    public class TokenRequest
    {
        [JsonProperty("client_id")]
        public string ClientId;

        [JsonProperty("client_secret")]
        public string ClientSecret;

        [JsonProperty("audience")]
        public string Audience { get; set; } = "https://api.sumo.tv2.no";

        [JsonProperty("grant_type")]
        public string GrantType { get; set; } = "client_credentials";

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

    }
}
