namespace Nop.Plugin.Misc.Moloni.Models
{
    public class InvoiceReceiptRequest
    {
        public Customer client { get; set; }
        public List<ReceivedProducts> ReceivedProducts { get; set; }
        public List<Payment> Payments { get; set; }
        public string Notes { get; set; } = "";
        public float FinancialDiscount { get; set; } = 0;
        public float SpecialDiscount { get; set; } = 0;
    }

}
