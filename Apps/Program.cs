using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CentraCRM.D365Service.Models;
using CentraCRM.D365Service.Services;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace CentraCRM.Apps
{
    public class GetContactsRecoveryDataProgram
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Do you want to perform data recovery job for CRM contacts? (Y/N)");
            var ans = Console.ReadLine();

            if (ans.ToLower() == "y")
            {
                RecoverContactsData();
            }
            else
            {
                Console.WriteLine("CRM data recovery job aborted.");
            }

            Console.WriteLine("The program has finished running. Please press Enter to exit the program.");
            Console.ReadLine();
        }

        private static void RecoverContactsData()
        {
            CRMService crmService = new CRMService();

            // operations:

            // first get potentially relevant auditlogs,
            var auditLogs = crmService.GetAuditLogs();

            // then from auditLogs find the ones that overwrote the name and phone fields, these are the true ones,
            var contacts = crmService.GetContactsRestoredData(auditLogs);

            // dump contacts to a file in case something breaks during updates, and can just use the dump file and start updating from there,
            DumpToTextFile(contacts);

            //
            // DISABLE ALL OTHER Contact PLUGINS BEFORE THIS!!!!!!!!!
            // restore the values of those contacts records from those logs,
            //var response = crmService.UpdateRecords(contacts);
        }

        private static void DumpToTextFile(List<Contact> contactModels)
        {
            // Convert the contactModels list to JSON
            string json = JsonConvert.SerializeObject(contactModels, Newtonsoft.Json.Formatting.Indented);

            // Write the JSON string to a text file
            string filePath = "contacts.json";
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Contacts have been dumped to {filePath}");
        }

        private static void DumpToTextFile(List<Entity> contacts)
        {
            // convert List<Entity> to List<Contact>,
            List<Contact> contactModels = ConvertToListContactModel(contacts);

            // Convert the contactModels list to JSON
            string json = JsonConvert.SerializeObject(contactModels, Newtonsoft.Json.Formatting.Indented);

            // Write the JSON string to a text file
            string filePath = "contacts.json";
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Contacts have been dumped to {filePath}");
        }

        private static List<Contact> ConvertToListContactModel(List<Entity> contacts)
        {
            return contacts.Select(contact => new Contact
            {
                Contactid = contact.Id,
                FirstName = contact.Contains("firstname") ? contact["firstname"].ToString() : string.Empty,
                LastName = contact.Contains("lastname") ? contact["lastname"].ToString() : string.Empty,
                FullName = contact.Contains("fullname") ? contact["fullname"].ToString() : string.Empty,
                Telephone2 = contact.Contains("telephone2") ? contact["telephone2"].ToString() : string.Empty,
                MobilePhone = contact.Contains("mobilephone") ? contact["mobilephone"].ToString() : string.Empty,
                LeadSourceCode = contact.Contains("leadsourcecode") ? contact["leadsourcecode"].ToString() : string.Empty,
                CampaignId = contact.Contains("new_campaign") ? contact["new_campaign"].ToString() : string.Empty,
            }).ToList();
        }
    }

    public class UpdateContactsProgram
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Do you want to perform a batch update job on CRM contacts? (Y/N)");
            var ans = Console.ReadLine();

            if (ans.ToLower() == "y")
            {
                UpdateCRMContactsData();
            }
            else
            {
                Console.WriteLine("CRM contacts batch update job aborted.");
            }

            Console.WriteLine("The program has finished running. Please press Enter to exit the program.");
            Console.ReadLine();
        }

        private static void UpdateCRMContactsData()
        {
            CRMService crmService = new CRMService();

            // Read JSON string from a text file,
            string filePath = "contacts.json";
            string json = File.ReadAllText(filePath);

            // Deserialize JSON into List<Contact>,
            List<Contact> contactModels = JsonConvert.DeserializeObject<List<Contact>>(json);

            // Convert List<Contact> to List<Entity> so that it can be passed to crmService for UpdateRecords(),
            var contacts = ConvertToListEntity(contactModels);

            // Update CRM contact records,
            var response = crmService.UpdateRecords(contacts);
        }

        private static List<Entity> ConvertToListEntity(List<Contact> contactModels)
        {
            // Convert List<Contact> to List<Entity> (CRM contacts)
            List<Entity> contacts = contactModels.Select(contact =>
            {
                Entity entity = new Entity("contact");
                entity.Id = contact.Contactid;

                if (!string.IsNullOrEmpty(contact.FirstName))
                    entity["firstname"] = contact.FirstName;

                if (!string.IsNullOrEmpty(contact.LastName))
                    entity["lastname"] = contact.LastName;

                if (!string.IsNullOrEmpty(contact.FullName))
                    entity["fullname"] = contact.FullName;

                if (!string.IsNullOrEmpty(contact.Telephone2))
                    entity["telephone2"] = contact.Telephone2;

                if (!string.IsNullOrEmpty(contact.MobilePhone))
                    entity["mobilephone"] = contact.MobilePhone;
                
                if (!string.IsNullOrEmpty(contact.LeadSourceCode))
                    entity["leadsourcecode"] = new OptionSetValue(int.Parse(contact.LeadSourceCode));
                
                if (!string.IsNullOrEmpty(contact.CampaignId))
                    entity["new_campaign"] = new EntityReference("campaign", new Guid(contact.CampaignId));

                return entity;
            }).ToList();

            return contacts;
        }
    }
}
