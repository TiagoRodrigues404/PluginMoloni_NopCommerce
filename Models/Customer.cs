using System.Text.Json.Serialization;

namespace Nop.Plugin.Misc.Moloni.Models
{
    public class Customer
    {
        public int? customer_id { get; set; }
        public string? number { get; set; }
        public string name { get; set; }
        public string vat { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public string? zip_code { get; set; }
        public string? email { get; set; }
        public string? phone { get; set; }
        public string? notes { get; set; }
        public int country_id { get; set; }
        public int salesman_id { get; set; } = 0;
        public int maturity_date_id { get; set; }
        public int payment_day { get; set; } = 0;
        public float? discount { get; set; } = 0;
        public int payment_method_id { get; set; }
        public float credit_limit { get; set; } = 0;
        public int delivery_method_id { get; set; } = 0;
    }
}
