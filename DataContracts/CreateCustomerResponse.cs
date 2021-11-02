using Newtonsoft.Json;
using System;

namespace PayMedia.ApplicationServices.TV2.IFComponents.DataContracts
{
    public class CreateCustomerResponse
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("username")]
        public string Username;

        [JsonProperty("firstname")]
        public string Firstname;

        [JsonProperty("lastname")]
        public string Lastname;

        [JsonProperty("address")]
        public SumoAddressResponse Address;

        [JsonProperty("gender")]
        public string Gender;

        [JsonProperty("email")]
        public string Email;

        [JsonProperty("emailConfirmed")]
        public bool EmailConfirmed;

        [JsonProperty("mobileNumber")]
        public string MobileNumber;

        [JsonProperty("mobileConfirmed")]
        public bool MobileConfirmed;

        [JsonProperty("dateOfBirth")]
        public string DateOfBirth;

        [JsonProperty("registered")]
        public DateTime Registered;

        [JsonProperty("conditionsAccepted")]
        public bool ConditionsAccepted;

        [JsonProperty("communicationConsent")]
        public CommunicationConsent CommunicationConsent;

        [JsonProperty("targetingId")]
        public string TargetingId;
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class SumoAddressResponse
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("streetAddress")]
        public string StreetAddress;

        [JsonProperty("zip")]
        public string Zip;

        [JsonProperty("city")]
        public string City;
    }







    /* Response
    {
      "id": "10474257",
      "username": "91129449",
      "firstname": "Michael",
      "lastname": "Tarvin",
      "address": {
        "type": "PostalAddress",
        "streetAddress": "",
        "zip": "0265",
        "city": "OSLO"
      },
      "gender": "MALE",
      "email": "mike.tarvin@telia.no",
      "emailConfirmed": false,
      "mobileNumber": "93646702",
      "mobileConfirmed": false,
      "dateOfBirth": "2021-10-21",
      "registered": "2021-10-21T22:25:50Z",
      "conditionsAccepted": true,
      "communicationConsent": {
        "email": true,
        "sms": true,
        "facebook": true,
        "instagram": true,
        "google": true
      },
      "targetingId": "746c43d5cd06ffa56f9917df32ed7565fe01135d76783d52e61bf812b6989da6"
    }
*/
}
