using MoloniAPI.Services.Utils;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniTaxService
{
    public class MoloniTaxService : IMoloniTaxService
    {
        private readonly IMoloniTokenService _moloniTokenService;
        private readonly IUtils _iUtils;
        private readonly IMoloniSettingsProvider _moloniSettings;

        /// <summary>
        /// Construtor para inicializar uma nova instância de MoloniTaxService.
        /// </summary>
        /// <param name="moloniTokenService">Serviço para gestão de tokens do Moloni.</param>
        /// <param name="utils">Utilitários para operações auxiliares.</param>
        /// <param name="moloniSettings">Configurações específicas do Moloni.</param>
        public MoloniTaxService(IMoloniTokenService moloniTokenService,
                                IUtils utils,
                                IMoloniSettingsProvider moloniSettingsProvider
            )
        {
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
            _moloniSettings = moloniSettingsProvider;
        }

        /// <summary>
        /// Obtém todas as taxas e taxas de serviço.
        /// </summary>
        /// <returns>Lista de taxas e taxas de serviço ou null se não for possível obter.</returns>
        public async Task<List<TaxAndFees>?> GetTaxesAndFees()
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;
            
            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();
            
            var requestUrl = $"taxes/getAll/?access_token={accessToken}";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<List<TaxAndFees>>(response);
        }
    }
}
