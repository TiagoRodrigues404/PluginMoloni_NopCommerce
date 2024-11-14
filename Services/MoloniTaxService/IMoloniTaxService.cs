using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniTaxService
{
    public interface IMoloniTaxService
    {
        /// <summary>
        /// Obtém todas as taxas e taxas de serviço.
        /// </summary>
        /// <returns>Lista de taxas e taxas de serviço ou null se não for possível obter.</returns>
        Task<List<TaxAndFees>?> GetTaxesAndFees();
    }
}
