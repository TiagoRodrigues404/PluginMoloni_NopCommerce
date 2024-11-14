namespace Nop.Plugin.Misc.Moloni.Models
{
    public class Payment
    {
        public int payment_method_id {  get; set; }
        public DateTime date { get; set; }
        public float value { get; set; }
        public string? notes { get; set; }
    }
}
