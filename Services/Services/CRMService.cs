using System;
using System.Collections.Generic;
using CentraCRM.D365Service.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CentraCRM.D365Service.Services
{
    public class CRMService : CRMBaseService
    {
        public CRMService() : base()
        {
        }

        public List<Entity> GetAuditLogs()
        {
            /*  // can't use this as there are more than 5000 records, needs pagination
            // Use FetchXML to query the Audit entity
            string fetchXml = @"
                <fetch>
                    <entity name='audit'>
                        <attribute name='auditid' />
                        <attribute name='action' />
                        <attribute name='operation' />
                        <attribute name='createdon' />
                        <attribute name='objecttypecode' />
                        <attribute name='objectid' />
                        <attribute name='attributemask' />
                        <attribute name='userid' />
                        <attribute name='callinguserid' />
                        <filter type='and'>
                            <condition attribute='objecttypecode' operator='eq' value='2' />
                            <condition attribute='operation' operator='eq' value='2' />
                            <condition attribute='createdon' operator='on' value='2024-08-24' />
                            <condition attribute='userid' operator='eq' value='{97F8F8D6-766E-E411-9E86-005056BF4E69}' />
                        </filter>
                    </entity>
                </fetch>";

            EntityCollection auditLogs = _orgService.RetrieveMultiple(new FetchExpression(fetchXml));
            */

            // Initialize the page number and paging cookie
            int pageNumber = 1;
            string pagingCookie = null;

            // Create the query for retrieving audit records
            QueryExpression query = new QueryExpression("audit")
            {
                ColumnSet = new ColumnSet("auditid"),  // Adjust the fields you need
                PageInfo = new PagingInfo
                {
                    Count = 5000,    // Max records per page (5000 is the upper limit)
                    PageNumber = pageNumber,
                    PagingCookie = pagingCookie
                }
            };

            // setting up the conditions for the query,
            query.Criteria.FilterOperator = LogicalOperator.And;
            query.Criteria.AddCondition("objecttypecode", ConditionOperator.Equal, 2);  // on contact
            query.Criteria.AddCondition("operation", ConditionOperator.Equal, 2);   // update event
            query.Criteria.AddCondition("createdon", ConditionOperator.On, new DateTime(2024, 8, 24));  // the import happened that date
            query.Criteria.AddCondition("userid", ConditionOperator.Equal, new Guid("97F8F8D6-766E-E411-9E86-005056BF4E69"));  // by Crm2013Admin user

            var auditLogs = RetrieveMultipleWithPagination(query);

            return auditLogs;
        }   // end of method GetAuditLogs()

        public List<Contact> GetContactsRestoredData(List<Entity> auditLogs)
        {
            List<Contact> contacts = new List<Contact>();

            // the attribute to check for old vs new value: ("firstname", "lastname", "telephone2", "mobilephone"), add more if needed

            int counter = 0;    // for debugger purpose to get an idea how far it has gone in the iteration in the foreach loop
            foreach (Entity auditLog in auditLogs)
            {
                Guid auditId = auditLog.Id;

                // Retrieve audit details for the given Audit record
                RetrieveAuditDetailsRequest auditDetailsRequest = new RetrieveAuditDetailsRequest
                {
                    AuditId = auditId
                };

                RetrieveAuditDetailsResponse auditDetailsResponse = (RetrieveAuditDetailsResponse)_orgService.Execute(auditDetailsRequest);

                // Cast to AttributeAuditDetail to get old and new values
                var auditDetail = auditDetailsResponse.AuditDetail as AttributeAuditDetail;
                if (auditDetail == null)
                    continue;

                //
                var isFisrtNameEmptied =  CheckIfFieldIsEmptied(auditDetail, "firstname");
                var isLastNameEmptied =  CheckIfFieldIsEmptied(auditDetail, "lastname");
                var isHomePhoneEmptied =  CheckIfFieldIsEmptied(auditDetail, "telephone2");
                var isMobilePhoneEmptied =  CheckIfFieldIsEmptied(auditDetail, "mobilephone");

                // if any of those fields gets emptied, we conclude the contact data needs to be restored,
                if (isFisrtNameEmptied || isLastNameEmptied || isHomePhoneEmptied || isMobilePhoneEmptied)
                {
                    // we retrieve the data of the audit log detail first,
                    // later when update we want to check if currently each field has data already. If it does then we don't update that field.
                    //Entity contact = new Entity("contact");
                    //contact.Id = auditDetail.AuditRecord.GetAttributeValue<EntityReference>("objectid").Id; // auditLog only contains auditid (on purpose as we want to limit the data traffic from the audits retrieval) so obtain info from auditDetail instead
                    //contact["firstname"] = auditDetail.OldValue["firstname"].ToString();
                    //contact["lastname"] = auditDetail.OldValue["lastname"].ToString();
                    //contact["fullname"] = auditDetail.OldValue["fullname"].ToString();
                    //contact["telephone2"] = auditDetail.OldValue["telephone2"].ToString();
                    //contact["mobilephone"] = auditDetail.OldValue["mobilephone"].ToString();

                    //// these 2 fields may currently contain value:
                    //contact["leadsourcecode"] = auditDetail.OldValue["leadsourcecode"].ToString();
                    //contact["new_campaign"] = auditDetail.OldValue["new_campaign"].ToString();

                    Contact contact = new Contact
                    {
                        Contactid = auditDetail.AuditRecord.GetAttributeValue<EntityReference>("objectid").Id, // auditLog only contains auditid (on purpose as we want to limit the data traffic from the audits retrieval) so obtain info from auditDetail instead
                        FirstName = auditDetail.OldValue.GetAttributeValue<string>("firstname"),
                        LastName = auditDetail.OldValue.GetAttributeValue<string>("lastname"),
                        FullName = auditDetail.OldValue.GetAttributeValue<string>("fullname"),
                        Telephone2 = auditDetail.OldValue.GetAttributeValue<string>("telephone2"),
                        MobilePhone = auditDetail.OldValue.GetAttributeValue<string>("mobilephone"),

                        // these 2 fields may currently contain value:
                        LeadSourceCode = auditDetail.OldValue.GetAttributeValue<OptionSetValue>("leadsourcecode") == null ? null : auditDetail.OldValue.GetAttributeValue<OptionSetValue>("leadsourcecode").Value.ToString(),
                        CampaignId = auditDetail.OldValue.GetAttributeValue<EntityReference>("new_campaign") == null ? null : auditDetail.OldValue.GetAttributeValue<EntityReference>("new_campaign").Id.ToString(),
                    };

                    contacts.Add(contact);

                    // testing purpose only:
                    //return contacts;    // we just want to try one for now!!!!!!!!!!
                }

                counter++;
            }

            return contacts;
        }   // end of method GetContactsRestoredData()

        private static bool CheckIfFieldIsEmptied(AttributeAuditDetail auditDetail, string attributeName)
        {
            // if the attribute is not in the NewValue,
            if (auditDetail != null && !auditDetail.NewValue.Contains(attributeName))
            {
                // compare oldValue vs newValue to double check the attribute value was originally there,
                var oldValue = auditDetail.OldValue.Contains(attributeName) ? auditDetail.OldValue[attributeName].ToString() : null;
                var newValue = auditDetail.NewValue.Contains(attributeName) ? auditDetail.NewValue[attributeName].ToString() : null;

                // Check if old value is not null/empty and new value is null/empty
                // we're only interested in the ones where oldValue has data but newValue doesn't,
                if (!string.IsNullOrEmpty(oldValue) && string.IsNullOrEmpty(newValue))
                {
                    //Console.WriteLine($"Field: {attributeName}");
                    //Console.WriteLine($"Old Value: {oldValue}");
                    //Console.WriteLine($"New Value: {newValue} (empty or null)");
                    //Console.WriteLine();

                    return true;
                }
            }

            return false;
        }   // end of method CheckIfFieldIsEmptied()

        public int UpdateRecords(List<Entity> records)
        {
            //
            // testing code:
            if (records == null || records.Count == 0)
            {
                // dummy Contact created for testing,
                // using Dummy Test, contactid = {179cf021-8170-ef11-8140-005056bf6c3d}

                // we first remove all those fields that lost data (got emptied),
                //ResetDummyContact();

                // then we try to update and verify the results,
                UpdateDummyContact();
                return 1;
            }
            // end of testing
            //

            int counter = 0;    // for debugger purpose to get an idea how far it has gone in the iteration in the foreach loop
            int updatedCount = 0;   // successfully updated counter

            // we only update the fields if they currently have no value
            foreach (var record in records)
            {
                try
                {
                    // obtain the current state of the record, then remove fields that already has values from the updates
                    var rec = _orgService.Retrieve("contact", record.Id, new ColumnSet(true));

                    // check if already has value, meaning someone has manually updated the record since the incident.
                    // in which case we don't want to update again,
                    if (!string.IsNullOrEmpty(rec.GetAttributeValue<string>("firstname")))
                        record.Attributes.Remove("firstname");
                    if (!string.IsNullOrEmpty(rec.GetAttributeValue<string>("lastname")))
                        record.Attributes.Remove("lastname");
                    if (!string.IsNullOrEmpty(rec.GetAttributeValue<string>("fullname")))
                        record.Attributes.Remove("fullname");
                    if (!string.IsNullOrEmpty(rec.GetAttributeValue<string>("telephone2")))
                        record.Attributes.Remove("telephone2");
                    if (!string.IsNullOrEmpty(rec.GetAttributeValue<string>("mobilephone")))
                        record.Attributes.Remove("mobilephone");

                    // for Lead Source and Campaign (lookup) fields, we always want to overwrite, there comment out the following:
                    //if (rec.GetAttributeValue<OptionSetValue>("leadsourcecode") != null)
                    //    record.Attributes.Remove("leadsourcecode");
                    //if (rec.GetAttributeValue<EntityReference>("new_campaign") != null)
                    //    record.Attributes.Remove("new_campaign");

                    //return 1; // we just want to see one for now!!!!!!!!!!

                    // updating the CRM record,
                    _orgService.Update(record);
                    
                    updatedCount++;
                }
                catch (Exception ex)
                {
                    //log it somewhere and continue
                    // updatedCount won't increment if it comes in here
                }

                counter++;
            }

            return updatedCount;
        }   // end of method UpdateRecords()

        //
        // testing helpers:
        private void ResetDummyContact()
        {
            Entity dummy = new Entity("contact");
            dummy.Id = new Guid("179cf021-8170-ef11-8140-005056bf6c3d");
            dummy["firstname"] = null;
            dummy["lastname"] = null;
            dummy["fullname"] = null;
            //dummy["telephone2"] = null;
            //dummy["mobilephone"] = null;
            dummy["leadsourcecode"] = null; // lead source field
            dummy["new_campaign"] = null;   // campaign lookup field
            _orgService.Update(dummy);
        }

        private void UpdateDummyContact()
        {
            var rec = _orgService.Retrieve("contact", new Guid("179cf021-8170-ef11-8140-005056bf6c3d"), new ColumnSet(true));

            Entity dummy = new Entity("contact");
            dummy.Id = new Guid("179cf021-8170-ef11-8140-005056bf6c3d");
            dummy["firstname"] = "Dummy";
            dummy["lastname"] = "Test";
            dummy["fullname"] = "Dummy Test";
            if (string.IsNullOrEmpty(rec.GetAttributeValue<string>("telephone2")))  // testing if has value, then don't overwrite it scenario
                dummy["telephone2"] = "604-593-1234";
            if (string.IsNullOrEmpty(rec.GetAttributeValue<string>("mobilephone")))
                dummy["mobilephone"] = "778-593-1234";
            dummy["leadsourcecode"] = new OptionSetValue(279640003);    // Internet
            dummy["new_campaign"] = new EntityReference("campaign", new Guid("a5a69c28-586c-ee11-8136-005056bf6c3d"));    // APC
            _orgService.Update(dummy);
        }

    }   // end of class
}
