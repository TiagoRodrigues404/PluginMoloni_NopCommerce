using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniMiscServices
{
    public class MoloniMiscServices : IMoloniMiscServices
    {
        private readonly IMoloniSettingsProvider _moloniSettings;
        private readonly IMoloniTokenService _moloniTokenService;
        private readonly IUtils _iUtils;

        /// <summary>
        /// Construtor para inicializar uma nova instância de MoloniMiscServices.
        /// </summary>
        /// <param name="moloniSettings">Configurações específicas do Moloni.</param>
        /// <param name="moloniTokenService">Serviço para gestão de tokens do Moloni.</param>
        /// <param name="utils">Utilitários para operações auxiliares.</param>
        public MoloniMiscServices(IMoloniSettingsProvider moloniSettings,
                                    IMoloniTokenService moloniTokenService,
                                    IUtils utils
            )
        {
            _moloniSettings = moloniSettings;
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
        }

        /// <summary>
        /// Obtém todas as unidades de medida disponíveis no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Define a URL da requisição com o token de acesso.
        /// 3. Cria o conteúdo da requisição com o ID da empresa.
        /// 4. Envia a requisição e processa a resposta; se falhar, retorna null.
        /// 5. Analisa a resposta JSON e converte-a para uma lista de unidades de medida.
        /// </summary>
        /// <returns>Lista de unidades de medida ou null se ocorrer um erro.</returns>
        public async Task<List<MeasurementUnit>?> GetAllMeasurementUnits()
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            var requestUrl = $"measurementUnits/getAll/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId)
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<List<MeasurementUnit>>(response);
        }

        /// <summary>
        /// Obtém todas as propriedades de produtos configuradas no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Define a URL da requisição com o token de acesso.
        /// 3. Cria o conteúdo da requisição com o ID da empresa.
        /// 4. Envia a requisição e processa a resposta; se falhar, retorna null.
        /// 5. Analisa a resposta JSON e converte-a para uma lista de propriedades de produtos.
        /// </summary>
        /// <returns>Lista de propriedades de produtos ou null se ocorrer um erro.</returns>
        public async Task<List<Properties>> GetPropertiesProducts()
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"productProperties/getAll/?access_token={accessToken}";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<List<Properties>>(response);
        }

        /// <summary>
        /// Cria uma nova propriedade de produto no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Define a URL da requisição com o token de acesso.
        /// 3. Cria o conteúdo da requisição com o ID da empresa e o nome da nova propriedade.
        /// 4. Envia a requisição e processa a resposta; se falhar, retorna -2.
        /// 5. Analisa a resposta JSON para obter o ID da nova propriedade criada.
        /// </summary>
        /// <param name="name">Nome da nova propriedade de produto.</param>
        /// <returns>ID da nova propriedade ou -2 se ocorrer um erro.</returns>
        public async Task<int> CreateProductProperty(string name)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"productProperties/insert/?access_token={accessToken}";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("title", name)
            });

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["property_id"]?.Value<int>() ?? -2;
        }

        /// <summary>
        /// Obtém uma lista de todos os países disponíveis no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Define a URL da requisição com o token de acesso.
        /// 3. Envia a requisição e processa a resposta; se falhar, retorna null.
        /// 4. Analisa a resposta JSON e converte-a para uma lista de países.
        /// </summary>
        /// <returns>Lista de países ou null se ocorrer um erro.</returns>
        public async Task<List<Country>?> GetCountries()
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            var requestUrl = $"countries/getAll/?access_token={accessToken}";

            var response = await _iUtils.SendRequestAsync(requestUrl, null!);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<List<Country>>(response);
        }

        /// <summary>
        /// Obtém a lista de todas as moedas disponíveis no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o token de acesso do Moloni e verifica se é válido.
        /// 2. Define a URL da requisição com o token de acesso.
        /// 3. Envia a requisição e processa a resposta; se falhar, retorna null.
        /// 4. Analisa a resposta JSON e converte-a para uma lista de moedas.
        /// </summary>
        /// <returns>Lista de moedas ou null se ocorrer um erro.</returns>
        public async Task<List<Currency>> GetCurrencies()
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            var requestUrl = $"currencies/getAll/?access_token={accessToken}";

            var response = await _iUtils.SendRequestAsync(requestUrl, null!);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<List<Currency>>(response);
        }
    }
}
