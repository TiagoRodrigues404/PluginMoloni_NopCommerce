using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniWarehouseService
{
    public interface IMoloniWarehouseService
    {
        /// <summary>
        /// Obtém a lista de todos os armazéns disponíveis no Moloni.
        /// Este método retorna todos os armazéns configurados na conta Moloni, incluindo detalhes como o nome, endereço e informações de contato.
        /// </summary>
        /// <returns>Lista de armazéns ou null se ocorrer um erro.</returns>
        Task<List<Warehouse>> GetAllWarehouses();

        /// <summary>
        /// Cria um novo armazém no Moloni.
        /// Este método é usado para adicionar um armazém à conta Moloni, fornecendo dados como título, endereço e informações de contato.
        /// </summary>
        /// <param name="warehouse">Objeto Warehouse contendo os detalhes do novo armazém.</param>
        /// <returns>ID do armazém criado ou -2 se ocorrer um erro.</returns>
        Task<int> CreateWarehouse(Warehouse warehouse);

        /// <summary>
        /// Atualiza um armazém existente no Moloni com novos dados.
        /// Este método permite modificar informações de um armazém já registrado, como o nome, endereço e dados de contato.
        /// </summary>
        /// <param name="warehouse">Objeto Warehouse contendo os novos detalhes do armazém.</param>
        /// <returns>ID do armazém atualizado ou -2 se ocorrer um erro.</returns>
        Task<int> UpdateWarehouse(Warehouse warehouse);

        /// <summary>
        /// Remove um armazém do Moloni com base no ID fornecido.
        /// Este método exclui um armazém da conta Moloni, identificando-o pelo seu ID.
        /// </summary>
        /// <param name="warehouseId">ID do armazém a ser removido.</param>
        /// <returns>Confirmação de remoção (1) ou -2 se ocorrer um erro.</returns>
        Task<int> RemoveWarehouse(int warehouseId);

    }
}
