using Newtonsoft.Json;

namespace DeliveryManager.Server.Models
{
    [JsonObject(ItemRequired = Required.AllowNull)]
    public class DeliveryForm
    {
        public string MFSTKEY { get; set; }
        public string? STATUS {  get; set; }
        public string? LASTUPDATE { get; set; }
        public string? MFSTNUMBER { get; set; }
        public string? POWERUNIT { get; set; }
        public int? STOP {  get; set; }
        public string? MFSTDATE { get; set; }
        public string? PRONUMBER { get; set; }
        public string? PRODATE { get; set; }
        public string? SHIPNAME { get; set; }
        public string? CONSNAME { get; set; }
        public string? CONSADD1 { get; set; }
        public string? CONSADD2 { get; set; }
        public string? CONSCITY { get; set; }
        public string? CONSSTATE { get; set; }
        public string? CONSZIP { get; set; }
        public int? TTLPCS { get; set; }
        public int? TTLYDS { get; set; }
        public int? TTLWGT { get; set; }
        public string? DLVDDATE { get; set; }
        public string? DLVDTIME { get; set; }
        public int? DLVDPCS { get; set; }
        public string? DLVDSIGN { get; set; }
        public string? DLVDNOTE { get; set; }
        public IFormFile? DLVDIMGFILELOCN { get; set; }
        public IFormFile? DLVDIMGFILESIGN { get; set; }

        public string? signature_string { get; set; }
        public string? location_string { get; set; }

    }
}
