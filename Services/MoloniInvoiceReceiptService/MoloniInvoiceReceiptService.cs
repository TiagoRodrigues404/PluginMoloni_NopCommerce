using Microsoft.IdentityModel.Tokens;
using Nop.Plugin.Misc.Moloni.Services.MoloniClientService;
using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;
using Nop.Plugin.Misc.Moloni.Services.MoloniProductService;
using Nop.Plugin.Misc.Moloni.Services.MoloniSetsService;
using Nop.Plugin.Misc.Moloni.Services.MoloniOrderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniInvoiceReceiptService
{
    public class MoloniInvoiceReceiptService : IMoloniInvoiceReceiptService
    {
        private readonly IMoloniTokenService _moloniTokenService;
        private readonly IUtils _iUtils;
        private readonly IMoloniSettingsProvider _moloniSettings;
        private readonly IMoloniClientService _moloniClientService;
        private readonly IMoloniProductService _moloniProductService;
        private readonly IMoloniSetsService _moloniSetsService;
        private static readonly int OrderValidation = 30;
        private readonly IMoloniOrderService _moloniOrderService;

        /// <summary>
        /// Construtor para inicializar uma nova instância de MoloniInvoiceReceiptService.
        /// </summary>
        /// <param name="moloniTokenService">Serviço para gestão de tokens do Moloni.</param>
        /// <param name="utils">Utilitários para operações auxiliares.</param>
        /// <param name="moloniSettings">Configurações específicas do Moloni.</param>
        /// <param name="moloniClientService">Serviço de gestão de clientes do Moloni.</param>
        /// <param name="moloniProductService">Serviço de gestão de produtos do Moloni.</param>
        /// <param name="moloniSetsService">Serviço de gestão de conjuntos de documentos do Moloni.</param>
        /// <param name="moloniOrderService">Serviço de gestão de ordens do Moloni.</param>
        public MoloniInvoiceReceiptService(IMoloniTokenService moloniTokenService,
                                           IUtils utils,
                                           IMoloniSettingsProvider moloniSettings,
                                           IMoloniClientService moloniClientService,
                                           IMoloniProductService moloniProductService,
                                           IMoloniSetsService moloniSetsService,
                                           IMoloniOrderService moloniOrderService
            )
        {
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
            _moloniSettings = moloniSettings;
            _moloniClientService = moloniClientService;
            _moloniProductService = moloniProductService;
            _moloniSetsService = moloniSetsService;
            _moloniOrderService = moloniOrderService;
        }

        /// <summary>
        /// Obtém os itens e cria um recibo de fatura.
        /// </summary>
        /// <param name="receivedClient">Cliente que fez a fatura.</param>
        /// <param name="receivedProducts">Lista de produtos recebidos para a fatura.</param>
        /// <param name="payments">Lista de pagamentos associados à fatura.</param>
        /// <param name="notes">Notas adicionais para a fatura.</param>
        /// <param name="financialDiscount">Desconto financeiro aplicado na fatura.</param>
        /// <param name="specialDiscount">Desconto especial aplicado na fatura.</param>
        /// <returns>ID do recibo de fatura criado ou -1 se a criação falhar.</returns>
        public async Task<int> GetItemsAndCreateInvoiceReceipt(Customer receivedClient, 
                                                               List<ReceivedProducts> receivedProducts,
                                                               List<Payment> payments,
                                                               int orderId,
                                                               AssociatedDocument associatedDocument,
                                                               float financialDiscount,
                                                               float specialDiscount
            )
        {
            var customer = await _moloniClientService.GetClientByVatAsync(receivedClient.vat);

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

                return await CreateInvoiceReceipt(customer, products, payments, orderId, associatedDocument, financialDiscount, specialDiscount);
            }
            else
            {
                Debug.WriteLine($"O cliente com o contribuinte {receivedClient.vat} não foi encontrado");
                return -1;
            }
        }

        /// <summary>
        /// Cria um novo recibo de fatura.
        /// </summary>
        /// <param name="customer">Cliente para o qual o recibo de fatura é criado.</param>
        /// <param name="productsList">Lista de produtos incluídos no recibo de fatura.</param>
        /// <param name="payments">Lista de pagamentos associados à fatura.</param>
        /// <param name="notes">Notas adicionais para o recibo de fatura.</param>
        /// <param name="financialDiscount">Desconto financeiro aplicado no recibo de fatura.</param>
        /// <param name="specialDiscount">Desconto especial aplicado no recibo de fatura.</param>
        /// <returns>ID do recibo de fatura criado ou -1 se a criação falhar.</returns>
        private async Task<int> CreateInvoiceReceipt(Customer customer,
                                                     List<Product> productsList,
                                                     List<Payment> payments,
                                                     int orderId,
                                                     AssociatedDocument associatedDocument,
                                                     float financialDiscount,
                                                     float specialDiscount
            )
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"invoiceReceipts/insert/?access_token={accessToken}&json=true";

            int documentSetId = await _moloniSetsService.GetSetId(DocumentTypes.InvoiceReceipt);
            string myReference = $"IR{orderId}";
            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var purchaseOrderData = new
            {
                company_id = settings.CompanyId.ToString(),
                date = DateTime.Now.ToString("yyyy-MM-dd"),
                expiration_date = DateTime.Now.AddDays(OrderValidation).ToString("yyyy-MM-dd"),
                maturity_date_id = customer.maturity_date_id,
                document_set_id = documentSetId,
                customer_id = customer.customer_id,
                your_reference = myReference,
                financial_discount = financialDiscount,
                special_discount = specialDiscount,
                products = productsList,
                payments = payments,
                notes = "",
                status = 1,
                associated_documents = new List<AssociatedDocument> { associatedDocument }
            };

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(purchaseOrderData), System.Text.Encoding.UTF8, "application/json");

            Debug.WriteLine($"Dados gerados para enviar: {System.Text.Json.JsonSerializer.Serialize(purchaseOrderData)}");

            var response = await _iUtils.SendRequestAsync(requestUrl, jsonContent);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["document_id"]?.Value<int>() ?? -2;
        }

        /// <summary>
        /// Obtém um recibo de fatura específico com base nos filtros fornecidos.
        /// </summary>
        /// <param name="document_id">ID do documento do recibo.</param>
        /// <param name="customerId">ID do cliente para filtrar o recibo.</param>
        /// <param name="date">Data para filtrar o recibo.</param>
        /// <param name="number">Número do recibo para filtrar.</param>
        /// <param name="myReference">Referência personalizada do recibo.</param>
        /// <returns>O recibo de fatura correspondente ou null se não for encontrado.</returns>
        public async Task<ProductOrder?> GetInvoiceReceipt(int? document_id, int? customerId, DateTime? date, int? number, string? myReference)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            var requestUrl = $"invoiceReceipts/getOne/?access_token={accessToken}";

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
        /// Obtém o número total de recibos de fatura com base nos filtros fornecidos.
        /// </summary>
        /// <param name="customerId">ID do cliente para filtrar os recibos.</param>
        /// <param name="date">Data para filtrar os recibos.</param>
        /// <param name="year">Ano para filtrar os recibos.</param>
        /// <returns>Número total de recibos de fatura ou -1 se não for possível obter.</returns>
        private async Task<int> GetCountInvoiceReceipts(int? customerId, DateTime? date, int? year)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"invoiceReceipts/count/?access_token={accessToken}";

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
