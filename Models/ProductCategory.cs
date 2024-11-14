namespace Nop.Plugin.Misc.Moloni.Models
{
    public class ProductCategory
    {
        public int category_id { get; set; }
        public int parent_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int pos_enabled { get; set; }
        public string image {  get; set; }
        public int num_categories { get; set; }
        public int num_products { get; set; }

    }
}