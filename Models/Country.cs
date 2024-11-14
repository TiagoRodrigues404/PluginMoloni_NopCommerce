namespace Nop.Plugin.Misc.Moloni.Models
{
    public class Country
    {
        public int country_id {  get; set; }
        public string name { get; set; }
        public string iso_3166_1 { get; set; }
        public string image {  get; set; }
        public int vies_vat_check_available { get; set; }
        public List<FiscalZones>? fiscal_zones { get; set; }
    }
}