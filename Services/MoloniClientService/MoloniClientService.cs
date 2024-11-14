using Nop.Plugin.Misc.Moloni.Services.IsolatedStorage;
using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;

/// <summary>
/// Serviço que interage com a API da Moloni para operações de clientes.
/// </summary>
namespace Nop.Plugin.Misc.Moloni.Services.MoloniClientService
{
    public class MoloniClientService : IMoloniClientService
    {
        private readonly IMoloniSettingsProvider _moloniSettings;
        private readonly IMoloniTokenService _moloniTokenService;
        private readonly IUtils _iUtils;

        /// <summary>
        /// Construtor para inicializar o serviço MoloniClientService.
        /// </summary>
        /// <param name="moloniSettings">Configurações da Moloni.</param>
        /// <param name="isolatedStorage">Serviço de armazenamento isolado.</param>
        /// <param name="utils">Serviço de utilidades.</param>
        /// <param name="moloniTokenService">Serviço de token da Moloni.</param>
        public MoloniClientService(IMoloniSettingsProvider moloniSettings,
                                   IIsolatedStorage isolatedStorage,
                                   IUtils utils,
                                   IMoloniTokenService moloniTokenService
            )
        {
            _moloniSettings = moloniSettings;
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
        }

        /// <summary>
        /// Obtém um cliente por e-mail.
        /// </summary>
        /// <param name="email">E-mail do cliente a ser buscado.</param>
        /// <returns>Retorna um objeto Customer se encontrado, caso contrário, null.</returns>
        public async Task<Customer?> GetClientByEmailAsync(string email)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            var requestUrl = $"customers/getByEmail/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("email", email)
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            var customers = await _iUtils.ParseJsonResponseAsync<List<Customer>>(response);
            return customers?.FirstOrDefault(pm => pm.email.ToLower().Equals(email.ToLower()));
        }

        /// <summary>
        /// Obtém um cliente por VAT (Número de Identificação Fiscal).
        /// </summary>
        /// <param name="vat">VAT do cliente a ser buscado.</param>
        /// <returns>Retorna um objeto Customer se encontrado, caso contrário, null.</returns>
        public async Task<Customer?> GetClientByVatAsync(string vat)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            var requestUrl = $"customers/getByVat/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("vat", vat)
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            var customers = await _iUtils.ParseJsonResponseAsync<List<Customer>>(response);
            return customers?.FirstOrDefault();
        }

        /// <summary>
        /// Insere um novo cliente.
        /// </summary>
        /// <param name="customer">Objeto Customer contendo os dados do cliente.</param>
        /// <returns>Retorna o ID do cliente inserido ou -2 em caso de erro.</returns>
        public async Task<int> InsertNewClient(Customer customer)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"customers/insert/?access_token={accessToken}&json=true";

            var clientNumber = await GetClientCounter();

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var purchaseOrderData = new
            {
                company_id = settings.CompanyId.ToString(),
                vat = customer.vat,
                number = clientNumber.ToString(),
                name = customer.name,
                language_id = "1",
                address = customer.address,
                zip_code = customer.zip_code,
                city = customer.city,
                country_id = customer.country_id.ToString(),
                email = customer.email,
                phone = customer.phone,
                notes = customer.notes,
                maturity_date_id = customer.maturity_date_id.ToString(),
                payment_method_id = customer.payment_method_id.ToString(),
                salesman_id = customer.salesman_id.ToString(),
                payment_day = customer.payment_day.ToString(),
                discount = customer.discount.ToString(),
                credit_limit = customer.credit_limit.ToString(),
                delivery_method_id = customer.delivery_method_id.ToString()
            };

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(purchaseOrderData), System.Text.Encoding.UTF8, "application/json");

            var response = await _iUtils.SendRequestAsync(requestUrl, jsonContent);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["customer_id"]?.Value<int>() ?? -2;
        }

        /// <summary>
        /// Atualiza um cliente existente.
        /// </summary>
        /// <param name="customer">Objeto Customer contendo os dados atualizados do cliente.</param>
        /// <returns>Retorna o ID do cliente atualizado ou -2 em caso de erro.</returns>
        public async Task<int> UpdateClient(Customer customer)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"customers/update/?access_token={accessToken}&json=true";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var purchaseOrderData = new
            {
                company_id = settings.CompanyId.ToString(),
                customer_id = customer.customer_id.ToString(),
                vat = customer.vat,
                number = customer.number!.ToString(),
                name = customer.name,
                language_id = "1",
                address = customer.address,
                zip_code = customer.zip_code,
                city = customer.city,
                country_id = customer.country_id.ToString(),
                email = customer.email,
                phone = customer.phone,
                notes = customer.notes,
                maturity_date_id = customer.maturity_date_id.ToString(),
                payment_method_id = customer.payment_method_id.ToString(),
                salesman_id = customer.salesman_id.ToString(),
                payment_day = customer.payment_day.ToString(),
                discount = customer.discount.ToString(),
                credit_limit = customer.credit_limit.ToString(),
                delivery_method_id = customer.delivery_method_id.ToString()
            };

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(purchaseOrderData), System.Text.Encoding.UTF8, "application/json");

            var response = await _iUtils.SendRequestAsync(requestUrl, jsonContent);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["customer_id"]?.Value<int>() ?? -2;
        }

        /// <summary>
        /// Obtém o contador de clientes.
        /// </summary>
        /// <returns>Retorna o próximo número de cliente disponível ou -2 em caso de erro.</returns>
        private async Task<int> GetClientCounter()
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"customers/count/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            int receivedDocumentSetId = jsonObject?["count"]?.Value<int>() ?? -2;

            if (receivedDocumentSetId == -2) return -2;

            var finalConsumer = await GetClientByVatAsync("999999990");

            if (finalConsumer == null)
                return receivedDocumentSetId++;
            if (receivedDocumentSetId == 9999)
                return receivedDocumentSetId++;

            return receivedDocumentSetId;
        }
    }
}
