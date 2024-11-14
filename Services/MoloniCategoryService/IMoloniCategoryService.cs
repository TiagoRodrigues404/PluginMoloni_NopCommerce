using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniCategoryService
{
    public interface IMoloniCategoryService
    {
        /// <summary>
        /// Obtém todas as categorias de produtos disponíveis no Moloni.
        /// Este método retorna uma lista de categorias com base no ID da categoria-pai fornecido, permitindo uma hierarquia de categorias.
        /// </summary>
        /// <param name="parentId">ID da categoria-pai para buscar categorias específicas.</param>
        /// <returns>Lista de categorias de produtos ou null se não for possível obter.</returns>
        Task<List<ProductCategory>?> GetAllProductCategories(int parentId);

        /// <summary>
        /// Obtém uma categoria específica do Moloni usando seu ID.
        /// Este método é usado para recuperar os detalhes completos de uma única categoria.
        /// </summary>
        /// <param name="categoryId">ID da categoria a ser obtida.</param>
        /// <returns>Categoria de produto correspondente ou null se não for possível obter.</returns>
        Task<ProductCategory> GetProductCategory(int categoryId);

        /// <summary>
        /// Atualiza uma categoria de produto existente no Moloni.
        /// Este método permite renomear uma categoria existente com base no ID da categoria.
        /// </summary>
        /// <param name="categoryId">ID da categoria a ser atualizada.</param>
        /// <param name="name">Novo nome para a categoria.</param>
        /// <returns>ID da categoria atualizada ou -1 se a atualização falhar.</returns>
        Task<int> UpdateProductCategory(int categoryId, string name);

        /// <summary>
        /// Remove uma categoria de produto no Moloni.
        /// Este método exclui uma categoria específica com base no ID fornecido.
        /// </summary>
        /// <param name="categoryId">ID da categoria a ser removida.</param>
        /// <returns>Confirmação de remoção (1) ou -1 se a remoção falhar.</returns>
        Task<int> RemoveProductCategory(int categoryId);

        /// <summary>
        /// Cria uma nova categoria de produto no Moloni.
        /// Este método cria uma nova categoria com o nome fornecido e associa-a a uma categoria-pai, se aplicável.
        /// </summary>
        /// <param name="name">Nome da nova categoria de produto.</param>
        /// <param name="categoryId">ID da categoria no NopCommerce para referência.</param>
        /// <param name="parentId">ID da categoria-pai, se aplicável.</param>
        /// <returns>ID da nova categoria ou -1 se a criação falhar.</returns>
        Task<int> CreateProductCategory(string name, int categoryId, int parentId);
    }
}
