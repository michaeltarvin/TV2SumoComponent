using Newtonsoft.Json;
using PayMedia.ApplicationServices.AgreementManagement.ServiceContracts.DataContracts;
using PayMedia.ApplicationServices.Customers.ServiceContracts.DataContracts;
using PayMedia.ApplicationServices.History.ServiceContracts.DataContracts;
using PayMedia.ApplicationServices.HostManagement.ServiceContracts.DataContracts;
using PayMedia.ApplicationServices.IntegrationFramework.ServiceContracts.DataContracts;
using PayMedia.ApplicationServices.ServiceMediator;
using PayMedia.ApplicationServices.SharedContracts;
using PayMedia.ApplicationServices.TV2.IFComponents.DataContracts;
using PayMedia.ApplicationServices.TV2.IFComponents.Token;
using PayMedia.Integration.CommunicationLog.ServiceContracts;
using PayMedia.Integration.CommunicationLog.ServiceContracts.DataContracts;
using PayMedia.Integration.FrameworkService.Common;
using PayMedia.Integration.FrameworkService.Interfaces;
using PayMedia.Integration.FrameworkService.Interfaces.Common;
using PayMedia.Security.Authentication.AuthenticationBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace PayMedia.ApplicationServices.TV2.IFComponents
{
    public class TV2SumoComponent : IFComponent
    {
        public IMessageAction Process(IMsgContext msgContext)
        {
            int customerId = GetCustomerId(msgContext);
            long historyId = GetHistoryId(msgContext);

            //SharedContracts.History history = ServiceGateway.History.HistoryService.GetHistoryRecord(historyId);

            MessageLog message = new MessageLog
            {
                CustomerId = customerId,
                HistoryId = historyId,
                CommandName = msgContext.GetEventValue(IFEventPropertyNames.PROV_EXT_CMD_ID, GetType().Name),
                Token = TV2SumoTokenManager.GetToken(),
                PipeLineKey = msgContext.GetEventValue(IFEventPropertyNames.PIPELINE_KEY, "TV2_SUMO"),
                Url = string.Format(GetHost().Url, customerId),
                Customer = ServiceGateway.Customers.CustomersBizService.GetCustomer(customerId)
        };

            if (string.IsNullOrEmpty(message.Token))
            {
                return MessageAction.DiscardMessageSkipFurtherProcessing("Failed to Retrieve Token.");
            }

            message.Request = GetRequestString(message.Customer);

            SendMessages(message);

            return MessageAction.ContinueProcessing;
        }

        private string GetRequestString(Customer customer)
        {
            CreateCustomerRequest request = new CreateCustomerRequest
            {
                Address = new SumoAddress
                {
                    City = customer.DefaultAddress.BigCity,
                    Zip = customer.DefaultAddress.PostalCode
                },
                DateOfBirth = customer.BirthDate.GetValueOrDefault().ToString("yyyy-MM-dd"),
                Email = customer.DefaultAddress.Email,
                MobileNumber = customer.DefaultAddress.Fax2,
                Firstname = customer.DefaultAddress.FirstName,
                Lastname = customer.DefaultAddress.Surname,
                Username = customer.DefaultAddress.Email,
                Password = "Password1234"
            };

            return request.ToString();
        }

        private void SendMessages(MessageLog message)
        {

            try
            {
                if (string.IsNullOrEmpty(message.Token))
                {
                    throw new Exception("Failed to get token from TV2SumoTokenManager.");
                }

                byte[] data = Encoding.UTF8.GetBytes(message.Request);

                var request = (HttpWebRequest)WebRequest.Create(message.Url);
                request.Proxy = null;
                request.Method = "POST";
                request.Timeout = 60000;
                request.ContentLength = data.Length;
                request.ContentType = "application/json";
                request.Headers.Add("Authorization", $"Bearer {message.Token}");

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);

                    try
                    {
                        using (var response = (HttpWebResponse)request.GetResponse())
                        {
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                message.Response = reader.ReadToEnd();

                                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                                {
                                    //TODO: what do with response?
                                }
                                else if (response.StatusCode == HttpStatusCode.BadRequest && message.Response.Contains("400 OrderItemAction = add is not allowed when service already exist in Service Inventory"))
                                {
                                    //TODO: what do with response?
                                    // UseCase for adding service that already exists
                                    message.Exception = null;
                                }
                                else
                                {
                                    string errorMsg = $"Received HttpStatusCode: {response.StatusDescription}:{response.StatusCode}";
                                    message.Exception = new Exception(errorMsg);
                                    message.Url = errorMsg;
                                }

                                WriteLegacyLogEntry(message);
                            }
                        }
                    }
                    catch (WebException e)
                    {
                        message.Exception = e;

                        if (e.Response != null)
                        {
                            var resp = (HttpWebResponse)e.Response;

                            using (var readStream = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                            {
                                message.Response = readStream.ReadToEnd();
                                WriteLegacyLogEntry(message);
                            }
                            resp.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        message.Exception = e;
                        WriteLegacyLogEntry(message, "Exception");
                    }
                }

            }
            catch (Exception ex)
            {
                message.Exception = ex;
                WriteLegacyLogEntry(message);
            }
        }

        /// <summary>
        /// Reauth on software writes one event for each device linked to the product, this will filter out all but the first the record history.
        /// </summary>
        /// <param name="history"></param>
        /// <returns></returns>
        private bool IsDuplicateReauthorize(SharedContracts.History history)
        {
            var firstRelatedHistory = ServiceGateway.History.HistoryService.FindHistoryRecords(new HistoryFindCriteria
            {
                FilterCriteria = Op.Eq("CustomerId", history.CustomerId.GetValueOrDefault()) && Op.Eq("TransactionId", history.TransactionId.GetValueOrDefault()),
                Events = new List<int> { 190 },
                SortCriteria = new SortCriteriaCollection { new SortCriteria("Id", SortDirections.Ascending) }
            }, 0).Items.FirstOrDefault();

            return history.Id.GetValueOrDefault() != firstRelatedHistory.Id.GetValueOrDefault();
        }
        private long GetHistoryId(IMsgContext msgContext)
        {
            string h = msgContext.GetMsgValue(IFEventPropertyNames.HISTORY_ID, "0");

            if (h == "0")
                h = msgContext.GetEventValue(IFEventPropertyNames.HISTORY_ID, "0");

            return long.Parse(h);
        }

        private int GetCustomerId(IMsgContext msgContext)
        {
            int customerId = msgContext.GetMsgValue(IFEventPropertyNames.CUSTOMER_ID, 0);

            if (customerId == 0)
                customerId = msgContext.GetEventValue(IFEventPropertyNames.CUSTOMER_ID, 0);

            return customerId;
        }

        private List<Entitlement> GetAgreementDetailEntitlements(SharedContracts.History history, int agreementDetailId, int hostId = 1)
        {
            List<Entitlement> entitlements = new List<Entitlement>();

            DeviceEntitlementCollection deviceEntitlementCollection = ServiceGateway.AgreementManagement.BizService.GetDeviceEntitlements(agreementDetailId);

            foreach (DeviceEntitlement deviceEntitlement in deviceEntitlementCollection)
            {
                entitlements.AddRange(deviceEntitlement.Entitlements.Items.FindAll(x => x.CASystemId.GetValueOrDefault() == hostId));
            }

            return entitlements;
        }

        private List<Entitlement> GetALaCarteChannelDeviceEntitlements(SharedContracts.History history, int hostId = 1)
        {
            List<Entitlement> entitlements = new List<Entitlement>();

            var linked = ServiceGateway.History.HistoryService.FindHistoryRecords(new HistoryFindCriteria { CustomerId = history.CustomerId, Events = new List<int> { 5247, 5248 }, FilterCriteria = Op.Eq("TransactionId", history.TransactionId) }, 0);

            foreach (var h in linked)
            {
                EntitlementCollection entitlementCollection = ServiceGateway.AgreementManagement.BizService.GetALaCarteChannelEntitlements((int)h.EntityId.GetValueOrDefault());

                entitlementCollection.Items.RemoveAll(x => x.CASystemId != hostId);

                if (entitlementCollection.Items.Count > 0)
                {
                    entitlements.AddRange(entitlementCollection.Items);
                }
            }

            return entitlements;
        }

        private List<Entitlement> GetActiveEntitlements(SharedContracts.History history, int customerId, int hostId)
        {
            List<Entitlement> entitlement = new List<Entitlement>();

            foreach (AgreementDetail agreementDetail in GetActiveProducts(customerId))
            {
                DeviceEntitlementCollection entitlements = ServiceGateway.AgreementManagement.BizService.GetAllDeviceEntitlements(agreementDetail.Id.Value, true);

                foreach (DeviceEntitlement e in entitlements.Items)
                {
                    if (e.Entitlements.Items.Where(x => x.CASystemId.GetValueOrDefault() == hostId).Any() == false) continue;

                    var ents = e.Entitlements.Items.Where(x => x.CASystemId.GetValueOrDefault() == hostId);

                    entitlement.AddRange(ents);
                }
            }

            // sets the message body action to add or delete

            var uniqueEnts = new List<Entitlement>();

            foreach (Entitlement e in entitlement)
            {
                if (uniqueEnts.Any(x => x.Entitlements1 == e.Entitlements1 && x.Entitlements2 == e.Entitlements2 && x.Entitlements3 == e.Entitlements3 && x.Extended == e.Extended)) continue;
                uniqueEnts.Add(e);
            }

            return uniqueEnts;
        }

        private AgreementDetailCollection GetActiveProducts(int customerId)
        {
            return ServiceGateway.AgreementManagement.BizService.GetAgreementDetailsByBaseQueryRequest(new BaseQueryRequest { FilterCriteria = Op.Eq("CustomerId", customerId) && Op.Eq("Status", 1) });
        }

        private Host GetHost()
        {
            return ServiceGateway.HostManagement.CachedConfig.GetHosts(new BaseQueryRequest { FilterCriteria = Op.Eq("Name", "TV2_API") }).FirstOrDefault();
        }

        #region Logging

        private CommunicationLogEntry GetCommLogEnty(LogEntry log)
        {
            return new CommunicationLogEntry
            {
                CustomerId = int.Parse(log.ICCCustomerId),
                HistoryId = log.ICCHistoryId,
                Host = log.MessageText,
                MessageTrackingId = log.MessageSource,
                Server = Environment.MachineName,
                Message = log.AdditionalInformation,
                MessageQualifier = log.LogType == "I" ? CommunicationLogEntryMessageQualifier.Receive : CommunicationLogEntryMessageQualifier.Error
            };
        }

        public void WriteLegacyLogEntry(MessageLog message, string msg, string a = null)
        {
            WriteLogEntry(
                new LogEntry
                {
                    ICCCustomerId = message.CustomerId.ToString(),
                    ICCHistoryId = message.HistoryId.GetValueOrDefault(),
                    LogEntryUTC = DateTime.Now,
                    ICCMessageUTC = DateTime.Now,
                    LogType = message.Exception == null ? "I" : "E",
                    ICCDSN = BusinessIdentity.CurrentIdentity.Dsn,
                    MessageSource = message.CommandName,
                    IFComponent = GetType().FullName,
                    IFPipeline = message.PipeLineKey,
                    MessageText = msg,
                    AdditionalInformation = a,
                    Server = Environment.MachineName
                });
        }

        public void WriteLegacyLogEntry(MessageLog message)
        {
            WriteLogEntry(
                new LogEntry
                {
                    ICCCustomerId = message.CustomerId.ToString(),
                    ICCHistoryId = message.HistoryId.GetValueOrDefault(),
                    LogEntryUTC = DateTime.Now,
                    ICCMessageUTC = DateTime.Now,
                    LogType = message.Exception == null ? "I" : "E",
                    ICCDSN = BusinessIdentity.CurrentIdentity.Dsn,
                    MessageSource = message.CommandName,
                    IFComponent = GetType().FullName,
                    IFPipeline = message.PipeLineKey,
                    MessageText = message.Url,
                    AdditionalInformation = message.ToString(),
                    Server = Environment.MachineName
                });
        }

        private void WriteLogEntry(LogEntry log, bool skipCommLog = false)
        {
            try
            {
                if (skipCommLog == false)
                {
                    ServiceGateway.CommunicationLog.CommunicationLogService.CreateCommunicationLogEntry(GetCommLogEnty(log));
                }
                ServiceGateway.IntegrationFramework.IntegrationFrameworkService.CreateLogEntry(log);
            }
            catch (Exception)
            {
            }
        }

        public class MessageLog
        {
            public int CustomerId;
            public long? HistoryId;
            public string Url;
            public string Request;
            public string Response;
            public string CommandName;
            public string Token;
            public string PipeLineKey;
            public List<Entitlement> Entitlements;
            public Customer Customer;
            public Exception Exception;

            public override string ToString()
            {
                if (Exception == null)
                {
                    return $"{Request}\r\n\r\nResponse:\r\n{FormatJson(Response)}";
                }
                else
                    return $"Error: {Exception.Message}\r\n\r\nRequest:\r\n{Request}\r\n\r\nResponse:\r\n{FormatJson(Response)}\r\n\r\nStack Trace:\r\n{Exception.StackTrace}";
            }

            //internal void Build(Entitlement entitlement)
            //{
            //    Clear();

            //    //TODO delete
            //    Response = "Some response here";

            //    if (entitlement.Extended == "add")
            //    {
            //        ServiceOrderRequest request = new ServiceOrderRequest(Customer, entitlement.Entitlements1, entitlement.Entitlements2, entitlement.Entitlements3);
            //        Request = request.ToString();
            //    }
            //    else
            //    {
            //        ServiceCancelRequest request = new ServiceCancelRequest(Customer, entitlement.Entitlements1, entitlement.Entitlements2, entitlement.Entitlements3);
            //        Request = request.ToString();
            //    }
            //}

            internal void Clear()
            {
                Request = null;
                Response = null;
                Exception = null;
            }

            private string FormatJson(string json)
            {
                try
                {
                    if (string.IsNullOrEmpty(json))
                    {
                        return "IS NULL";
                    }
                    dynamic parsedJson = JsonConvert.DeserializeObject(json);
                    return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                }
                catch (Exception)
                {
                    return json;
                }

            }
        }

        #endregion
    }
}

