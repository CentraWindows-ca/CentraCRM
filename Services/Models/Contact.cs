using System;

namespace CentraCRM.D365Service.Models
{
    public class Contact
    {
        public Guid Contactid { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Telephone2 { get; set; }
        public string MobilePhone { get; set; }
        public string LeadSourceCode { get; set; }
        public string CampaignId { get; set; }
    }
}
