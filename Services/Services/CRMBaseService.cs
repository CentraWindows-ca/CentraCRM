using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace CentraCRM.D365Service.Services
{
    public abstract class CRMBaseService
    {
        protected IOrganizationService _orgService;
        protected OrganizationServiceProxy _orgProxy;

        protected CRMBaseService()
        {
            ConnectToCRM();
        }

        private void ConnectToCRM()
        {
            // CRM connection string for IFD
            // as Crm2013admin,
            string connectionString = "AuthType=IFD;Url=https://centra.centrawindows.com/Centra;" +
                                      "Domain=centrawindows;Username=centrawindows\\Crm2013admin;Password=Titan5010!;Organization=Centra;RequireNewInstance=true;";
            // as Steve,
            connectionString = "AuthType=IFD;Url=https://centra.centrawindows.com/Centra;" +
                                      "Domain=centrawindows;Username=centrawindows\\swilkins;Password=Yanked1726354;Organization=Centra;RequireNewInstance=true;";
            //string connectionString = "AuthType=IFD;Url=https://centra.centrawindows.com/Centra;" +
            //                          "Username=centrawindows\\Crm2013admin;Password=Titan5010!;";

            // Connect to CRM
            CrmServiceClient conn = new CrmServiceClient(connectionString);
            NetworkCredential cred = new NetworkCredential()
            {
                UserName = "Crm2013admin",
                Password = "Titan5010!",
                Domain = "centrawindows"
            };
            //CrmServiceClient conn = new CrmServiceClient(cred, authType: Microsoft.Xrm.Tooling.Connector.AuthenticationType.IFD, "https://centra.centrawindows.com", "443", "Centra");
            //CrmServiceClient conn = new CrmServiceClient(cred, "https://centra.centrawindows.com", "443", "Centra");
            //CrmServiceClient conn = new CrmServiceClient(cred, AuthenticationType.IFD, "");
            //CrmServiceClient conn = new CrmServiceClient("Crm2013admin", CrmServiceClient.MakeSecureString("Titan5010!"), "centrawindows", "", "https://centra.centrawindows.com", "", "", true, true);

            // Check if connection was successful
            if (conn.IsReady)
            {
                _orgService = conn.OrganizationServiceProxy;
                _orgProxy = conn.OrganizationServiceProxy;
            }
            else
            {
                throw new Exception("Failed to connect to CRM: " + conn.LastCrmError);
            }

            // tests:
            _orgProxy.EnableProxyTypes();
            var userid = ((WhoAmIResponse)_orgProxy.Execute(
                        new WhoAmIRequest())).UserId;
        }   // end of method ConnectToCRM()

        protected List<Entity> RetrieveMultipleWithPagination(QueryExpression query)
        {
            List<Entity> audits = new List<Entity>();

            // Initialize the page number and paging cookie
            int pageNumber = 1;
            string pagingCookie = null;

            bool moreRecords = true;

            while (moreRecords)
            {
                // Execute the query
                EntityCollection auditRecords = _orgService.RetrieveMultiple(query);

                // Process retrieved audit records
                foreach (Entity audit in auditRecords.Entities)
                {
                    //Console.WriteLine($"Audit ID: {audit.Id}");

                    // Add to audit records list for RetrieveAuditDetailsRequest individually,
                    audits.Add(audit);
                }

                // Check if there are more records to retrieve
                if (auditRecords.MoreRecords)
                {
                    // Increment the page number
                    query.PageInfo.PageNumber++;
                    // Get the paging cookie to fetch the next set of records
                    query.PageInfo.PagingCookie = auditRecords.PagingCookie;
                }
                else
                {
                    // No more records to retrieve
                    moreRecords = false;
                }
            }

            return audits;
        }   // end of method RetrieveMultipleWithPagination()


    }   // end of class
}
