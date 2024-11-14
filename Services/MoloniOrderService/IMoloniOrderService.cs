using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniOrderService
{
    public interface IMoloniOrderService
    {
        /// <summary>
        /// Obtém itens e cria uma ordem de compra.
        /// </summary>
        /// <param name="receivedClient">Cliente que fez a ordem.</param>
        /// <param name="receivedProducts">Lista de produtos recebidos para a ordem.</param>
        /// <param name="notes">Notas adicionais para a ordem.</param>
        /// <param name="discount">Desconto aplicado na ordem.</param>
        /// <returns>ID da ordem de compra criada ou -1 se a criação falhar.</returns>
        Task<int> GetItemsAndMakeOrder(Customer customer, List<ReceivedProducts> receivedProducts, int currencyId, int orderId, float discount = 0);

        /// <summary>
        /// Obtém uma ordem de compra específica com base nos filtros fornecidos.
        /// </summary>
        /// <param name="document_id">ID do documento da ordem.</param>
        /// <param name="customerId">ID do cliente para filtrar a ordem.</param>
        /// <param name="date">Data para filtrar a ordem.</param>
        /// <param name="number">Número da ordem para filtrar.</param>
        /// <param name="myReference">Referência personalizada da ordem.</param>
        /// <returns>A ordem de compra correspondente ou null se não for encontrada.</returns>
        Task<ProductOrder?> GetPurchaseOrder(int? document_id, int? customerId, DateTime? date, int? number, string? myReference);
    }
}