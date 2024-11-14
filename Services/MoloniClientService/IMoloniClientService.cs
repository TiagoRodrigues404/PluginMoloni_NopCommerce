using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniClientService
{
    /// <summary>
    /// Interface para o serviço de cliente da Moloni.
    /// </summary>
    public interface IMoloniClientService
    {
        /// <summary>
        /// Obtém um cliente pelo e-mail.
        /// </summary>
        /// <param name="email">O e-mail do cliente.</param>
        /// <returns>Retorna um objeto Customer se encontrado, caso contrário, null.</returns>
        Task<Customer?> GetClientByEmailAsync(string email);

        /// <summary>
        /// Obtém um cliente pelo VAT (Número de Identificação Fiscal).
        /// </summary>
        /// <param name="vat">O VAT do cliente.</param>
        /// <returns>Retorna um objeto Customer se encontrado, caso contrário, null.</returns>
        Task<Customer?> GetClientByVatAsync(string vat);

        /// <summary>
        /// Insere um novo cliente.
        /// </summary>
        /// <param name="customer">O objeto Customer contendo os dados do cliente.</param>
        /// <returns>Retorna o ID do cliente inserido ou -1 em caso de erro.</returns>
        Task<int> InsertNewClient(Customer customer);

        /// <summary>
        /// Atualiza um cliente existente.
        /// </summary>
        /// <param name="customer">O objeto Customer contendo os dados atualizados do cliente.</param>
        /// <returns>Retorna o ID do cliente atualizado ou -1 em caso de erro.</returns>
        Task<int> UpdateClient(Customer customer);
    }
}