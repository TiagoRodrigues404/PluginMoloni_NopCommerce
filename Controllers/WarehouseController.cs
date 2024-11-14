using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Misc.Moloni.Services.MoloniWarehouseService;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Events;
using MoloniWarehouse = Nop.Plugin.Misc.Moloni.Models.Warehouse;
using Nop.Plugin.Misc.Moloni.Services.MoloniTaxService;
using System.Diagnostics;
using Nop.Plugin.Misc.Moloni.Services.MoloniMiscServices;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Plugin.Misc.Moloni.Services.SubscriptionService;

namespace Nop.Plugin.Misc.Moloni.Controllers
{
    public class WarehouseController : IConsumer<EntityInsertedEvent<Warehouse>>,
                                       IConsumer<EntityUpdatedEvent<Warehouse>>,
                                       IConsumer<EntityDeletedEvent<Warehouse>>
    {
        #region Fields

        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IMoloniWarehouseService _moloniWarehouseService;
        private readonly IMoloniTaxService _moloniTaxService;
        private readonly IMoloniMiscServices _moloniMiscServices;
        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly ISubscriptionService _subscriptionService;

        #endregion

        #region Ctor
        public WarehouseController(IRepository<Warehouse> warehouseRepository,
                                   IMoloniWarehouseService moloniWarehouse,
                                   IMoloniTaxService moloniTaxService,
                                   IAddressService addressService,
                                   ICountryService countryService,
                                   IMoloniMiscServices miscServices,
                                   IStoreContext storeContext,
                                   ISettingService settingService,
                                   ISubscriptionService subscriptionService
            ) 
        {
            _warehouseRepository = warehouseRepository;
            _moloniWarehouseService = moloniWarehouse;
            _addressService = addressService;
            _countryService = countryService;
            _moloniTaxService = moloniTaxService;
            _moloniMiscServices = miscServices;
            _storeContext = storeContext;
            _settingService = settingService;
            _subscriptionService = subscriptionService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Captura o evento de inserção de um novo armazém e cria uma entrada correspondente no sistema Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o armazém a partir do evento e carrega os dados de endereço e país.
        /// 2. Encontra o país correspondente no sistema Moloni usando o nome do país.
        /// 3. Cria um novo objeto MoloniWarehouse com os dados do armazém, incluindo nome, código, endereço, código postal, cidade e país.
        /// 4. Inclui informações adicionais, como telefone, fax, nome de contato e email, se disponíveis.
        /// 5. Envia o objeto MoloniWarehouse para o serviço do Moloni para criar o novo armazém.
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento que contém o armazém inserido.</param>
        public async Task HandleEventAsync(EntityInsertedEvent<Warehouse> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
            {
                var warehouse = eventMessage.Entity;
                var address = await _addressService.GetAddressByIdAsync(warehouse.AddressId);
                var country = await _countryService.GetCountryByIdAsync(address.CountryId ?? 0);
                var moloniCountries = await _moloniMiscServices.GetCountries();
                var moloniCountry = moloniCountries.FirstOrDefault(c => c.name.ToLower().Equals(country.Name.ToLower()));

                var moloniWarehouse = new MoloniWarehouse
                {
                    title = warehouse.Name,
                    code = warehouse.Id.ToString(),
                    address = address.Address1 + " " + address.Address2,
                    zip_code = address.ZipPostalCode,
                    city = address.City,
                    country_id = moloniCountry.country_id
                };

                if (!string.IsNullOrEmpty(address.PhoneNumber))
                    moloniWarehouse.phone = address.PhoneNumber;
                if(!string.IsNullOrEmpty(address.FaxNumber))
                    moloniWarehouse.fax = address.FaxNumber;
                if(!string.IsNullOrEmpty(address.FirstName))
                    moloniWarehouse.contact_name = address.FirstName;
                if (!string.IsNullOrEmpty(address.LastName))
                    moloniWarehouse.contact_name += " " + address.LastName;
                if(!string.IsNullOrEmpty(address.Email))
                    moloniWarehouse.contact_email = address.Email;

                await _moloniWarehouseService.CreateWarehouse(moloniWarehouse);
            }
        }

        /// <summary>
        /// Captura o evento de atualização de um armazém e sincroniza as alterações no sistema Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o armazém a partir do evento e busca o armazém correspondente no sistema Moloni.
        /// 2. Carrega os dados de endereço e país para o armazém e encontra o país correspondente no Moloni.
        /// 3. Cria um novo objeto MoloniWarehouse atualizado com os dados do armazém e inclui informações como telefone, 
        /// fax, nome de contato e email, se disponíveis.
        /// 4. Envia o objeto atualizado para o serviço do Moloni para atualizar o armazém existente.
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento que contém o armazém atualizado.</param>
        public async Task HandleEventAsync(EntityUpdatedEvent<Warehouse> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
            {
                var warehouse = eventMessage.Entity;
                var moloniWarehouses = await _moloniWarehouseService.GetAllWarehouses();
                var moloniWarehouse = moloniWarehouses.FirstOrDefault(c => c.code.Equals(warehouse.Id.ToString()));

                var address = await _addressService.GetAddressByIdAsync(warehouse.AddressId);
                var country = await _countryService.GetCountryByIdAsync(address.CountryId ?? 0);
                var moloniCountries = await _moloniMiscServices.GetCountries();
                var moloniCountry = moloniCountries.FirstOrDefault(c => c.name.ToLower().Equals(country.Name.ToLower()));

                var newMoloniWarehouse = new MoloniWarehouse
                {
                    warehouse_id = moloniWarehouse.warehouse_id,
                    title = warehouse.Name,
                    code = warehouse.Id.ToString(),
                    address = address.Address1 + " " + address.Address2,
                    zip_code = address.ZipPostalCode,
                    city = address.City,
                    country_id = moloniCountry.country_id
                };

                if (!string.IsNullOrEmpty(address.PhoneNumber))
                    newMoloniWarehouse.phone = address.PhoneNumber;
                if (!string.IsNullOrEmpty(address.FaxNumber))
                    newMoloniWarehouse.fax = address.FaxNumber;
                if (!string.IsNullOrEmpty(address.FirstName))
                    newMoloniWarehouse.contact_name = address.FirstName;
                if (!string.IsNullOrEmpty(address.LastName))
                    newMoloniWarehouse.contact_name += " " + address.LastName;
                if (!string.IsNullOrEmpty(address.Email))
                    newMoloniWarehouse.contact_email = address.Email;

                await _moloniWarehouseService.UpdateWarehouse(newMoloniWarehouse);
            }
        }

        /// <summary>
        /// Captura o evento de exclusão de um armazém e remove o armazém correspondente do sistema Moloni.
        /// Este método executa os seguintes passos:
        /// 1. Obtém o armazém a partir do evento e busca o armazém correspondente no sistema Moloni usando o código do armazém.
        /// 2. Envia uma solicitação ao serviço do Moloni para remover o armazém, utilizando o ID do armazém no Moloni.
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento que contém o armazém excluído.</param>
        public async Task HandleEventAsync(EntityDeletedEvent<Warehouse> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
            {
                var warehouse = eventMessage.Entity;
                var moloniWarehouses = await _moloniWarehouseService.GetAllWarehouses();
                var moloniWarehouse = moloniWarehouses.FirstOrDefault(c => c.code.Equals(warehouse.Id.ToString()));

                await _moloniWarehouseService.RemoveWarehouse(moloniWarehouse.warehouse_id);
            }
        }

        #endregion
    }
}
