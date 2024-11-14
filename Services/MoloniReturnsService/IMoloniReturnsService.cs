using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniReturnsService
{
    public interface IMoloniReturnsService
    {
        /// <summary>
        /// Cria um novo documento de devolução de pagamento no Moloni.
        /// Este método permite registrar uma devolução de pagamento associada a um documento original, como uma fatura,
        /// especificando o cliente, o valor da devolução, a moeda e quaisquer notas adicionais.
        /// </summary>
        /// <param name="payment">Objeto de pagamento contendo detalhes do valor e método do pagamento.</param>
        /// <param name="associatedDocument">Documento associado à devolução, geralmente a fatura original ou outro comprovativo.</param>
        /// <param name="customer">Cliente para o qual a devolução será processada.</param>
        /// <param name="currencyId">ID da moeda utilizada para o pagamento e devolução.</param>
        /// <param name="notes">Notas opcionais relacionadas à devolução, como justificativas ou observações adicionais.</param>
        /// <returns>ID do documento de devolução criado ou -2 em caso de erro.</returns>
        Task<int> CreateNewReturn(Payment payment, AssociatedDocument associatedDocument, Customer customer, int currencyId, string notes = "");
    }
}
