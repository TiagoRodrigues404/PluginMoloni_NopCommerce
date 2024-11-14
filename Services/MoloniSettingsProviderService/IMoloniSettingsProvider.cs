namespace Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService
{
    public interface IMoloniSettingsProvider
    {
        /// <summary>
        /// Obtém as configurações específicas do Moloni para o escopo ativo da loja.
        /// Esse método é responsável por carregar e retornar as configurações do plugin Moloni,
        /// permitindo acesso a parâmetros essenciais, como IDs de empresa, chaves de API, e outras configurações necessárias.
        /// </summary>
        /// <returns>Um objeto <see cref="MoloniSettings"/> com as configurações do Moloni para o escopo da loja ativa.</returns>
        Task<MoloniSettings> GetMoloniSettingsAsync();
    }
}
