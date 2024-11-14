using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniPaymentService
{
    public interface IMoloniPaymentService
    {
        /// <summary>
        /// Obtém um método de pagamento específico pelo nome.
        /// </summary>
        /// <param name="paymentName">O nome do método de pagamento a ser obtido.</param>
        /// <returns>O método de pagamento correspondente ou null se não for encontrado.</returns>
        Task<PaymentMethod?> GetPayment(string paymentName);

        /// <summary>
        /// Insere um novo método de pagamento.
        /// </summary>
        /// <param name="methodName">O nome do método de pagamento a ser inserido.</param>
        /// <returns>ID do novo método de pagamento ou -1 se a inserção falhar.</returns>
        Task<int> InsertPaymentMethod(string methodName);
    }
}
