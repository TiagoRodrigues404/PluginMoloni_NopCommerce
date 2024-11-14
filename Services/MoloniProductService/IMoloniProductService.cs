using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniProductService
{
    public interface IMoloniProductService
    {
        /// <summary>
        /// Obtém todos os produtos de uma categoria específica.
        /// </summary>
        /// <param name="categoryId">O ID da categoria para obter os produtos.</param>
        /// <returns>Lista de produtos ou null se não for possível obter.</returns>
        Task<List<Product>> GetAllProducts(int categoryId);

        /// <summary>
        /// Obtém um produto específico pela referência.
        /// </summary>
        /// <param name="reference">A referência do produto a ser obtido.</param>
        /// <returns>O produto correspondente ou null se não for encontrado.</returns>
        Task<Product> GetProduct(string reference);

        /// <summary>
        /// Insere um novo produto.
        /// </summary>
        /// <param name="product">O produto a ser inserido.</param>
        /// <returns>ID do novo produto ou -1 se a inserção falhar.</returns>
        Task<int> InsertNewProduct(ProductToSend product);

        /// <summary>
        /// Atualiza um produto existente.
        /// </summary>
        /// <param name="product">O produto a ser atualizado.</param>
        /// <returns>ID do produto atualizado ou -1 se a atualização falhar.</returns>
        Task<int> UpdateProduct(ProductToSend product);

        /// <summary>
        /// Remove um produto existente.
        /// </summary>
        /// <param name="productId">O id do produto a ser removido.</param>
        /// <returns>Se a operação ocorreu com sucesso</returns>
        Task<int> RemoveProduct(int productId);
    }
}
