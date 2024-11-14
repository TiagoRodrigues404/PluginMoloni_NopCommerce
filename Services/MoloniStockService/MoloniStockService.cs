using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniStockService
{
    public class MoloniStockService : IMoloniStockService
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
        public MoloniStockService(IMoloniSettingsProvider moloniSettings,
                                    IMoloniTokenService moloniTokenService,
                                    IUtils utils
            )
        {
            _moloniSettings = moloniSettings;
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
        }

        /// <summary>
        /// Insere um novo stock de produto.
        /// </summary>
        /// <param name="productId">O ID do produto.</param>
        /// <param name="qty">A quantidade de stock.</param>
        /// <param name="warehouse_id">O ID do armazém.</param>
        /// <param name="notes">Notas sobre o stock.</param>
        /// <returns>ID do movimento de stock ou -2 se a inserção falhar.</returns>
        public async Task<int> InsertNewProductStock(int productId, int qty, int? warehouse_id, string? notes)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            var requestUrl = $"productStocks/insert/?access_token={accessToken}";

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("product_id", productId.ToString()),
                new KeyValuePair<string, string>("movement_date", DateTime.Now.ToString("yyyy-mm-dd hh:mm:ss")),
                new KeyValuePair<string, string>("qty", qty.ToString())
            };

            if (warehouse_id != null)
                parameters.Add(new KeyValuePair<string, string>("warehouse_id", warehouse_id.ToString()!));
            if (notes != null)
                parameters.Add(new KeyValuePair<string, string>("notes", notes));

            var content = new FormUrlEncodedContent(parameters);

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["stock_movement_id"]?.Value<int>() ?? -2;
        }
    }
}
