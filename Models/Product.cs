using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nop.Plugin.Misc.Moloni.Models
{
    public class Product
    {
        public int category_id { get; set; }
        public ProductTypes type { get; set; }
        public string name { get; set; }
        public string reference { get; set; }
        public float price { get; set; }
        public int unit_id { get; set; }
        public int has_stock { get; set; }
        public int stock { get; set; }
        public int minimum_stock { get; set; }
        public string? at_product_category { get; set; } //M = Mercadorias
                                                        //P = Matérias-primas, subsidiárias e de consumo
                                                        //A = Produtos acabados e intermédios
                                                        //S = Subprodutos, desperdícios e refugos
                                                        //T = Produtos e trabalhos em curso
        public string? exemption_reason { get; set; }
        public string? summary { get; set; }
        public string? ean { get; set; }
        public int? product_id { get; set; }
        public float? qty { get; set; }
        public string? notes { get; set; }
        public string? image { get; set; }
        public int? visibility_id { get; set; }
        public int? warehouse_id { get; set; }
        public DateTime? created {  get; set; }
        public ProductCategory? category { get; set; }
        public MeasurementUnit? measurement_unit { get; set; }
        public List<Tax>? taxes { get; set; }
        public List<Supplier>? suppliers { get; set; }
        public List<Properties>? properties { get; set; }
        public List<Warehouse>? warehouses { get; set; }
    }

    public class Properties
    {
        public int property_id { get; set; }
        public string value { get; set; }
        public string title {  get; set; }
    }

    public class Warehouse
    {
        public int warehouse_id { get; set; }
        public float stock { get; set; }
        public string? title { get; set; }
        public int? is_default { get; set; }
        public string? code { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
        public string? zip_code { get; set; }
        public int? country_id { get; set; }
        public string? phone {  get; set; }
        public string? fax { get; set; }
        public string? contact_name { get; set; }
        public string? contact_email { get; set; }
        public Country? country { get; set; }

    }

    public class ProductToSend
    {
        public int category_id { get; set; }
        public ProductTypes type { get; set; }
        public string name { get; set; }
        public string reference { get; set; }
        public float price { get; set; }
        public int unit_id { get; set; }
        public int has_stock { get; set; }
        public int stock { get; set; }
        public int minimum_stock { get; set; }
        public string? at_product_category { get; set; }
        public string? exemption_reason { get; set; }
        public string? summary { get; set; }
        public int? product_id { get; set; }
        public string? notes { get; set; }
        public DateTime? created { get; set; }
        public List<TaxToSend>? taxes { get; set; }
        public List<PropertiesToSend>? properties { get; set; }
        public List<WarehouseToSend>? warehouses { get; set; }
    }

    public class WarehouseToSend
    {
        public int warehouse_id { get; set; }
        public float stock { get; set; }
    }

    public class PropertiesToSend
    {
        public int property_id { get; set; }
        public string value { get; set; }
    }

    public class TaxToSend
    {
        public int tax_id { get; set; }
        public float value { get; set; }
        public int order { get; set; }
        public int cumulative { get; set; }
    }
}