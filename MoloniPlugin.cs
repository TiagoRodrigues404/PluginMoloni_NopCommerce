using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Misc.Moloni
{
    /// <summary>
    /// Plugin Moloni para o NopCommerce que integra funcionalidades específicas do Moloni, 
    /// como a configuração de página e a adição de itens ao menu de administração.
    /// </summary>
    public class MoloniPlugin : BasePlugin, IMiscPlugin, IAdminMenuPlugin
    {
        #region Fields

        private readonly IWebHelper _webHelper;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Ctor

        /// <summary>
        /// Construtor que inicializa uma nova instância de MoloniPlugin.
        /// Este método configura os serviços de helper web e de permissão necessários para o plugin.
        /// </summary>
        /// <param name="webHelper">Helper web para obter informações da loja, como a URL base.</param>
        /// <param name="permissionService">Serviço de permissão para verificar direitos de acesso ao plugin.</param>
        public MoloniPlugin(IWebHelper webHelper, IPermissionService permissionService)
        {
            _webHelper = webHelper;
            _permissionService = permissionService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Retorna a URL da página de configuração do plugin Moloni.
        /// Este método constrói a URL de configuração com base na localização da loja obtida pelo webHelper.
        /// </summary>
        /// <returns>URL da página de configuração do plugin.</returns>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/MoloniConfiguration/Configure";
        }

        /// <summary>
        /// Gerencia o menu do painel de administração, adicionando um link para a configuração do plugin Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Verifica se o usuário tem permissão para gerenciar plugins.
        /// 2. Procura o nó de configuração ("Configuration") no menu e, dentro dele, o nó de plugins locais ("Local plugins").
        /// 3. Insere um novo item de menu para o plugin Moloni na posição correta, com o título "Moloni" e um ícone específico.
        /// </summary>
        /// <param name="rootNode">Nó raiz do menu do site para adicionar o item de configuração do Moloni.</param>
        public async Task ManageSiteMapAsync(SiteMapNode rootNode)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return;

            var config = rootNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Configuration"));
            if (config == null)
                return;

            var plugins = config.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Local plugins"));

            if (plugins == null)
                return;

            var index = config.ChildNodes.IndexOf(plugins);

            if (index < 0)
                return;

            config.ChildNodes.Insert(index, new SiteMapNode
            {
                SystemName = "nopCommerce Moloni",
                Title = "Moloni",
                ControllerName = "AdminPanel",
                ActionName = "Configure",
                IconClass = "far fa-dot-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary { { "area", AreaNames.ADMIN } }
            });
        }

        #endregion
    }
}