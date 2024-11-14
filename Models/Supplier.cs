namespace Nop.Plugin.Misc.Moloni.Models
{
    public class Supplier
    {
        public int supplier_id { get; set; }
        public float cost_price { get; set; }
        public string? reference {  get; set; }
        public int? product_id { get; set; }
        public float? comercial_discount { get; set; }
        public float? financial_discount { get; set; }
        public float? cost_price_discounted { get; set; }
    }
}
