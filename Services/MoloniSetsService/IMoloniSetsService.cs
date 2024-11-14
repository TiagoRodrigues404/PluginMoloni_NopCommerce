using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniSetsService
{
    public interface IMoloniSetsService
    {
        /// <summary>
        /// Obtém o ID de um conjunto de documentos com base no tipo de documento.
        /// </summary>
        /// <param name="document">O tipo de documento.</param>
        /// <returns>ID do conjunto de documentos ou -1 se não for encontrado.</returns>
        Task<int> GetSetId(DocumentTypes document);
    }
}
