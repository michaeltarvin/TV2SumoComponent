using Newtonsoft.Json;

namespace PayMedia.ApplicationServices.TV2.IFComponents.DataContracts
{

    public class CreateCustomerRequest
    {
        [JsonProperty("accountType")]
        public string AccountType { get; set; } = "SVOD";

        [JsonProperty("address")]
        public SumoAddress Address = new SumoAddress();

        [JsonProperty("communicationConsent")]
        public CommunicationConsent CommunicationConsent = new CommunicationConsent();

        [JsonProperty("communicationConsentAll")]
        public bool CommunicationConsentAll { get; set; } = true;

        [JsonProperty("conditionsAccepted")]
        public bool ConditionsAccepted { get; set; } = true;

        [JsonProperty("dateOfBirth")]
        public string DateOfBirth;

        [JsonProperty("email")]
        public string Email;

        [JsonProperty("firstname")]
        public string Firstname;

        [JsonProperty("gender")]
        public string Gender { get; set; } = "UNKNOWN";

        [JsonProperty("lastname")]
        public string Lastname;

        [JsonProperty("mobileNumber")]
        public string MobileNumber;

        [JsonProperty("password")]
        public string Password;

        [JsonProperty("username")]
        public string Username;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class SumoAddress
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "PostalAddress"; 

         [JsonProperty("zip")]
        public string Zip;

        [JsonProperty("city")]
        public string City;
    }

    public class CommunicationConsent
    {
        [JsonProperty("email")]
        public bool Email { get; set; } = true;

        [JsonProperty("facebook")]
        public bool Facebook { get; set; } = true;

        [JsonProperty("google")]
        public bool Google { get; set; } = true;

        [JsonProperty("instagram")]
        public bool Instagram { get; set; } = true;

        [JsonProperty("sms")]
        public bool Sms { get; set; } = true;
    }


    /* Request
        {
	        "accountType": "SVOD",
	        "address": {
		        "type": "PostalAddress",
		        "zip": "0265",
		        "city": "OSLO"
	        },
	        "communicationConsent": {
		        "email": true,
		        "facebook": true,
		        "google": true,
		        "instagram": true,
		        "sms": true
	        },
	        "communicationConsentAll": true,
	        "conditionsAccepted": true,
	        "dateOfBirth": "2021-10-21",
	        "email": "mike.tarvin@telia.no",
	        "firstname": "Michael",
	        "gender": "MALE",
	        "lastname": "Tarvin",
	        "mobileNumber": "93646702",
	        "password": "Passw0rd",
	        "username": "91129449"
        }
     */



}
