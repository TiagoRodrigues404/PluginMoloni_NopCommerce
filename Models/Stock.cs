namespace Nop.Plugin.Misc.Moloni.Models
{
    public class Stock
    {
        public int product_stock_id { get; set; }
        public int stock_movement_id { get; set; }
        public int warehouse_id { get; set; }
        public DateTime movement_date { get; set; }
        public int document_id { get; set; }
        public int qty { get; set; }
        public int accumulated { get; set; }
        public string? notes { get; set; }
        public StockProduct product { get; set; }
        public Warehouse warehouse { get; set; }
        public List<Document> document { get; set; }
    }

    public class StockProduct
    {
        public string lastmodifiedby { get; set; }
        public DateTime lastmodified { get; set; }
        public DateTime created { get; set; }
        public int created_by { get; set; }
        public DateTime first_used { get; set; }
        public string artigo_unico { get; set; }
        public string ean {  get; set; }
        public int stock { get; set; }
        public int recem_importado { get; set; }
        public int obs_impressas_exportacoes { get; set; }
        public int product_id { get; set; }
        public int visibility_id { get; set; }
        public int category_id { get; set; }
        public ProductTypes type { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string summary { get; set; }
        public string notes { get; set; }
        public string reference { get; set; }
        public float price { get; set; }
        public int unit_id { get; set; }
        public int has_stock { get; set; }
        public string image {  get; set; }
        public string exemption_reason { get; set; }
        public string at_product_category { get; set; }
    }

    public class StockToSend
    {
        public int product_id { get; set; }
        public DateTime movement_date { get; set; }
        public float qty { get; set; }
        public int warehouse_id { get; set; }
    }
}
