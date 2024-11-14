using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniSetsService;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniReturnsService
{
    public class MoloniReturnsService : IMoloniReturnsService
    {
        private readonly IMoloniSettingsProvider _moloniSettings;
        private readonly IMoloniSetsService _moloniSetsService;
        private readonly IMoloniTokenService _moloniTokenService;
        private readonly IUtils _iUtils;

        /// <summary>
        /// Construtor para inicializar uma nova instância de MoloniReturnsService.
        /// Este método inicializa os serviços de configurações do Moloni, gestão de tokens, utilitários e conjuntos de documentos.
        /// </summary>
        /// <param name="moloniSettings">Configurações específicas do Moloni.</param>
        /// <param name="moloniTokenService">Serviço para gestão de tokens do Moloni.</param>
        /// <param name="utils">Utilitários para operações auxiliares.</param>
        /// <param name="moloniSetsService">Serviço para obter conjuntos de documentos configurados no Moloni.</param>
        public MoloniReturnsService(IMoloniSettingsProvider moloniSettings,
                                    IMoloniTokenService moloniTokenService,
                                    IUtils utils,
                                    IMoloniSetsService moloniSetsService
            )
        {
            _moloniSettings = moloniSettings;
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
            _moloniSetsService = moloniSetsService;
        }

        /// <summary>
        /// Cria um novo documento de devolução de pagamento no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Obtém as configurações do Moloni, como o ID da empresa.
        /// 3. Define os dados da devolução, incluindo o cliente, valor, documentos associados e pagamentos.
        /// 4. Constrói a URL de requisição e define o conteúdo JSON com os dados da devolução, como ID da empresa, data, valor e notas.
        /// 5. Envia a requisição ao Moloni para criar a devolução de pagamento e processa a resposta; se falhar, retorna -2.
        /// 6. Analisa a resposta JSON para obter e retornar o ID do conjunto de documentos criado ou -2 em caso de falha.
        /// </summary>
        /// <param name="payment">Objeto de pagamento que contém detalhes do pagamento associado à devolução.</param>
        /// <param name="associatedDocument">Documento associado à devolução, como uma fatura original.</param>
        /// <param name="customer">Cliente associado à devolução.</param>
        /// <param name="currencyId">ID da moeda utilizada para a devolução.</param>
        /// <param name="notes">Notas opcionais para a devolução.</param>
        /// <returns>ID do documento de devolução criado ou -2 em caso de erro.</returns>
        public async Task<int> CreateNewReturn(Payment payment, AssociatedDocument associatedDocument, Customer customer, int currencyId, string notes = "")
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var paymentsList = new List<Payment> { payment };
            var associatedDocumentList = new List<AssociatedDocument> { associatedDocument };

            var requestUrl = $"paymentReturns/insert/?access_token={accessToken}";

            var productData = new Dictionary<string, object>
            {
                { "company_id", settings.CompanyId },
                { "date", DateTime.Now.ToString("yyyy-MM-dd") },
                { "document_set_id", await _moloniSetsService.GetSetId(DocumentTypes.ReturnPayment) },
                { "customer_id", customer.customer_id },
                { "net_value", payment.value },
                { "associated_documents", associatedDocumentList },
                { "payments", paymentsList },
                { "exchange_currency_id", currencyId },
                { "notes", notes },
                { "status", 1 }
            };

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(productData), System.Text.Encoding.UTF8, "application/json");

            var response = await _iUtils.SendRequestAsync(requestUrl, jsonContent);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["document_set_id"]?.Value<int>() ?? -2;
        }
    }
}
