using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;
using System.Diagnostics;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniPaymentService
{
    public class MoloniPaymentService : IMoloniPaymentService
    {
        private readonly IMoloniTokenService _moloniTokenService;
        private readonly IUtils _iUtils;
        private readonly IMoloniSettingsProvider _moloniSettings;

        /// <summary>
        /// Construtor para inicializar uma nova instância de MoloniPaymentService.
        /// </summary>
        /// <param name="moloniTokenService">Serviço para gestão de tokens do Moloni.</param>
        /// <param name="utils">Utilitários para operações auxiliares.</param>
        /// <param name="moloniSettings">Configurações específicas do Moloni.</param>
        public MoloniPaymentService(IMoloniTokenService moloniTokenService,
                                    IUtils utils,
                                    IMoloniSettingsProvider moloniSettings
            )
        {
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
            _moloniSettings = moloniSettings;
        }

        /// <summary>
        /// Obtém um método de pagamento específico pelo nome.
        /// </summary>
        /// <param name="paymentName">O nome do método de pagamento a ser obtido.</param>
        /// <returns>O método de pagamento correspondente ou null se não for encontrado.</returns>
        public async Task<PaymentMethod?> GetPayment(string paymentName)
        {
            var paymentMethods = await GetPaymentMethodsAsync();
       
            return paymentMethods?.FirstOrDefault(pm => pm.name.ToLower().Equals(paymentName.ToLower()));
        }

        /// <summary>
        /// Obtém todos os métodos de pagamento disponíveis.
        /// </summary>
        /// <returns>Lista de métodos de pagamento ou null se não for possível obter.</returns>
        private async Task<List<PaymentMethod>?> GetPaymentMethodsAsync()
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            var requestUrl = $"paymentMethods/getAll/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<List<PaymentMethod>>(response);
        }

        /// <summary>
        /// Insere um novo método de pagamento.
        /// </summary>
        /// <param name="methodName">O nome do método de pagamento a ser inserido.</param>
        /// <returns>ID do novo método de pagamento ou -1 se a inserção falhar.</returns>
        public async Task<int> InsertPaymentMethod(string methodName)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"paymentMethods/insert/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("name", methodName),
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["payment_method_id"]?.Value<int>() ?? -2;
        }
    }
}
