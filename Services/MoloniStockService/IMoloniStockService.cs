using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniStockService
{
    public interface IMoloniStockService
    {
        /// <summary>
        /// Insere um novo stock de produto.
        /// </summary>
        /// <param name="productId">O ID do produto.</param>
        /// <param name="qty">A quantidade de stock.</param>
        /// <param name="warehouse_id">O ID do armazém.</param>
        /// <param name="notes">Notas sobre o stock.</param>
        /// <returns>ID do movimento de stock ou -1 se a inserção falhar.</returns>
        Task<int> InsertNewProductStock(int productId, int qty, int? warehouse_id, string? notes);
    }
}
