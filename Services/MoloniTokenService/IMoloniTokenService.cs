namespace Nop.Plugin.Misc.Moloni.Services.MoloniTokenService
{
    /// <summary>
    /// Interface para o serviço MoloniTokenService.
    /// Define métodos para obter e renovar tokens de autenticação na API Moloni.
    /// </summary>
    public interface IMoloniTokenService
    {
        /// <summary>
        /// Obtém um token de acesso.
        /// </summary>
        /// <returns>Retorna o token de acesso válido ou null se não for possível obter um token.</returns>
        Task<string?> GetAccessTokenAsync();
    }
}
