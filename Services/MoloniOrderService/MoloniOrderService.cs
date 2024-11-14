using Microsoft.IdentityModel.Tokens;
using Nop.Plugin.Misc.Moloni.Services.MoloniClientService;
using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;
using Nop.Plugin.Misc.Moloni.Services.MoloniProductService;
using Nop.Plugin.Misc.Moloni.Services.MoloniSetsService;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Core.Domain.Directory;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniOrderService
{
    public class MoloniOrderService : IMoloniOrderService
    {
        private static readonly int OrderValidation = 30;

        private readonly IMoloniSettingsProvider _moloniSettings;
        private readonly IMoloniTokenService _moloniTokenService;
        private readonly IUtils _iUtils;
        private readonly IMoloniClientService _moloniClientService;
        private readonly IMoloniProductService _moloniProductService;
        private readonly IMoloniSetsService _moloniSetsService;

        /// <summary>
        /// Construtor para inicializar uma nova instância de MoloniOrderService.
        /// </summary>
        /// <param name="moloniSettings">Configurações específicas do Moloni.</param>
        /// <param name="moloniTokenService">Serviço para gestão de tokens do Moloni.</param>
        /// <param name="utils">Utilitários para operações auxiliares.</param>
        /// <param name="moloniClientService">Serviço de gestão de clientes do Moloni.</param>
        /// <param name="moloniProductService">Serviço de gestão de produtos do Moloni.</param>
        /// <param name="moloniSetsService">Serviço de gestão de conjuntos de documentos do Moloni.</param>
        public MoloniOrderService(IMoloniSettingsProvider moloniSettings,
                                  IMoloniTokenService moloniTokenService,
                                  IUtils utils,
                                  IMoloniClientService moloniClientService,
                                  IMoloniProductService moloniProductService,
                                  IMoloniSetsService moloniSetsService)
        {
            _moloniSettings = moloniSettings;
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
            _moloniClientService = moloniClientService;
            _moloniProductService = moloniProductService;
            _moloniSetsService = moloniSetsService;
        }

        /// <summary>
        /// Obtém itens e cria uma ordem de compra.
        /// </summary>
        /// <param name="receivedClient">Cliente que fez a ordem.</param>
        /// <param name="receivedProducts">Lista de produtos recebidos para a ordem.</param>
        /// <param name="notes">Notas adicionais para a ordem.</param>
        /// <param name="discount">Desconto aplicado na ordem.</param>
        /// <returns>ID da ordem de compra criada ou -1 se a criação falhar.</returns>
        public async Task<int> GetItemsAndMakeOrder(Customer customer, List<ReceivedProducts> receivedProducts, int currencyId, int orderId, float discount = 0)
        {
            if (customer != null)
            {
                var products = new List<Product>();
                foreach (var productItem in receivedProducts)
                {
                    var currentProduct = await _moloniProductService.GetProduct(productItem.Reference);
                    if (currentProduct != null)
                    {
                        currentProduct.qty = productItem.Quantity;
                        products.Add(currentProduct);
                    }
                    else
                    {
                        Debug.WriteLine($"O produto com a referência {productItem.Reference} não foi encontrado");
                        return -1;
                    }
                }

                return await CreatePurchaseOrder(customer, products, orderId, discount, currencyId);
            }
            else
            {
                Debug.WriteLine($"O cliente com o e-mail {customer.email} não foi encontrado");
                return -1;
            }
        }

        /// <summary>
        /// Cria uma nova ordem de compra.
        /// </summary>
        /// <param name="customer">Cliente para o qual a ordem de compra é criada.</param>
        /// <param name="productsList">Lista de produtos incluídos na ordem de compra.</param>
        /// <param name="notes">Notas adicionais para a ordem.</param>
        /// <param name="discount">Desconto aplicado na ordem.</param>
        /// <returns>ID da ordem de compra criada ou -1 se a criação falhar.</returns>
        private async Task<int> CreatePurchaseOrder(Customer customer, List<Product> productsList, int orderId, float discount, int currencyId)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"purchaseOrder/insert/?access_token={accessToken}&json=true";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            int documentSetId = await _moloniSetsService.GetSetId(DocumentTypes.PurchaseOrder);
            string myReference = $"PO{orderId}";

            var purchaseOrderData = new
            {
                company_id = settings.CompanyId.ToString(),
                date = DateTime.Now.ToString("yyyy-MM-dd"),
                expiration_date = DateTime.Now.AddDays(OrderValidation).ToString("yyyy-MM-dd"),
                maturity_date_id = customer.maturity_date_id,
                document_set_id = documentSetId,
                customer_id = customer.customer_id,
                your_reference = myReference,
                special_discount = discount,
                products = productsList,
                delivery_destination_address = customer.address,
                delivery_destination_city = customer.city,
                delivery_destination_zip_code = customer.zip_code,
                delivery_destination_country = customer.country_id,
                exchange_currency_id = currencyId,
                notes = "",
                status = 1
            };

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(purchaseOrderData), System.Text.Encoding.UTF8, "application/json");

            Debug.WriteLine($"Dados gerados para enviar: {System.Text.Json.JsonSerializer.Serialize(purchaseOrderData)}");

            var response = await _iUtils.SendRequestAsync(requestUrl, jsonContent);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["document_id"]?.Value<int>() ?? -2;
        }

        /// <summary>
        /// Obtém uma ordem de compra específica com base nos filtros fornecidos.
        /// </summary>
        /// <param name="document_id">ID do documento da ordem.</param>
        /// <param name="customerId">ID do cliente para filtrar a ordem.</param>
        /// <param name="date">Data para filtrar a ordem.</param>
        /// <param name="number">Número da ordem para filtrar.</param>
        /// <param name="myReference">Referência personalizada da ordem.</param>
        /// <returns>A ordem de compra correspondente ou null se não for encontrada.</returns>
        public async Task<ProductOrder?> GetPurchaseOrder(int? document_id, int? customerId, DateTime? date, int? number, string? myReference)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            var requestUrl = $"purchaseOrder/getOne/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
            };

            if (document_id != null && document_id >= 0)
                parameters.Add(new KeyValuePair<string, string>("document_id", document_id.ToString()!));
            if (customerId != null && customerId >= 0)
                parameters.Add(new KeyValuePair<string, string>("customer_id", customerId.ToString()!));
            if (date != null)
                parameters.Add(new KeyValuePair<string, string>("date", date.Value.ToString("yyyy-MM-dd")));
            if (number != null && number >= 0)
                parameters.Add(new KeyValuePair<string, string>("number", number.ToString()!));
            if (!myReference.IsNullOrEmpty())
                parameters.Add(new KeyValuePair<string, string>("your_reference", myReference!));

            var content = new FormUrlEncodedContent(parameters);

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<ProductOrder>(response);
        }

        /// <summary>
        /// Obtém o número total de ordens de compra com base nos filtros fornecidos.
        /// </summary>
        /// <param name="customerId">ID do cliente para filtrar as ordens.</param>
        /// <param name="date">Data para filtrar as ordens.</param>
        /// <param name="year">Ano para filtrar as ordens.</param>
        /// <returns>Número total de ordens de compra ou -1 se não for possível obter.</returns>
        private async Task<int> GetCountPurchaseOrders(int? customerId, DateTime? date, int? year)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"purchaseOrder/count/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
            };

            if (customerId != null && customerId >= 0)
                parameters.Add(new KeyValuePair<string, string>("customer_id", customerId.ToString()!));
            if (date != null)
                parameters.Add(new KeyValuePair<string, string>("date", date.Value.ToString("yyyy-MM-dd")));
            if (year != null && year >= 0)
                parameters.Add(new KeyValuePair<string, string>("year", year.ToString()!));

            var content = new FormUrlEncodedContent(parameters);

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["count"]?.Value<int>() ?? -2;
        }
    }
}