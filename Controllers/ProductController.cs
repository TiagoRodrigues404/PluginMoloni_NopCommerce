using Nop.Core.Domain.Catalog;
using Nop.Services.Events;
using Nop.Plugin.Misc.Moloni.Services.MoloniProductService;
using Nop.Core.Events;
using Nop.Services.Catalog;
using System.Diagnostics;
using Nop.Data;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Services.Tax;
using Nop.Plugin.Misc.Moloni.Services.MoloniTaxService;
using Nop.Plugin.Misc.Moloni.Services.MoloniWarehouseService;
using Product = Nop.Core.Domain.Catalog.Product;
using ProductCategory = Nop.Core.Domain.Catalog.ProductCategory;
using MoloniProductCategory = Nop.Plugin.Misc.Moloni.Models.ProductCategory;
using Nop.Plugin.Misc.Moloni.Services.MoloniMiscServices;
using Nop.Plugin.Misc.Moloni.Services.MoloniStockService;
using Nop.Plugin.Misc.Moloni.Services.MoloniCategoryService;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Plugin.Misc.Moloni.Services.SubscriptionService;

namespace Nop.Plugin.Misc.Moloni.Controllers
{
    public class ProductController : IConsumer<EntityInsertedEvent<ProductCategory>>,
                                     IConsumer<EntityUpdatedEvent<Product>>,
                                     IConsumer<EntityDeletedEvent<Product>>
                                     //IConsumer<EntityInsertedEvent<ProductProductTagMapping>>
    {
        #region Fields

        private readonly IMoloniProductService _moloniProductService;
        private readonly IMoloniTaxService _moloniTaxService;
        private readonly IMoloniWarehouseService _moloniWarehouseService;
        private readonly IMoloniMiscServices _moloniMiscServices;
        private readonly IMoloniStockService _moloniStockService;
        private readonly IMoloniCategoryService _moloniCategoryService;
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IProductTagService _productTagService;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly IRepository<ProductCategory> _productCategoryRepository;
        private readonly IRepository<ProductWarehouseInventory> _productWarehouseInventoryRepository;
        private static DateTime _lastUpdateTime = DateTime.MinValue;
        private static readonly TimeSpan _debounceInterval = TimeSpan.FromSeconds(5);
        private static int? _lastUpdatedProductId = null;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly ISubscriptionService _subscriptionService;

        #endregion

        #region Ctor

        public ProductController(IMoloniProductService moloniProductService,
                                 IMoloniTaxService moloniTaxService,
                                 IMoloniWarehouseService moloniWarehouseService,
                                 IMoloniMiscServices moloniMiscServices,
                                 ICategoryService categoryService,
                                 IProductService productService,
                                 IProductTagService productTagService,
                                 ITaxCategoryService taxCategoryService,
                                 IRepository<ProductCategory> productCategoryRepository,
                                 IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
                                 IMoloniStockService moloniStockService,
                                 IMoloniCategoryService moloniCategoryService,
                                 IStoreContext storeContext,
                                 ISettingService settingService,
                                 ISubscriptionService subscriptionService
            )
        {
            _moloniProductService = moloniProductService;
            _categoryService = categoryService;
            _productCategoryRepository = productCategoryRepository;
            _productService = productService;
            _productTagService = productTagService;
            _taxCategoryService = taxCategoryService;
            _moloniTaxService = moloniTaxService;
            _moloniWarehouseService = moloniWarehouseService;
            _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
            _moloniMiscServices = moloniMiscServices;
            _moloniStockService = moloniStockService;
            _moloniCategoryService = moloniCategoryService;
            _storeContext = storeContext;
            _settingService = settingService;
            _subscriptionService = subscriptionService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Captura o evento de inserção de uma nova categoria de produto e sincroniza essa categoria com o sistema Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém a categoria de produto a partir do evento recebido.
        /// 2. Verifica se o produto associado à categoria existe no sistema.
        /// 3. Navega pelas categorias principais e secundárias do NopCommerce para determinar a categoria correspondente no Moloni.
        /// 4. Cria uma nova entrada de produto no Moloni com a categoria, preço, descrição, stock e informações de armazém.
        /// 5. Armazena detalhes como impostos e outras propriedades relacionadas ao produto no Moloni.
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento que contém a categoria de produto inserida.</param>
        public async Task HandleEventAsync(EntityInsertedEvent<ProductCategory> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
            {
                var productCategory = eventMessage.Entity;
                var product = await _productService.GetProductByIdAsync(productCategory.ProductId);

                if (product == null)
                    return;

                // Processo para obter a categoria
                string categoryName = null;
                MoloniProductCategory moloniCategory = null;
                var originalCategory = await _categoryService.GetCategoryByIdAsync(productCategory.CategoryId);
                var currentCategory = originalCategory;

                while (currentCategory.ParentCategoryId > 0)
                {
                    var parentMoloniCategory = await ConvertNopCategoryToMoloniCategory(currentCategory.ParentCategoryId);

                    if (moloniCategory == null)
                    {
                        moloniCategory = await ConvertNopCategoryToMoloniCategory(currentCategory.Id, parentMoloniCategory.category_id);
                    }

                    currentCategory = await _categoryService.GetCategoryByIdAsync(currentCategory.ParentCategoryId);
                    categoryName = parentMoloniCategory.name;
                }

                if (moloniCategory == null)
                {
                    moloniCategory = await ConvertNopCategoryToMoloniCategory(currentCategory.Id);
                    categoryName = moloniCategory.name;
                }

                var moloniProduct = new ProductToSend
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
                    moloniProduct.summary = product.ShortDescription;

                if (!string.IsNullOrEmpty(product.FullDescription))
                    moloniProduct.notes = product.FullDescription;

                // Processo para armazenar stock
                var moloniWarehouses = await _moloniWarehouseService.GetAllWarehouses();
                var productWarehouseInventory = _productWarehouseInventoryRepository.Table.Where(w => w.ProductId == product.Id).ToList();

                if (productWarehouseInventory.Any())
                {
                    int reserved = 0;
                    foreach (var productWarehouse in productWarehouseInventory)
                    {
                        var moloniWarehouse = moloniWarehouses.FirstOrDefault(wh => wh.code.Equals(productWarehouse.WarehouseId.ToString()));

                        moloniProduct.warehouses.Add(new WarehouseToSend
                        {
                            warehouse_id = moloniWarehouse.warehouse_id,
                            stock = productWarehouse.StockQuantity
                        });

                        reserved += productWarehouse.ReservedQuantity;
                    }

                    moloniProduct.minimum_stock = reserved;
                }
                else
                {
                    var moloniWarehouse = moloniWarehouses.FirstOrDefault(wh => wh.code.Equals(product.WarehouseId.ToString()));

                    if (moloniWarehouse == null)
                        moloniWarehouse = moloniWarehouses.FirstOrDefault(wh => wh.is_default == 1);

                    moloniProduct.warehouses.Add(new WarehouseToSend
                    {
                        warehouse_id = moloniWarehouse.warehouse_id,
                        stock = product.StockQuantity
                    });

                    moloniProduct.minimum_stock = product.MinStockQuantity;
                };

                // Processo para obter Impostos
                if (product.IsTaxExempt)
                {
                    moloniProduct.exemption_reason = "M19";
                }
                else
                {
                    var moloniTaxes = await _moloniTaxService.GetTaxesAndFees();
                    var taxCategory = await _taxCategoryService.GetTaxCategoryByIdAsync(product.TaxCategoryId);
                    var moloniTax = moloniTaxes.FirstOrDefault(t => t.name.ToLower().Equals(taxCategory.Name.ToLower()));

                    moloniProduct.taxes = new List<TaxToSend>
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

                moloniProduct.properties = new List<PropertiesToSend>
                {
                    new PropertiesToSend
                    {
                        property_id = refereceProperty.property_id,
                        value = product.Id.ToString()
                    }
                };

                var result = await _moloniProductService.InsertNewProduct(moloniProduct);

                Debug.WriteLine($"Inseriu: {result}");
            }
        }

        /// <summary>
        /// Captura o evento de atualização de um produto e sincroniza as alterações desse produto com o sistema Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Verifica se o produto existe e se o evento de atualização é válido, evitando duplicados.
        /// 2. Obtém a categoria correspondente no Moloni e verifica a existência do produto.
        /// 3. Cria uma nova entrada de produto ou atualiza a existente no Moloni, incluindo dados como nome, preço, stock e descrição.
        /// 4. Atualiza a quantidade de stock no Moloni conforme necessário, com base no inventário do armazém.
        /// 5. Aplica as informações fiscais do produto, incluindo isenção de impostos, se aplicável.
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento que contém o produto atualizado.</param>
        public async Task HandleEventAsync(EntityUpdatedEvent<Product> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
            {
                var product = eventMessage.Entity;

                if (product == null)
                    return;

                if (_lastUpdatedProductId == product.Id && DateTime.UtcNow - _lastUpdateTime < _debounceInterval)
                {
                    Debug.WriteLine($"Ignorando evento duplicado para o produto ID {product.Id}");
                    return;
                }

                _lastUpdateTime = DateTime.UtcNow;
                _lastUpdatedProductId = product.Id;

                if (product.UpdatedOnUtc.AddSeconds(10) > DateTime.UtcNow)
                {
                    // Processo para obter a categoria
                    string categoryName = null;
                    MoloniProductCategory moloniCategory = null;
                    var productCategory = _productCategoryRepository.Table.FirstOrDefault(c => c.ProductId == product.Id);
                    var originalCategory = await _categoryService.GetCategoryByIdAsync(productCategory.CategoryId);
                    var currentCategory = originalCategory;

                    while (currentCategory.ParentCategoryId > 0)
                    {
                        var parentMoloniCategory = await ConvertNopCategoryToMoloniCategory(currentCategory.ParentCategoryId);

                        if (parentMoloniCategory != null)
                        {
                            if (moloniCategory == null)
                                moloniCategory = await ConvertNopCategoryToMoloniCategory(currentCategory.Id, parentMoloniCategory.category_id);

                            currentCategory = await _categoryService.GetCategoryByIdAsync(currentCategory.ParentCategoryId);
                            categoryName = parentMoloniCategory.name;
                        };
                    };

                    if (moloniCategory == null)
                    {
                        moloniCategory = await ConvertNopCategoryToMoloniCategory(currentCategory.Id);
                        categoryName = moloniCategory.name;
                    };

                    // Processo para obter o produto no Moloni
                    var moloniProd = await _moloniProductService.GetProduct(product.Sku);

                    if (moloniProd == null)
                    {
                        var allProducts = await _moloniProductService.GetAllProducts(moloniCategory.category_id);
                        var properties = await _moloniMiscServices.GetPropertiesProducts();
                        var refereceProperty = properties.FirstOrDefault(p => p.title.Equals("Referência do NopCommerce"));
                        moloniProd = allProducts.FirstOrDefault(r => r.properties != null && r.properties.Any(p => p.property_id == refereceProperty.property_id && p.value.Equals(product.Id.ToString())));
                    };

                    if (moloniProd == null)
                        return;

                    // Processo de criar objeto moloni
                    var moloniProduct = new ProductToSend
                    {
                        product_id = moloniProd.product_id,
                        category_id = moloniCategory.category_id,
                        type = ProductTypes.Product,
                        name = product.Name,
                        reference = product.Sku,
                        price = (float)product.Price,
                        unit_id = await GetProductUnit(),
                        has_stock = (product.StockQuantity > 0) ? 1 : 0,
                        stock = product.StockQuantity,
                        at_product_category = "M",
                        warehouses = new List<WarehouseToSend>()
                    };

                    if (!string.IsNullOrEmpty(product.ShortDescription))
                        moloniProduct.summary = product.ShortDescription;

                    if (!string.IsNullOrEmpty(product.FullDescription))
                        moloniProduct.notes = product.FullDescription;

                    // Processo para armazenar stock
                    var moloniWarehouses = await _moloniWarehouseService.GetAllWarehouses();
                    var productWarehouseInventory = _productWarehouseInventoryRepository.Table.Where(w => w.ProductId == product.Id).ToList();

                    if (productWarehouseInventory.Any())
                    {
                        int reserved = 0;
                        foreach (var productWarehouse in productWarehouseInventory)
                        {
                            var moloniWarehouse = moloniWarehouses.FirstOrDefault(wh => wh.code.Equals(productWarehouse.WarehouseId.ToString()));
                            var moloniStock = moloniProd.warehouses.FirstOrDefault(wh => wh.warehouse_id == moloniWarehouse.warehouse_id);
                            var stockDifference = (int)(productWarehouse.StockQuantity - moloniStock.stock);

                            if (moloniWarehouse != null && stockDifference != 0)
                                await _moloniStockService.InsertNewProductStock(moloniProduct.product_id ?? -1, stockDifference, moloniWarehouse.warehouse_id, null);

                            reserved += productWarehouse.ReservedQuantity;
                        }

                        moloniProduct.minimum_stock = reserved;
                    }
                    else
                    {
                        var moloniWarehouse = moloniWarehouses.FirstOrDefault(wh => wh.code.Equals(product.WarehouseId.ToString()));

                        if (moloniWarehouse == null)
                            moloniWarehouse = moloniWarehouses.FirstOrDefault(wh => wh.is_default == 1);

                        var moloniStock = moloniProd.warehouses.FirstOrDefault(wh => wh.warehouse_id == moloniWarehouse.warehouse_id);
                        var stockDifference = (int)(product.StockQuantity - moloniStock.stock);

                        if (stockDifference != 0)
                            await _moloniStockService.InsertNewProductStock(moloniProduct.product_id ?? -1, stockDifference, moloniWarehouse.warehouse_id, null);

                        moloniProduct.minimum_stock = product.MinStockQuantity;
                    };

                    // Processo para obter Impostos
                    if (product.IsTaxExempt)
                    {
                        moloniProduct.exemption_reason = "M19";
                    }
                    else
                    {
                        var moloniTaxes = await _moloniTaxService.GetTaxesAndFees();
                        var taxCategory = await _taxCategoryService.GetTaxCategoryByIdAsync(product.TaxCategoryId);
                        var moloniTax = moloniTaxes.FirstOrDefault(t => t.name.ToLower().Equals(taxCategory.Name.ToLower()));

                        moloniProduct.taxes = new List<TaxToSend>
                        {
                            new TaxToSend
                            {
                                tax_id = moloniTax.tax_id,
                                value = (float)product.Price,
                                order = taxCategory.DisplayOrder,
                                cumulative = 0
                            }
                        };
                    }

                    var result = await _moloniProductService.UpdateProduct(moloniProduct);
                    Debug.WriteLine($"Atualizou: {result}");
                }
            }
        }

        /// <summary>
        /// Captura o evento de exclusão de um produto e remove-o do sistema Moloni para manter a sincronização.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o produto a partir do evento recebido.
        /// 2. Verifica se o produto existe no Moloni.
        /// 3. Remove o produto do Moloni se ele for encontrado.
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento que contém o produto excluído.</param>
        public async Task HandleEventAsync(EntityDeletedEvent<Product> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
            {
                var product = eventMessage.Entity;
                var moloniProduct = await _moloniProductService.GetProduct(product.Sku);

                if (moloniProduct != null)
                    await _moloniProductService.RemoveProduct(moloniProduct.product_id ?? 0);
            }
        }

        /// <summary>
        /// Obtém o ID da categoria do produto no NopCommerce, necessária para sincronização com o Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Procura as categorias associadas ao produto no NopCommerce.
        /// 2. Retorna o ID da primeira categoria encontrada, ou -1 se nenhuma categoria estiver associada.
        /// </summary>
        /// <param name="product">Objeto produto do NopCommerce.</param>
        /// <returns>ID da categoria, ou -1 caso não seja encontrada.</returns>
        private async Task<int> GetCategoryId(Product product)
        {
            var productCategories = await _productCategoryRepository.Table.Where(pc => pc.ProductId == product.Id).ToListAsync();

            if (productCategories.Any())
            {
                var firstCategory = productCategories.FirstOrDefault();
                if (firstCategory != null)
                {
                    var category = await _categoryService.GetCategoryByIdAsync(firstCategory.CategoryId);
                    if (category != null)
                    {
                        return category.Id;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Obtém a unidade de medida do produto de acordo com os dados disponíveis no Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Solicita a lista de unidades de medida disponíveis no Moloni.
        /// 2. Procura a unidade padrão, como "uni.", e retorna seu ID.
        /// 3. Retorna -1 caso nenhuma unidade padrão seja encontrada.
        /// </summary>
        /// <returns>ID da unidade de medida.</returns>
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

        ///// <summary>
        ///// Determina o tipo do produto com base nas tags associadas no NopCommerce para que o tipo correto seja definido no Moloni.
        ///// Este método executa os seguintes passos:
        ///// 1. Obtém todas as tags do produto no NopCommerce.
        ///// 2. Avalia se as tags contêm "Produto" ou "Serviço" para definir o tipo.
        ///// 3. Retorna ProductTypes.Others se nenhuma tag correspondente for encontrada.
        ///// </summary>
        ///// <param name="product">Produto a ser verificado.</param>
        ///// <returns>O tipo do produto como enumeração ProductTypes.</returns>
        //private async Task<ProductTypes> ProductType(Product product)
        //{
        //    var productTags = await _productTagService.GetAllProductTagsByProductIdAsync(product.Id);

        //    if (productTags == null || !productTags.Any())
        //    {
        //        Debug.WriteLine("Nenhuma tag encontrada para o produto.");
        //        return ProductTypes.Others;
        //    }

        //    foreach (var tag in productTags)
        //    {
        //        if (tag.Name.Equals("Produto"))
        //            return ProductTypes.Product;
        //        else if (tag.Name.Equals("Serviço"))
        //            return ProductTypes.Service;
        //    }

        //    return ProductTypes.Others;
        //}

        ///// <summary>
        ///// Captura o evento de inserção de mapeamento de tag de produto e atualiza o tipo do produto no Moloni, se necessário.
        ///// Este método executa os seguintes passos:
        ///// 1. Obtém a tag do produto a partir do evento de inserção.
        ///// 2. Verifica o tipo do produto com base nas tags e obtém o produto correspondente no Moloni.
        ///// 3. Atualiza o tipo do produto no Moloni com base na tag, incluindo informações adicionais como imposto e propriedades.
        ///// </summary>
        ///// <param name="eventMessage">Mensagem do evento que contém o mapeamento da tag do produto.</param>
        //public async Task HandleEventAsync(EntityInsertedEvent<ProductProductTagMapping> eventMessage)
        //{
        //    var productTag = eventMessage.Entity;
        //    var product = await _productService.GetProductByIdAsync(productTag.Id);

        //    if (product != null)
        //    {
        //        var productType = await ProductType(product);
        //        var moloniProduct = await _moloniProductService.GetProduct(product.Sku);

        //        if (moloniProduct != null)
        //        {
        //            moloniProduct.type = productType;

        //            var tempProduct = new ProductToSend
        //            {
        //                product_id = moloniProduct.product_id,
        //                reference = moloniProduct.reference,
        //                type = productType,
        //                name = moloniProduct.name,
        //                price = moloniProduct.price,
        //                unit_id = moloniProduct.unit_id,
        //                has_stock = moloniProduct.has_stock,
        //                stock = moloniProduct.stock,
        //                minimum_stock = moloniProduct.minimum_stock,
        //                at_product_category = moloniProduct.at_product_category,
        //                created = moloniProduct.created,
        //            };

        //            if (!string.IsNullOrEmpty(moloniProduct.exemption_reason))
        //                tempProduct.exemption_reason = moloniProduct.exemption_reason;
        //            if (!string.IsNullOrEmpty(moloniProduct.summary))
        //                tempProduct.summary = moloniProduct.summary;
        //            if (!string.IsNullOrEmpty(moloniProduct.notes))
        //                tempProduct.notes = moloniProduct.notes;

        //            if (moloniProduct.taxes != null)
        //            {
        //                tempProduct.taxes = new List<TaxToSend>();

        //                foreach (var tax in moloniProduct.taxes)
        //                {
        //                    tempProduct.taxes.Add(new TaxToSend
        //                    {
        //                        tax_id = tax.tax_id,
        //                        value = tax.value,
        //                        order = tax.order,
        //                        cumulative = tax.cumulative
        //                    });
        //                }
        //            };

        //            if (moloniProduct.properties != null)
        //            {
        //                tempProduct.properties = new List<PropertiesToSend>();

        //                foreach (var properties in moloniProduct.properties)
        //                {
        //                    tempProduct.properties.Add(new PropertiesToSend
        //                    {
        //                        property_id = properties.property_id,
        //                        value = properties.value
        //                    });
        //                }
        //            };

        //            if (moloniProduct.warehouses != null)
        //            {
        //                tempProduct.warehouses = new List<WarehouseToSend>();

        //                foreach (var warehouse in moloniProduct.warehouses)
        //                {
        //                    tempProduct.warehouses.Add(new WarehouseToSend
        //                    {
        //                        stock = warehouse.stock,
        //                        warehouse_id = warehouse.warehouse_id
        //                    });
        //                }
        //            };

        //            await _moloniProductService.UpdateProduct(tempProduct);

        //            Debug.WriteLine($"Atualizou o tipo de produto no Moloni: {productType}");
        //        }
        //    }
        //}

        /// <summary>
        /// Converte uma categoria do NopCommerce para o formato de categoria do Moloni, criando ou mapeando-a corretamente.
        /// Este método executa os seguintes passos:
        /// 1. Obtém todas as categorias de produtos a partir do Moloni com base no ID da categoria-pai, se fornecido.
        /// 2. Procura a categoria correspondente no Moloni pelo ID ou descrição que contenha "NopID".
        /// 3. Retorna a categoria do Moloni correspondente, ou null caso não seja encontrada.
        /// </summary>
        /// <param name="categoryId">ID da categoria no NopCommerce.</param>
        /// <param name="parentCategoryId">ID opcional da categoria-pai, utilizado para organização hierárquica no Moloni.</param>
        /// <returns>Categoria correspondente do Moloni ou null se não for encontrada.</returns>
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