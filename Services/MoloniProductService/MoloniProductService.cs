using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;
using System.Diagnostics;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniProductService
{
    public class MoloniProductService : IMoloniProductService
    {
        private readonly IMoloniSettingsProvider _moloniSettings;
        private readonly IMoloniTokenService _moloniTokenService;
        private readonly IUtils _iUtils;

        /// <summary>
        /// Construtor para inicializar uma nova instância de MoloniProductService.
        /// </summary>
        /// <param name="moloniSettings">Configurações específicas do Moloni.</param>
        /// <param name="moloniTokenService">Serviço para gestão de tokens do Moloni.</param>
        /// <param name="utils">Utilitários para operações auxiliares.</param>
        public MoloniProductService(IMoloniSettingsProvider moloniSettings,
                                    IMoloniTokenService moloniTokenService,
                                    IUtils utils
            )
        {
            _moloniSettings = moloniSettings;
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
        }

        /// <summary>
        /// Obtém todos os produtos de uma categoria específica.
        /// </summary>
        /// <param name="categoryId">O ID da categoria para obter os produtos.</param>
        /// <returns>Lista de produtos ou null se não for possível obter.</returns>
        public async Task<List<Product>> GetAllProducts(int categoryId)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"products/getAll/?access_token={accessToken}";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("category_id", categoryId.ToString())
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<List<Product>>(response);
        }


        /// <summary>
        /// Obtém um produto específico pela referência.
        /// </summary>
        /// <param name="reference">A referência do produto a ser obtido.</param>
        /// <returns>O produto correspondente ou null se não for encontrado.</returns>
        public async Task<Product> GetProduct(string reference)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"products/getByReference/?access_token={accessToken}";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("reference", reference)
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            var products = await _iUtils.ParseJsonResponseAsync<List<Product>>(response);
            return products?.FirstOrDefault();
        }


        /// <summary>
        /// Insere um novo produto.
        /// </summary>
        /// <param name="product">O produto a ser inserido.</param>
        /// <returns>ID do novo produto ou -2 se a inserção falhar.</returns>
        public async Task<int> InsertNewProduct(ProductToSend product)
        {
            Debug.WriteLine("Chegou Em InsertNewProduct");

            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            Debug.WriteLine("Já tem o token de acesso");

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();
            var requestUrl = $"products/insert/?access_token={accessToken}&json=true";

            var productData = new Dictionary<string, object>
            {
                { "company_id", settings.CompanyId },
                { "category_id", product.category_id },
                { "type", ((int) product.type) },
                { "name", product.name },
                { "reference", product.reference },
                { "price", product.price },
                { "unit_id", product.unit_id },
                { "has_stock", product.has_stock },
                { "stock", product.stock },
                { "at_product_category", product.at_product_category },
                { "minimum_stock", product.minimum_stock }
            };

            Debug.WriteLine($"Dados gerados atá agora: {System.Text.Json.JsonSerializer.Serialize(productData)}");

            if (!string.IsNullOrEmpty(product.summary))
                productData.Add("summary", product.summary);
            if (!string.IsNullOrEmpty(product.notes))
                productData.Add("notes", product.notes);
            if (!string.IsNullOrEmpty(product.exemption_reason))
                productData.Add("exemption_reason", product.exemption_reason);
            if (product.taxes != null && product.taxes.Any())
                productData.Add("taxes", product.taxes);
            if (product.properties != null && product.properties.Any())
                productData.Add("properties", product.properties);
            if (product.warehouses != null && product.warehouses.Any())
                productData.Add("warehouses", product.warehouses);

            Debug.WriteLine($"Dados gerados no final: {System.Text.Json.JsonSerializer.Serialize(productData)}");

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(productData), System.Text.Encoding.UTF8, "application/json");

            var response = await _iUtils.SendRequestAsync(requestUrl, jsonContent);
            Debug.WriteLine("Depois de response");
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            Debug.WriteLine("Depois de jsonObject");
            return jsonObject?["product_id"]?.Value<int>() ?? -2;
        }


        /// <summary>
        /// Atualiza um produto existente.
        /// </summary>
        /// <param name="product">O produto a ser atualizado.</param>
        /// <returns>ID do produto atualizado ou -2 se a atualização falhar.</returns>
        public async Task<int> UpdateProduct(ProductToSend product)
        {
            Debug.WriteLine("Chegou Em UpdateProduct");

            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"products/update/?access_token={accessToken}&json=true";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var productData = new Dictionary<string, object>
            {
                { "company_id", settings.CompanyId },
                { "product_id", product.product_id },
                { "category_id", product.category_id },
                { "type", ((int)product.type) },
                { "name", product.name },
                { "reference", product.reference },
                { "price", product.price },
                { "unit_id", product.unit_id },
                { "has_stock", product.has_stock },
                { "stock", product.stock },
                { "at_product_category", product.at_product_category },
                { "minimum_stock", product.minimum_stock }
            };

            if (!string.IsNullOrEmpty(product.summary))
                productData.Add("summary", product.summary);
            if (!string.IsNullOrEmpty(product.notes))
                productData.Add("notes", product.notes);
            if (!string.IsNullOrEmpty(product.exemption_reason))
                productData.Add("exemption_reason", product.exemption_reason);
            if (product.taxes != null && product.taxes.Any())
                productData.Add("taxes", product.taxes);
            if (product.properties != null && product.properties.Any())
                productData.Add("properties", product.properties);
            if (product.warehouses != null && product.warehouses.Any())
                productData.Add("warehouses", product.warehouses);

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(productData), System.Text.Encoding.UTF8, "application/json");

            var response = await _iUtils.SendRequestAsync(requestUrl, jsonContent);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["product_id"]?.Value<int>() ?? -2;
        }

        /// <summary>
        /// Remove um produto existente.
        /// </summary>
        /// <param name="productId">O id do produto a ser removido.</param>
        /// <returns>Se a operação ocorreu com sucesso</returns>
        public async Task<int> RemoveProduct(int productId)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"products/delete/?access_token={accessToken}";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("product_id", productId.ToString())
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["valid"]?.Value<int>() ?? -2;
        }
    }
}
