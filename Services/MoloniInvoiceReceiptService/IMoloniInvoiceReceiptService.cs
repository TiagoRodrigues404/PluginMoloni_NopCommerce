using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniInvoiceReceiptService
{
    public interface IMoloniInvoiceReceiptService
    {
        /// <summary>
        /// Obtém os itens e cria um recibo de fatura.
        /// </summary>
        /// <param name="receivedClient">Cliente que fez a fatura.</param>
        /// <param name="receivedProducts">Lista de produtos recebidos para a fatura.</param>
        /// <param name="payments">Lista de pagamentos associados à fatura.</param>
        /// <param name="notes">Notas adicionais para a fatura.</param>
        /// <param name="financialDiscount">Desconto financeiro aplicado na fatura.</param>
        /// <param name="specialDiscount">Desconto especial aplicado na fatura.</param>
        /// <returns>ID do recibo de fatura criado ou -1 se a criação falhar.</returns>
        Task<int> GetItemsAndCreateInvoiceReceipt(Customer receivedClient, List<ReceivedProducts> receivedProducts, List<Payment> payments, int orderId, AssociatedDocument associatedDocument, float financialDiscount, float specialDiscount);

        /// <summary>
        /// Obtém um recibo de fatura específico com base nos filtros fornecidos.
        /// </summary>
        /// <param name="document_id">ID do documento do recibo.</param>
        /// <param name="customerId">ID do cliente para filtrar o recibo.</param>
        /// <param name="date">Data para filtrar o recibo.</param>
        /// <param name="number">Número do recibo para filtrar.</param>
        /// <param name="myReference">Referência personalizada do recibo.</param>
        /// <returns>O recibo de fatura correspondente ou null se não for encontrado.</returns>
        Task<ProductOrder?> GetInvoiceReceipt(int? document_id, int? customerId, DateTime? date, int? number, string? myReference);
    }
}
