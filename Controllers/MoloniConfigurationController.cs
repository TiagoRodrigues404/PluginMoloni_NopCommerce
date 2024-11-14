using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Security;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework.Controllers;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Core.Domain.Catalog;
using MoloniProductCategory = Nop.Plugin.Misc.Moloni.Models.ProductCategory;
using Nop.Plugin.Misc.Moloni.Services.MoloniCategoryService;
using Nop.Services.Catalog;
using Nop.Plugin.Misc.Moloni.Services.MoloniMiscServices;
using Nop.Data;
using Nop.Plugin.Misc.Moloni.Services.MoloniTaxService;
using Nop.Services.Tax;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Misc.Moloni.Services.SubscriptionService;

namespace Nop.Plugin.Misc.Moloni.Controllers
{
    public class MoloniConfigurationController : BaseController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IMoloniCategoryService _moloniCategoryService;
        private readonly ICategoryService _categoryService;
        private readonly IMoloniMiscServices _moloniMiscServices;
        private readonly IRepository<ProductTag> _productTagRepository;
        private readonly IMoloniTaxService _moloniTaxService;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ISubscriptionService _subscriptionService;

        #endregion

        #region Ctor

        public MoloniConfigurationController(ILocalizationService localizationService,
                                             INotificationService notificationService,
                                             IPermissionService permissionService,
                                             ISettingService settingService,
                                             IStoreContext storeContext,
                                             IMoloniCategoryService moloniCategoryService,
                                             ICategoryService categoryService,
                                             IMoloniMiscServices moloniMiscServices,
                                             IRepository<ProductTag> productTagRepository,
                                             IMoloniTaxService moloniTaxService,
                                             ITaxCategoryService taxCategoryService,
                                             ISubscriptionService subscriptionService
            )
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
            _moloniCategoryService = moloniCategoryService;
            _categoryService = categoryService;
            _moloniMiscServices = moloniMiscServices;
            _productTagRepository = productTagRepository;
            _moloniTaxService = moloniTaxService;
            _taxCategoryService = taxCategoryService;
            _subscriptionService = subscriptionService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Renderiza a página de configuração para o plugin Moloni no painel de administração.
        /// Este método executa os seguintes passos:
        /// 1. Verifica se o usuário tem permissão para gerenciar métodos de pagamento.
        /// 2. Carrega as configurações atuais do Moloni para o escopo da loja ativa.
        /// 3. Cria um modelo de configuração preenchido com as configurações carregadas.
        /// 4. Retorna a visão de configuração com o modelo populado.
        /// </summary>
        /// <returns>A visão de configuração do plugin com o modelo de configuração ou uma visão de acesso negado.</returns>
        [AuthorizeAdmin]
        [Area(AreaNames.ADMIN)]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);

            var model = new ConfigurationModel
            {
                StripeEmail = MoloniSettings.StripeEmail,
                ClientId = MoloniSettings.ClientId,
                ClientSecret = MoloniSettings.ClientSecret,
                Username = MoloniSettings.Username,
                Password = MoloniSettings.Password,
                RedirectURI = MoloniSettings.RedirectURI,
                CompanyId = MoloniSettings.CompanyId,
                _key = MoloniSettings._key,
                _iv = MoloniSettings._iv
            };

            return View("~/Plugins/Misc.Moloni/Views/Configure.cshtml", model);
        }

        /// <summary>
        /// Salva as configurações fornecidas para o plugin Moloni e executa sincronizações iniciais.
        /// Este método executa os seguintes passos:
        /// 1. Verifica se o usuário tem permissão para gerenciar métodos de pagamento.
        /// 2. Valida o modelo fornecido e retorna a visão de configuração em caso de erro.
        /// 3. Carrega as configurações do Moloni para o escopo da loja ativa e atualiza-as com os valores do modelo.
        /// 4. Limpa o cache de configurações e salva as novas configurações.
        /// 5. Insere categorias do NopCommerce no Moloni e garante que a hierarquia de categorias exista.
        /// 6. Verifica e cria a propriedade "Referência do NopCommerce" no Moloni se ela ainda não existir.
        /// 7. Adiciona as tags "Produto" e "Serviço" no NopCommerce se não estiverem presentes.
        /// 8. Insere categorias de impostos do Moloni como TaxCategories no NopCommerce se não forem encontradas.
        /// 9. Exibe uma notificação de sucesso para o usuário e retorna a visão de configuração.
        /// </summary>
        /// <param name="model">Modelo de configuração preenchido pelo usuário.</param>
        /// <returns>A visão de configuração do plugin.</returns>
        [AuthorizeAdmin]
        [Area(AreaNames.ADMIN)]
        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);

            MoloniSettings.ClientId = model.ClientId;
            MoloniSettings.ClientSecret = model.ClientSecret;
            MoloniSettings.Username = model.Username;
            MoloniSettings.Password = model.Password;
            MoloniSettings.RedirectURI = model.RedirectURI;
            MoloniSettings.CompanyId = model.CompanyId;
            MoloniSettings._key = model._key;
            MoloniSettings._iv = model._iv;
            MoloniSettings.StripeEmail = model.StripeEmail;

            await _settingService.ClearCacheAsync();
            await _settingService.SaveSettingAsync(MoloniSettings);

            // Agora vamos verificar a subscrição no Stripe com base no e-mail
            var subscriptionValid = await _subscriptionService.CheckSubscription(model.StripeEmail);

            if (subscriptionValid)
            {
                // Processo para inserir as categorias
                var categories = await _categoryService.GetAllCategoriesAsync();
                foreach (var category in categories)
                {
                    await EnsureCategoryHierarchyInMoloni(category);
                }

                // Processo para inserir a propriedade
                var existingProperties = await _moloniMiscServices.GetPropertiesProducts();
                if (!existingProperties.Any(p => p.title == "Referência do NopCommerce"))
                    await _moloniMiscServices.CreateProductProperty("Referência do NopCommerce");

                // Processo para adicionar as tags
                var existingTags = await _productTagRepository.Table.Select(t => t.Name).ToListAsync();
                if (!existingTags.Contains("Produto"))
                    await _productTagRepository.InsertAsync(new ProductTag { Name = "Produto" });
                if (!existingTags.Contains("Serviço"))
                    await _productTagRepository.InsertAsync(new ProductTag { Name = "Serviço" });


                // Processo para adicionar as taxas
                var existingTaxCategories = await _taxCategoryService.GetAllTaxCategoriesAsync();
                var taxes = await _moloniTaxService.GetTaxesAndFees();
                foreach (var tax in taxes)
                {
                    if (!existingTaxCategories.Any(tc => tc.Name == tax.name))
                        await _taxCategoryService.InsertTaxCategoryAsync(new TaxCategory { Name = tax.name });
                }
            }
            
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        /// <summary>
        /// Garante que a hierarquia de categorias do NopCommerce esteja refletida no sistema Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Verifica se a categoria do NopCommerce já existe no Moloni e retorna-a se for encontrada.
        /// 2. Se a categoria tiver uma categoria-pai, garante que a categoria-pai também esteja criada no Moloni.
        /// 3. Cria uma nova categoria no Moloni se a categoria não existir, utilizando o ID e o nome da categoria do NopCommerce, e associa-a à categoria-pai, se aplicável.
        /// 4. Retorna a categoria recém-criada ou existente no Moloni.
        /// </summary>
        /// <param name="category">Categoria do NopCommerce a ser sincronizada.</param>
        /// <returns>A categoria correspondente no Moloni.</returns>
        private async Task<MoloniProductCategory> EnsureCategoryHierarchyInMoloni(Category category)
        {
            var moloniCategory = await ConvertNopCategoryToMoloniCategory(category.Id);

            if (moloniCategory != null)
                return moloniCategory;

            MoloniProductCategory parentMoloniCategory = null;
            if (category.ParentCategoryId > 0)
            {
                var parentCategory = await _categoryService.GetCategoryByIdAsync(category.ParentCategoryId);
                parentMoloniCategory = await EnsureCategoryHierarchyInMoloni(parentCategory);
            }

            var newCategoryId = await _moloniCategoryService.CreateProductCategory(category.Name, category.Id, parentMoloniCategory?.category_id ?? 0);
            moloniCategory = await _moloniCategoryService.GetProductCategory(newCategoryId);

            return moloniCategory;
        }

        /// <summary>
        /// Converte uma categoria do NopCommerce em uma categoria correspondente no Moloni, se já existente.
        /// Este método executa os seguintes passos:
        /// 1. Obtém todas as categorias de produtos no Moloni associadas ao ID da categoria-pai, se fornecido.
        /// 2. Procura e retorna a categoria correspondente no Moloni que contém o ID do NopCommerce na descrição.
        /// 3. Retorna null se nenhuma categoria correspondente for encontrada.
        /// </summary>
        /// <param name="categoryId">ID da categoria do NopCommerce.</param>
        /// <param name="parentCategoryId">ID opcional da categoria-pai no Moloni.</param>
        /// <returns>A categoria correspondente no Moloni, ou null se não for encontrada.</returns>
        private async Task<MoloniProductCategory> ConvertNopCategoryToMoloniCategory(int categoryId, int parentCategoryId = 0)
        {
            var moloniCategories = await _moloniCategoryService.GetAllProductCategories(parentCategoryId);

            if (moloniCategories != null)
                return moloniCategories.FirstOrDefault(n => !string.IsNullOrEmpty(n.description) && n.description.Contains($"NopID:{categoryId}"));

            return null;
        }

        #endregion
    }
}
