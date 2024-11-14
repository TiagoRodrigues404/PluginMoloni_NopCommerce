using System.Text.Json.Serialization;

namespace Nop.Plugin.Misc.Moloni.Models
{
    public class Order
    {
        public DateTime date { get; set; }
        public DateTime expiration_date { get; set; }
        public int document_set_id { get; set; }
        public int customer_id { get; set; }
        public string? your_reference { get; set; }
        public float? financial_discount { get; set; }
        public float? special_discount { get; set; }
        public List<Product> products { get; set; }
        public int? exchange_currency_id { get; set; }
        public float? exchange_rate { get; set; }
        public DateTime? delivery_datetime { get; set; }
        public int? DeliveryDepartureCountry { get; set; }
        public string? delivery_destination_address { get; set; }
        public string? delivery_destination_city { get; set; }
        public string? delivery_destination_zip_code { get; set; }
        public int? delivery_destination_country { get; set; }
        public int? status { get; set; }
    }
}
