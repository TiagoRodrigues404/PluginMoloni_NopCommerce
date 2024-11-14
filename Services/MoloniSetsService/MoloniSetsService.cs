using MoloniAPI.Services.Utils;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;
using System.Diagnostics;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniSetsService
{
    public class MoloniSetsService : IMoloniSetsService
    {
        private readonly IMoloniSettingsProvider _moloniSettings;
        private readonly IMoloniTokenService _moloniTokenService;
        private readonly IUtils _iUtils;

        /// <summary>
        /// Construtor para inicializar uma nova instância de MoloniSetsService.
        /// </summary>
        /// <param name="moloniSettings">Configurações específicas do Moloni.</param>
        /// <param name="moloniTokenService">Serviço para gestão de tokens do Moloni.</param>
        /// <param name="utils">Utilitários para operações auxiliares.</param>
        public MoloniSetsService(IMoloniSettingsProvider moloniSettings,
                                 IMoloniTokenService moloniTokenService,
                                 IUtils utils
            )
        {
            _moloniSettings = moloniSettings;
            _moloniTokenService = moloniTokenService;
            _iUtils = utils;
        }

        /// <summary>
        /// Obtém todos os conjuntos de documentos disponíveis.
        /// </summary>
        /// <returns>Lista de conjuntos de documentos ou null se não for possível obter.</returns>
        private async Task<List<DocumentSet>?> GetAllSets()
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return null;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"documentSets/getAll/?access_token={accessToken}";

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
            };

            var content = new FormUrlEncodedContent(parameters);

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return null;

            return await _iUtils.ParseJsonResponseAsync<List<DocumentSet>>(response);
        }

        /// <summary>
        /// Obtém o ID de um conjunto de documentos com base no tipo de documento.
        /// </summary>
        /// <param name="document">O tipo de documento.</param>
        /// <returns>ID do conjunto de documentos ou -1 se não for encontrado.</returns>
        public async Task<int> GetSetId(DocumentTypes document)
        {
            var documentSets = await GetAllSets();

            if (documentSets != null)
            {
                int anoAtual = DateTime.Now.Year;
                string purchaseOrderStr = $"{document}{anoAtual}";

                var documentSet = documentSets.FirstOrDefault(n => n.name!.ToLower() == purchaseOrderStr.ToLower());
                if (documentSet == null)
                    return await CreateNewSetId(purchaseOrderStr, document);
                else
                    return documentSet.document_set_id;
            }
            else
                return -1;
        }

        /// <summary>
        /// Cria um novo ID de conjunto de documentos.
        /// </summary>
        /// <param name="setName">O nome do conjunto de documentos.</param>
        /// <param name="documentType">O tipo de documento.</param>
        /// <returns>ID do novo conjunto de documentos ou -1 se a criação falhar.</returns>
        private async Task<int> CreateNewSetId(string setName, DocumentTypes documentType)
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"documentSets/insert/?access_token={accessToken}";

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("name", setName),
            };

            var content = new FormUrlEncodedContent(parameters);

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            var documentSetId = jsonObject?["document_set_id"]?.Value<int>() ?? -2;

            await RegisterSetInAT(documentSetId, documentType, null, null);

            return documentSetId;
        }

        /// <summary>
        /// Regista um conjunto de documentos na Autoridade Tributária (AT).
        /// Os campos documentSetAtCode e initialNum só são necessários caso a comunicação automatica para AT esteja desativada
        /// E deverão corresponder aos que foram registados manualmente no portal da AT
        /// </summary>
        /// <param name="documentSetId">O ID do conjunto de documentos.</param>
        /// <param name="documentType">O tipo de documento.</param>
        /// <param name="documentSetAtCode">O código do conjunto de documentos na AT (opcional).</param>
        /// <param name="initialNum">O número inicial do conjunto de documentos na AT (opcional).</param>
        /// <returns>ID do conjunto de documentos registado ou -1 se o registo falhar.</returns>
        private async Task<int> RegisterSetInAT(int documentSetId, DocumentTypes documentType, int? documentSetAtCode, int? initialNum) 
        {
            var accessToken = await _moloniTokenService.GetAccessTokenAsync();
            if (accessToken == null) return -2;

            int documentTypeId = -1;

            if (documentType == DocumentTypes.PurchaseOrder) 
            {
                documentTypeId = 28;
            } 
            else if(documentType == DocumentTypes.InvoiceReceipt)
            {
                documentTypeId = 27;
            }

            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var requestUrl = $"documentSets/ATInsertCode/?access_token={accessToken}";

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("company_id", settings.CompanyId),
                new KeyValuePair<string, string>("document_set_id", documentSetId.ToString()),
                new KeyValuePair<string, string>("document_type_id", documentTypeId.ToString())
            };

            if (documentSetAtCode != null && documentSetAtCode >= 0 && initialNum != null && initialNum >= 0)
            {
                parameters.Add(new KeyValuePair<string, string>("document_set_at_code", documentSetAtCode.ToString()!));
                parameters.Add(new KeyValuePair<string, string>("initial_num", initialNum.ToString()!));
                parameters.Add(new KeyValuePair<string, string>("initial_date", DateTime.Now.ToString("yyyy-MM-dd")));
            }                

            var content = new FormUrlEncodedContent(parameters);

            var response = await _iUtils.SendRequestAsync(requestUrl, content);
            if (response == null) return -2;

            var jsonObject = await _iUtils.ParseJsonResponseAsync<JObject>(response);
            return jsonObject?["document_set_id"]?.Value<int>() ?? -2;
        }
    }
}
