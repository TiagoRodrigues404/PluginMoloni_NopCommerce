using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniWarehouseService
{
    public class MoloniWarehouseService : IMoloniWarehouseService
    {
        private readonly IMoloniTokenService _moloniTokenService;
        private readonly IUtils _iUtils;
        private readonly IMoloniSettingsProvider _moloniSettings;

        /// <summary>
        /// Construtor para inicializar uma nova instância de MoloniWarehouseService.
        /// Este método inicializa os serviços de token, utilitários e configurações do Moloni, necessários para as operações de armazém.
        /// </summary>
        /// <param name="moloniTokenService">Serviço para gestão de tokens do Moloni.</param>
        /// <param name="utils">Utilitários para operações auxiliares, como envio de requisições.</param>
        /// <param name="moloniSettingsProvider">Provedor de configurações do Moloni.</param>
        public MoloniWarehouseService(IMoloniTokenService moloniTokenService,
        IUtils utils,
                                      IMoloniSettingsProvider moloniSettingsProvider)
        {
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
            _moloniSettings = moloniSettingsProvider;
        }

        /// <summary>
        /// Obtém todos os armazéns disponíveis no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Define a URL de requisição com o token de acesso e os parâmetros de configuração da empresa.
        /// 3. Envia a requisição e processa a resposta; se falhar, retorna null.
        /// 4. Analisa a resposta JSON e converte-a para uma lista de armazéns.
        /// </summary>
        /// <returns>Lista de armazéns ou null se ocorrer um erro.</returns>
        public async Task<List<Warehouse>> GetAllWarehouses()
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"warehouses/getAll/?access_token={accessToken}";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<List<Warehouse>>(response);
        }

        /// <summary>
        /// Cria um novo armazém no Moloni com base nos detalhes fornecidos.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Define a URL de requisição e cria o conteúdo JSON com os dados do novo armazém, como título, endereço e contato.
        /// 3. Envia a requisição ao Moloni para criar o armazém e processa a resposta; se falhar, retorna -2.
        /// 4. Analisa a resposta JSON para obter e retornar o ID do armazém criado.
        /// </summary>
        /// <param name="warehouse">Objeto Warehouse contendo os detalhes do novo armazém.</param>
        /// <returns>ID do armazém criado ou -2 se ocorrer um erro.</returns>
        public async Task<int> CreateWarehouse(Warehouse warehouse)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"warehouses/insert/?access_token={accessToken}&json=true";

            var productData = new Dictionary<string, object>
            {
                { "company_id", settings.CompanyId.ToString() },
                { "title", warehouse.title },
                { "code", warehouse.code },
                { "address", warehouse.address },
                { "city", warehouse.city },
                { "zip_code", warehouse.zip_code },
                { "country_id", warehouse.country_id.ToString() },
            };

            if (warehouse.is_default != null)
                productData.Add("is_default", warehouse.is_default.ToString());
            if (!string.IsNullOrEmpty(warehouse.phone))
                productData.Add("phone", warehouse.phone);
            if (!string.IsNullOrEmpty(warehouse.fax))
                productData.Add("fax", warehouse.fax);
            if (!string.IsNullOrEmpty(warehouse.contact_name))
                productData.Add("contact_name", warehouse.contact_name);
            if (!string.IsNullOrEmpty(warehouse.contact_email))
                productData.Add("contact_email", warehouse.contact_email);

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(productData), System.Text.Encoding.UTF8, "application/json");

            var response = await _iUtils.SendRequestAsync(requestUrl, jsonContent);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["warehouse_id"]?.Value<int>() ?? -2;
        }

        /// <summary>
        /// Atualiza um armazém existente no Moloni com novos dados fornecidos.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Define a URL de requisição e cria o conteúdo JSON com os dados atualizados do armazém.
        /// 3. Envia a requisição ao Moloni para atualizar o armazém e processa a resposta; se falhar, retorna -2.
        /// 4. Analisa a resposta JSON para obter e retornar o ID do armazém atualizado.
        /// </summary>
        /// <param name="warehouse">Objeto Warehouse contendo os novos detalhes do armazém.</param>
        /// <returns>ID do armazém atualizado ou -2 se ocorrer um erro.</returns>
        public async Task<int> UpdateWarehouse(Warehouse warehouse)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"warehouses/update/?access_token={accessToken}&json=true";

            var productData = new Dictionary<string, object>
            {
                { "company_id", settings.CompanyId.ToString() },
                { "warehouse_id", warehouse.warehouse_id },
                { "title", warehouse.title },
                { "code", warehouse.code },
                { "address", warehouse.address },
                { "city", warehouse.city },
                { "zip_code", warehouse.zip_code },
                { "country_id", warehouse.country_id.ToString() },
            };

            if (warehouse.is_default != null)
                productData.Add("is_default", warehouse.is_default.ToString());
            if (!string.IsNullOrEmpty(warehouse.phone))
                productData.Add("phone", warehouse.phone);
            if (!string.IsNullOrEmpty(warehouse.fax))
                productData.Add("fax", warehouse.fax);
            if (!string.IsNullOrEmpty(warehouse.contact_name))
                productData.Add("contact_name", warehouse.contact_name);
            if (!string.IsNullOrEmpty(warehouse.contact_email))
                productData.Add("contact_email", warehouse.contact_email);

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(productData), System.Text.Encoding.UTF8, "application/json");

            var response = await _iUtils.SendRequestAsync(requestUrl, jsonContent);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["warehouse_id"]?.Value<int>() ?? -2;
        }

        /// <summary>
        /// Remove um armazém do Moloni com base no ID fornecido.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Define a URL de requisição e os parâmetros necessários, incluindo o ID da empresa e do armazém a ser removido.
        /// 3. Envia a requisição ao Moloni para remover o armazém e processa a resposta; se falhar, retorna -2.
        /// 4. Analisa a resposta JSON para confirmar e retornar o resultado da remoção.
        /// </summary>
        /// <param name="warehouseId">ID do armazém a ser removido.</param>
        /// <returns>Confirmação de remoção (1) ou -2 se ocorrer um erro.</returns>
        public async Task<int> RemoveWarehouse(int warehouseId)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"warehouses/delete/?access_token={accessToken}";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("warehouse_id", warehouseId.ToString()),
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["valid"]?.Value<int>() ?? -2;
        }
    }
}
