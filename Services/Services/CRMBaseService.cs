using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;
using System.Security;
using System.Net;

namespace D365Service.Services
{
    public abstract class CRMBaseService
    {
        protected IOrganizationService orgService;
        protected OrganizationServiceProxy orgProxy;

        protected CRMBaseService()
        {
            ConnectToCRM();
        }

        private void ConnectToCRM()
        {
            // CRM connection string for IFD
            //string connectionString = "AuthType=IFD;Url=https://centra.centrawindows.com/Centra;" +
            //                          "Domain=centrawindows;Username=Crm2013admin;Password=Titan5010!;";
            string connectionString = "AuthType=IFD;Url=https://centra.centrawindows.com/Centra;" +
                                      "Username=centrawindows\\Crm2013admin;Password=Titan5010!;";

            // Connect to CRM
            //CrmServiceClient conn = new CrmServiceClient(connectionString);
            NetworkCredential cred = new NetworkCredential()
            {
                UserName = "Crm2013admin",
                Password = "Titan5010!",
                Domain = "centrawindows"
            };
            //CrmServiceClient conn = new CrmServiceClient(cred, authType: Microsoft.Xrm.Tooling.Connector.AuthenticationType.IFD, "https://centra.centrawindows.com", "443", "Centra");
            CrmServiceClient conn = new CrmServiceClient(cred, "https://centra.centrawindows.com", "443", "Centra");
            //CrmServiceClient conn = new CrmServiceClient(cred, AuthenticationType.IFD, "");

            // Check if connection was successful
            if (conn.IsReady)
            {
                orgService = conn.OrganizationServiceProxy;
                orgProxy = conn.OrganizationServiceProxy;
            }
            else
            {
                throw new Exception("Failed to connect to CRM: " + conn.LastCrmError);
            }

            // tests:
            orgProxy.EnableProxyTypes();
            var userid = ((WhoAmIResponse)orgProxy.Execute(
                        new WhoAmIRequest())).UserId;
        }
    }
}
