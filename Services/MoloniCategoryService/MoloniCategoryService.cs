using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniCategoryService
{
    public class MoloniCategoryService : IMoloniCategoryService
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
        public MoloniCategoryService(IMoloniSettingsProvider moloniSettings,
                                    IMoloniTokenService moloniTokenService,
                                    IUtils utils
            )
        {
            _moloniSettings = moloniSettings;
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
        }

        /// <summary>
        /// Obtém todas as categorias de produtos do Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Cria a URL de requisição com o token de acesso.
        /// 3. Define o conteúdo da requisição, incluindo o ID da empresa e o ID da categoria-pai.
        /// 4. Envia a requisição e recebe a resposta; se falhar, retorna null.
        /// 5. Analisa a resposta JSON e converte-a para uma lista de categorias de produtos.
        /// </summary>
        /// <param name="parentId">ID da categoria-pai para buscar categorias específicas.</param>
        /// <returns>Lista de categorias de produtos ou null se ocorrer um erro.</returns>
        public async Task<List<ProductCategory>?> GetAllProductCategories(int parentId)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            var requestUrl = $"productCategories/getAll/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("parent_id", parentId.ToString())
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<List<ProductCategory>>(response);
        }

        /// <summary>
        /// Obtém uma categoria específica do Moloni usando seu ID.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Cria a URL de requisição com o token de acesso.
        /// 3. Define o conteúdo da requisição, incluindo o ID da empresa e o ID da categoria.
        /// 4. Envia a requisição e recebe a resposta; se falhar, retorna null.
        /// 5. Analisa a resposta JSON e converte-a para um objeto ProductCategory.
        /// </summary>
        /// <param name="categoryId">ID da categoria a ser obtida.</param>
        /// <returns>Categoria de produto correspondente ou null se ocorrer um erro.</returns>
        public async Task<ProductCategory> GetProductCategory(int categoryId)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            var requestUrl = $"productCategories/getOne/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("category_id", categoryId.ToString())
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<ProductCategory>(response);
        }

        /// <summary>
        /// Cria uma nova categoria de produto no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Cria a URL de requisição com o token de acesso.
        /// 3. Define o conteúdo da requisição, incluindo o ID da empresa, o nome da categoria, o ID da categoria-pai e uma descrição personalizada.
        /// 4. Envia a requisição e recebe a resposta; se falhar, retorna -2.
        /// 5. Analisa a resposta JSON para obter o ID da nova categoria criada no Moloni.
        /// </summary>
        /// <param name="name">Nome da nova categoria de produto.</param>
        /// <param name="categoryId">ID da categoria no NopCommerce.</param>
        /// <param name="parentId">ID da categoria-pai, se aplicável.</param>
        /// <returns>ID da nova categoria ou -2 se ocorrer um erro.</returns>
        public async Task<int> CreateProductCategory(string name, int categoryId, int parentId)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"productCategories/insert/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("parent_id", parentId.ToString()),
                new KeyValuePair<string, string>("name", name),
                new KeyValuePair<string, string>("description", $"NopID:{categoryId}")
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["category_id"]?.Value<int>() ?? -2;
        }

        /// <summary>
        /// Atualiza uma categoria de produto existente no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Cria a URL de requisição com o token de acesso.
        /// 3. Define o conteúdo da requisição, incluindo o ID da empresa, o ID da categoria e o novo nome da categoria.
        /// 4. Envia a requisição e recebe a resposta; se falhar, retorna -2.
        /// 5. Analisa a resposta JSON para confirmar o ID da categoria atualizada.
        /// </summary>
        /// <param name="categoryId">ID da categoria no Moloni a ser atualizada.</param>
        /// <param name="name">Novo nome para a categoria.</param>
        /// <returns>ID da categoria atualizada ou -2 se ocorrer um erro.</returns>
        public async Task<int> UpdateProductCategory(int categoryId, string name)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"productCategories/update/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("category_id", categoryId.ToString()),
                new KeyValuePair<string, string>("parent_id", "0"),
                new KeyValuePair<string, string>("name", name)
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["category_id"]?.Value<int>() ?? -2;
        }

        /// <summary>
        /// Remove uma categoria de produto no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Cria a URL de requisição com o token de acesso.
        /// 3. Define o conteúdo da requisição, incluindo o ID da empresa e o ID da categoria a ser removida.
        /// 4. Envia a requisição e recebe a resposta; se falhar, retorna -2.
        /// 5. Analisa a resposta JSON para confirmar a remoção da categoria.
        /// </summary>
        /// <param name="categoryId">ID da categoria no Moloni a ser removida.</param>
        /// <returns>Confirmação de remoção (1) ou -2 se ocorrer um erro.</returns>
        public async Task<int> RemoveProductCategory(int categoryId)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"productCategories/delete/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("category_id", categoryId.ToString())
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["valid"]?.Value<int>() ?? -2;
        }
    }
}
