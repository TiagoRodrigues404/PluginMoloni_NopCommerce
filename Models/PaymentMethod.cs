using System.Text.Json.Serialization;

namespace Nop.Plugin.Misc.Moloni.Models
{
    public class PaymentMethod
    {
        public int payment_method_id {  get; set; }
        public string name { get; set; }
    }
}
