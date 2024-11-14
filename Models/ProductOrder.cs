namespace Nop.Plugin.Misc.Moloni.Models
{
    public class ProductOrder
    {
        public int terminal_id { get; set; }
        public DateTime lastmodified { get; set; }
        public string lastmodifiedby { get; set; }
        public int document_id { get; set; }
        public int company_id { get; set; }
        public int document_type_id { get; set; }
        public int customer_id { get; set; }
        public int supplier_id { get; set; }
        public int document_set_id { get; set; }
        public string document_set_name { get; set; }
        public int number { get; set; }
        public DateTime date { get; set; }
        public DateTime expiration_date { get; set; }
        public int? maturity_date_days { get; set; }
        public int maturity_date_id { get; set; }
        public string maturity_date_name { get; set; }
        public string your_reference { get; set; }
        public string our_reference { get; set; }
        public string entity_number { get; set; }
        public string entity_name { get; set; }
        public string entity_vat { get; set; }
        public string entity_address { get; set; }
        public string entity_city { get; set; }
        public string entity_zip_code { get; set; }
        public string entity_country { get; set; }
        public int entity_country_id { get; set; }
        public decimal financial_discount { get; set; }
        public decimal gross_value { get; set; }
        public decimal comercial_discount_value { get; set; }
        public decimal financial_discount_value { get; set; }
        public decimal taxes_value { get; set; }
        public decimal deduction_value { get; set; }
        public decimal net_value { get; set; }
        public decimal reconciled_value { get; set; }
        public int status { get; set; }
        public int exchange_currency_id { get; set; }
        public decimal exchange_total_value { get; set; }
        public decimal exchange_rate { get; set; }
        public DocumentType document_type { get; set; }
        public DocumentSet document_set { get; set; }
        public List<Document> associated_documents { get; set; }
        public DocumentCalcMethod document_calc_method { get; set; }
    }

    public class DocumentCalcMethod
    {
        public long document_id { get; set; }
        public int calc_method_id { get; set; }
    }
}
