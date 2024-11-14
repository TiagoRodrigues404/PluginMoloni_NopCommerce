using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Events;
using Nop.Plugin.Misc.Moloni.Services.MoloniCategoryService;
using Nop.Plugin.Misc.Moloni.Services.MoloniProductService;
using Nop.Plugin.Misc.Moloni.Services.SubscriptionService;
using Nop.Services.Configuration;
using Nop.Services.Events;
using System.Diagnostics;
using MoloniProductCategory = Nop.Plugin.Misc.Moloni.Models.ProductCategory;

namespace Nop.Plugin.Misc.Moloni.Controllers
{
    public class CategoryController : IConsumer<EntityInsertedEvent<Category>>,
                                      IConsumer<EntityUpdatedEvent<Category>>,
                                      IConsumer<EntityDeletedEvent<Category>>
    {

        #region Fields

        private readonly IMoloniCategoryService _moloniCategoryService;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly ISubscriptionService _subscriptionService;

        #endregion

        #region Ctor
        public CategoryController(IMoloniProductService moloniProductService,
                                  IMoloniCategoryService categoryService,
                                  IStoreContext storeContext,
                                  ISettingService settingService,
                                  ISubscriptionService subscriptionService
            )
        {
            _moloniCategoryService = categoryService;
            _storeContext = storeContext;
            _settingService = settingService;
            _subscriptionService = subscriptionService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Captura o evento de exclusão de uma categoria e remove a categoria correspondente no sistema Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém a categoria a partir do evento de exclusão e busca a categoria correspondente no Moloni usando o nome.
        /// 2. Verifica se a categoria foi encontrada no Moloni.
        /// 3. Envia uma solicitação para remover a categoria no sistema Moloni usando o ID da categoria do Moloni.
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento que contém a categoria excluída.</param>
        public async Task HandleEventAsync(EntityDeletedEvent<Category> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
            {
                var category = eventMessage.Entity;
                var moloniCategories = await _moloniCategoryService.GetAllProductCategories(0);
                var moloniCategory = moloniCategories.FirstOrDefault(n => n.name.Equals(category.Name));
                await _moloniCategoryService.RemoveProductCategory(moloniCategory.category_id);
            }
        }

        /// <summary>
        /// Captura o evento de atualização de uma categoria e sincroniza as alterações no sistema Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém a categoria a partir do evento e identifica seu ID.
        /// 2. Converte a categoria do NopCommerce para o formato de categoria do Moloni, chamando o método ConvertNopCategoryToMoloniCategory.
        /// 3. Verifica se a categoria foi encontrada no Moloni.
        /// 4. Atualiza o nome da categoria no Moloni com o novo nome da categoria no NopCommerce.
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento que contém a categoria atualizada.</param>
        public async Task HandleEventAsync(EntityUpdatedEvent<Category> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
            {
                var updatedCategory = eventMessage.Entity;
                var categoryId = updatedCategory.Id;

                var moloniCategory = await ConvertNopCategoryToMoloniCategory(categoryId);

                if (moloniCategory != null)
                    await _moloniCategoryService.UpdateProductCategory(moloniCategory.category_id, updatedCategory.Name);
            }
        }

        /// <summary>
        /// Captura o evento de inserção de uma nova categoria e cria uma categoria correspondente no sistema Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém a categoria a partir do evento de inserção.
        /// 2. Verifica se a categoria possui uma categoria-pai. Se sim, converte a categoria-pai para o formato Moloni
        /// usando o método ConvertNopCategoryToMoloniCategory.
        /// 3. Cria a nova categoria no sistema Moloni, associando-a ao ID da categoria-pai, se aplicável.
        /// 4. Envia uma solicitação para criar a categoria no Moloni, passando o nome, ID e o ID da categoria-pai.
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento que contém a categoria inserida.</param>
        public async Task HandleEventAsync(EntityInsertedEvent<Category> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
            {
                var category = eventMessage.Entity;

                if (category.ParentCategoryId > 0)
                {
                    var moloniCategory = await ConvertNopCategoryToMoloniCategory(category.ParentCategoryId);

                    if (moloniCategory != null)
                        await _moloniCategoryService.CreateProductCategory(category.Name, category.Id, moloniCategory.category_id);
                }
                else
                {
                    await _moloniCategoryService.CreateProductCategory(category.Name, category.Id, 0);
                }
            }  
        }

        /// <summary>
        /// Converte uma categoria do NopCommerce para uma categoria correspondente no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém todas as categorias no Moloni associadas ao ID da categoria-pai, se fornecido.
        /// 2. Procura e retorna a categoria correspondente no Moloni com base no ID da categoria do NopCommerce armazenado na descrição.
        /// 3. Retorna null se nenhuma correspondência for encontrada.
        /// </summary>
        /// <param name="categoryId">ID da categoria no NopCommerce.</param>
        /// <param name="parentCategoryId">ID opcional da categoria-pai no Moloni.</param>
        /// <returns>Categoria correspondente no Moloni ou null se não for encontrada.</returns>
        private async Task<MoloniProductCategory> ConvertNopCategoryToMoloniCategory(int categoryId, int parentCategoryId = 0)
        {
            var moloniCategories = await _moloniCategoryService.GetAllProductCategories(parentCategoryId);

            if (moloniCategories != null)
            {
                return moloniCategories.FirstOrDefault(n => !string.IsNullOrEmpty(n.description) && n.description.Contains($"NopID:{categoryId}"));
            }
            return null;
        }

        #endregion
    }
}
