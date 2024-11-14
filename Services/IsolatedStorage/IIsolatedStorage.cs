using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.IsolatedStorage
{
    /// <summary>
    /// Interface para o serviço de armazenamento isolado.
    /// Define métodos para salvar e carregar tokens de autenticação usando armazenamento isolado.
    /// </summary>
    public interface IIsolatedStorage
    {
        /// <summary>
        /// Salva os tokens de autenticação no armazenamento isolado.
        /// </summary>
        /// <param name="tokenResponse">Objeto contendo os tokens de autenticação a serem salvos.</param>
        void SaveTokensToIsolatedStorage(MoloniTokenResponse tokenResponse);

        /// <summary>
        /// Carrega os tokens de autenticação do armazenamento isolado.
        /// </summary>
        /// <returns>Retorna um objeto contendo os tokens de autenticação carregados.</returns>
        MoloniTokenResponse? LoadTokensFromIsolatedStorage();
    }
}
