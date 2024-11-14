using Nop.Core;
using Nop.Services.Configuration;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService
{
    public class MoloniSettingsProvider : IMoloniSettingsProvider
    {
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private MoloniSettings _moloniSettings;

        /// <summary>
        /// Construtor para inicializar uma nova instância de MoloniSettingsProvider.
        /// Este método inicializa o contexto da loja e o serviço de configurações, necessários para carregar as configurações do Moloni.
        /// </summary>
        /// <param name="storeContext">Contexto da loja para obter o escopo de configuração ativo.</param>
        /// <param name="settingService">Serviço de configuração para carregar as configurações do Moloni.</param>
        public MoloniSettingsProvider(IStoreContext storeContext, ISettingService settingService)
        {
            _storeContext = storeContext;
            _settingService = settingService;
        }

        /// <summary>
        /// Obtém as configurações do Moloni para o escopo da loja ativa.
        /// Este método executa os seguintes passos:
        /// 1. Verifica se as configurações já foram carregadas em `_moloniSettings`.
        /// 2. Se `_moloniSettings` for null, obtém o escopo de configuração ativo e carrega as configurações do Moloni para esse escopo.
        /// 3. Retorna as configurações do Moloni carregadas.
        /// </summary>
        /// <returns>As configurações do Moloni para o escopo da loja ativa.</returns>
        public async Task<MoloniSettings> GetMoloniSettingsAsync()
        {
            if (_moloniSettings == null)
            {
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                _moloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            }
            return _moloniSettings;
        }
    }
}
