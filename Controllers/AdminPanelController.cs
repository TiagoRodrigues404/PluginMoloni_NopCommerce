using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Services.Security;
using Nop.Plugin.Misc.Moloni.Services.MoloniProductService;
using Nop.Services.Catalog;
using MoloniProductCategory = Nop.Plugin.Misc.Moloni.Models.ProductCategory;
using Nop.Plugin.Misc.Moloni.Services.MoloniCategoryService;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniMiscServices;
using Nop.Plugin.Misc.Moloni.Services.MoloniWarehouseService;
using Nop.Data;
using Nop.Plugin.Misc.Moloni.Services.MoloniTaxService;
using Nop.Services.Tax;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Plugin.Misc.Moloni.Services.SubscriptionService;

namespace Nop.Plugin.Misc.Moloni.Controllers;

[AutoValidateAntiforgeryToken]
[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
public class AdminPanelController : BasePluginController
{
    #region Fields

    private readonly IPermissionService _permissionService;
    private readonly IMoloniProductService _moloniProductService;
    private readonly IMoloniCategoryService _moloniCategoryService;
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly IMoloniMiscServices _moloniMiscServices;
    private readonly IMoloniWarehouseService _moloniWarehouseService;
    private readonly IRepository<ProductWarehouseInventory> _productWarehouseInventoryRepository;
    private readonly IMoloniTaxService _moloniTaxService;
    private readonly ITaxCategoryService _taxCategoryService;
    private readonly IStoreContext _storeContext;
    private readonly ISettingService _settingService;
    private readonly ISubscriptionService _subscriptionService;

    #endregion

    #region Ctor 

    public AdminPanelController(IPermissionService permissionService,
                                IMoloniProductService moloniProductService,
                                IMoloniCategoryService moloniCategoryService,
                                ICategoryService categoryService,
                                IProductService productService,
                                IMoloniMiscServices moloniMiscServices,
                                IMoloniWarehouseService moloniWarehouseService,
                                IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
                                IMoloniTaxService moloniTaxService,
                                ITaxCategoryService taxCategoryService,
                                IStoreContext storeContext,
                                ISettingService settingService,
                                ISubscriptionService subscriptionService
        )
    {
        _permissionService = permissionService;
        _moloniProductService = moloniProductService;
        _moloniCategoryService = moloniCategoryService;
        _categoryService = categoryService;
        _productService = productService;
        _moloniMiscServices = moloniMiscServices;
        _moloniWarehouseService = moloniWarehouseService;
        _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
        _moloniTaxService = moloniTaxService;
        _taxCategoryService = taxCategoryService;
        _storeContext = storeContext;
        _settingService = settingService;
        _subscriptionService = subscriptionService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Renderiza a página de configuração do plugin no painel de administração.
    /// Este método executa os seguintes passos:
    /// 1. Verifica se o usuário tem permissão para gerenciar plugins.
    /// 2. Retorna a visão de configuração caso a permissão seja concedida; caso contrário, retorna uma visão de acesso negado.
    /// </summary>
    /// <returns>A visão de configuração do plugin ou uma visão de acesso negado.</returns>
    public virtual async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
            return AccessDeniedView();

        return View("~/Plugins/Misc.Moloni/Views/Admin/Configure.cshtml");
    }

    /// <summary>
    /// Sincroniza os produtos do NopCommerce com o sistema Moloni, criando-os no Moloni caso ainda não existam.
    /// Este método executa os seguintes passos:
    /// 1. Obtém todas as categorias de produtos do NopCommerce.
    /// 2. Para cada categoria, verifica se a hierarquia de categorias existe no Moloni e cria-a caso não exista.
    /// 3. Obtém os produtos dentro de cada categoria e verifica se o produto já existe no Moloni.
    /// 4. Se o produto não existir no Moloni, cria um novo produto no sistema com dados como nome, referência, preço, stock e unidade de medida.
    /// 5. Define informações de armazém e stock para o produto no Moloni.
    /// 6. Adiciona as informações fiscais ao produto, verificando se ele está isento de impostos.
    /// 7. Armazena o ID do produto do NopCommerce como uma propriedade no Moloni para referência futura.
    /// 8. Retorna uma resposta JSON indicando o sucesso da operação.
    /// </summary>
    /// <returns>Um objeto JSON com a mensagem de sucesso.</returns>
    [HttpPost]
    public async Task<JsonResult> SyncProducts()
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
        var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

        if (subscriptionActive)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();

            foreach (var category in categories)
            {
                var moloniCategory = await EnsureCategoryHierarchyInMoloni(category);
                var productsInCategory = await _productService.SearchProductsAsync(categoryIds: new List<int> { category.Id });

                foreach (var product in productsInCategory)
                {
                    var moloniProduct = await _moloniProductService.GetProduct(product.Sku);

                    if (moloniProduct == null)
                    {
                        var newMoloniProduct = new ProductToSend
                        {
                            category_id = moloniCategory.category_id,
                            type = ProductTypes.Product,
                            name = product.Name,
                            reference = product.Sku,
                            price = (float)product.Price,
                            unit_id = await GetProductUnit(),
                            has_stock = (product.StockQuantity > 0) ? 1 : 0,
                            stock = product.StockQuantity,
                            at_product_category = "M",
                            created = DateTime.UtcNow,
                            warehouses = new List<WarehouseToSend>()
                        };

                        if (!string.IsNullOrEmpty(product.ShortDescription))
                            newMoloniProduct.summary = product.ShortDescription;

                        if (!string.IsNullOrEmpty(product.FullDescription))
                            newMoloniProduct.notes = product.FullDescription;

                        // Processo para armazenar stock
                        var moloniWarehouses = await _moloniWarehouseService.GetAllWarehouses();
                        var productWarehouseInventory = _productWarehouseInventoryRepository.Table.Where(w => w.ProductId == product.Id).ToList();

                        if (productWarehouseInventory.Any())
                        {
                            int reserved = 0;
                            foreach (var productWarehouse in productWarehouseInventory)
                            {
                                var moloniWarehouse = moloniWarehouses.FirstOrDefault(wh => wh.code.Equals(productWarehouse.WarehouseId.ToString()));

                                newMoloniProduct.warehouses.Add(new WarehouseToSend
                                {
                                    warehouse_id = moloniWarehouse.warehouse_id,
                                    stock = productWarehouse.StockQuantity
                                });

                                reserved += productWarehouse.ReservedQuantity;
                            }

                            newMoloniProduct.minimum_stock = reserved;
                        }
                        else
                        {
                            var moloniWarehouse = moloniWarehouses.FirstOrDefault(wh => wh.code.Equals(product.WarehouseId.ToString()));

                            if (moloniWarehouse == null)
                                moloniWarehouse = moloniWarehouses.FirstOrDefault(wh => wh.is_default == 1);

                            newMoloniProduct.warehouses.Add(new WarehouseToSend
                            {
                                warehouse_id = moloniWarehouse.warehouse_id,
                                stock = product.StockQuantity
                            });

                            newMoloniProduct.minimum_stock = product.MinStockQuantity;
                        };

                        // Processo para obter Impostos
                        if (product.IsTaxExempt)
                        {
                            newMoloniProduct.exemption_reason = "M19";
                        }
                        else
                        {
                            var moloniTaxes = await _moloniTaxService.GetTaxesAndFees();
                            var taxCategory = await _taxCategoryService.GetTaxCategoryByIdAsync(product.TaxCategoryId);
                            var moloniTax = moloniTaxes.FirstOrDefault(t => t.name.ToLower().Equals(taxCategory.Name.ToLower()));

                            newMoloniProduct.taxes = new List<TaxToSend>
                        {
                            new TaxToSend
                            {
                                tax_id = moloniTax.tax_id,
                                value = (float)product.Price,
                                order = 0,
                                cumulative = 0
                            }
                        };
                        }

                        // Processo de injeção do ID do produto no Moloni
                        var properties = await _moloniMiscServices.GetPropertiesProducts();
                        var refereceProperty = properties.FirstOrDefault(p => p.title.Equals("Referência do NopCommerce"));

                        newMoloniProduct.properties = new List<PropertiesToSend>
                    {
                        new PropertiesToSend
                        {
                            property_id = refereceProperty.property_id,
                            value = product.Id.ToString()
                        }
                    };

                        var result = await _moloniProductService.InsertNewProduct(newMoloniProduct);
                    }
                }
            }

            return Json(new { message = "Ação executada com sucesso!" });
        }
        
        return Json(new { message = "Não tem subscrição ativa!" });
    }

    /// <summary>
    /// Garante que a hierarquia de categorias do NopCommerce seja refletida no Moloni.
    /// Este método executa os seguintes passos:
    /// 1. Verifica se a categoria do NopCommerce já existe no Moloni e retorna-a se for encontrada.
    /// 2. Se a categoria tiver uma categoria-pai, garante que a categoria-pai também esteja criada no Moloni.
    /// 3. Cria uma nova categoria no Moloni se a categoria não existir, utilizando o ID e nome da categoria do NopCommerce,
    /// e associa à categoria-pai, se aplicável.
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

    /// <summary>
    /// Obtém a unidade de medida padrão para produtos no Moloni.
    /// Este método executa os seguintes passos:
    /// 1. Recupera todas as unidades de medida disponíveis no Moloni.
    /// 2. Procura e retorna o ID da unidade de medida correspondente ao nome curto "uni.".
    /// 3. Retorna -1 se nenhuma unidade de medida correspondente for encontrada.
    /// </summary>
    /// <returns>ID da unidade de medida ou -1 se não for encontrada.</returns>
    private async Task<int> GetProductUnit()
    {
        var measurementUnits = await _moloniMiscServices.GetAllMeasurementUnits();
        if (measurementUnits != null)
        {
            var unit = measurementUnits.FirstOrDefault(n => n.short_name.ToLower().Equals("uni."));
            return unit.unit_id;
        }

        return -1;
    }
    #endregion
}