namespace Nop.Plugin.Misc.Moloni.Models
{
    public class MaturityDate
    {
        public int maturity_date_id {  get; set; }
        public string name { get; set; }
        public int days {  get; set; }
        public float associated_discount { get; set; }
    }
}
