using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Events;
using Nop.Plugin.Misc.Moloni.Services.MoloniClientService;
using Nop.Plugin.Misc.Moloni.Services.MoloniMiscServices;
using Nop.Plugin.Misc.Moloni.Services.SubscriptionService;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using System.Diagnostics;
using Customer = Nop.Plugin.Misc.Moloni.Models.Customer;
using NopCustomer = Nop.Core.Domain.Customers.Customer;

namespace Nop.Plugin.Misc.Moloni.Controllers
{
    public class ClientController : IConsumer<EntityUpdatedEvent<Address>>, 
                                    IConsumer<EntityInsertedEvent<Address>>, 
                                    IConsumer<CustomerRegisteredEvent>, 
                                    IConsumer<EntityUpdatedEvent<NopCustomer>>
    {

        #region Fields

        private readonly IMoloniClientService _moloniClientService;
        private readonly ICustomerService _customerService;
        private readonly ICountryService _countryService;
        private readonly IMoloniMiscServices _moloniMiscServices;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly ISubscriptionService _subscriptionService;

        #endregion

        #region Ctor

        /// <summary>
        /// Construtor da classe ClientController, inicializa os serviços necessários
        /// </summary>
        /// <param name="moloniClientService">Serviço de cliente Moloni</param>
        /// <param name="customerService">Serviço de clientes</param>
        /// <param name="countryService">Serviço de países</param>
        /// <param name="miscServices">Serviço de funcionalidades adicionais Moloni</param>
        public ClientController(IMoloniClientService moloniClientService,
                                ICustomerService customerService,
                                ICountryService countryService,
                                IMoloniMiscServices miscServices,
                                IStoreContext storeContext,
                                ISettingService settingService,
                                ISubscriptionService subscriptionService
            )
        {
            _moloniClientService = moloniClientService;
            _customerService = customerService;
            _countryService = countryService;
            _moloniMiscServices = miscServices;
            _storeContext = storeContext;
            _settingService = settingService;
            _subscriptionService = subscriptionService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Captura o evento de registo de novo cliente e sincroniza com o Moloni
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento de registo do cliente</param>
        public async Task HandleEventAsync(CustomerRegisteredEvent eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
                await SincronizarComMoloni(eventMessage.Customer, null);
        }

        /// <summary>
        /// Captura o evento de atualização dos dados do cliente e sincroniza com o Moloni
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento de atualização do cliente</param>
        public async Task HandleEventAsync(EntityUpdatedEvent<NopCustomer> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
            {
                var customer = eventMessage.Entity;
                var billingAddress = await _customerService.GetCustomerBillingAddressAsync(customer);

                await SincronizarComMoloni(customer, billingAddress);
            }
        }

        /// <summary>
        /// Captura o evento de inserção de nova morada e processa o evento
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento de inserção de morada</param>
        public async Task HandleEventAsync(EntityInsertedEvent<Address> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
                await ProcessAddressEventAsync(eventMessage.Entity);
        }

        /// <summary>
        /// Captura o evento de atualização de morada e processa o evento
        /// </summary>
        /// <param name="eventMessage">Mensagem do evento de atualização de morada</param>
        public async Task HandleEventAsync(EntityUpdatedEvent<Address> eventMessage)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
            var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

            if (subscriptionActive)
                await ProcessAddressEventAsync(eventMessage.Entity);
        }

        /// <summary>
        /// Processa o evento de inserção ou atualização de morada e sincroniza com o Moloni
        /// </summary>
        /// <param name="address">Morada a ser processada</param>
        private async Task ProcessAddressEventAsync(Address address)
        {
            var customer = await _customerService.GetCustomerByEmailAsync(address.Email);
            if (customer == null)
                return;

            await SincronizarComMoloni(customer, address);
        }

        /// <summary>
        /// Sincroniza os dados do cliente e da morada com o Moloni
        /// </summary>
        /// <param name="customer">Dados do cliente</param>
        /// <param name="address">Morada do cliente</param>
        private async Task SincronizarComMoloni(NopCustomer customer, Address address)
        {
            if (address == null)
            {
                address = new Address
                {
                    Address1 = "Morada Padrão",
                    ZipPostalCode = "0000-000",
                    City = "Cidade Padrão",
                    CountryId = 179
                };
            }

            if (!address.CountryId.HasValue)
            {
                Debug.WriteLine("CountryId é nulo.");
                return;
            }

            var tempCountry = await _countryService.GetCountryByIdAsync(address.CountryId.Value);
            if (tempCountry == null)
            {
                Debug.WriteLine("País não encontrado para o endereço.");
                return;
            }

            if (address.PhoneNumber != null)
                customer.Phone = address.PhoneNumber;

            var countryList = await _moloniMiscServices.GetCountries();
            var moloniCountry = countryList.FirstOrDefault(c => c.name.Equals(tempCountry.Name, StringComparison.OrdinalIgnoreCase));
            if (moloniCountry == null)
            {
                Debug.WriteLine("País não encontrado na API Moloni.");
                return;
            }

            var moloniClient = new Customer
            {
                name = $"{customer.FirstName} {customer.LastName}",
                vat = customer.VatNumber,
                email = customer.Email,
                address = address.Address1,
                zip_code = address.ZipPostalCode,
                city = address.City,
                country_id = moloniCountry.country_id,
                phone = customer.Phone,
                maturity_date_id = 0,
                payment_method_id = 0,
                payment_day = 0,
                credit_limit = 0,
                delivery_method_id = 0,
            };

            var existingClient = await _moloniClientService.GetClientByEmailAsync(customer.Email);
            if (existingClient == null)
            {
                await _moloniClientService.InsertNewClient(moloniClient);
            }
            else
            {
                moloniClient.customer_id = existingClient.customer_id;
                moloniClient.number = existingClient.number;

                await _moloniClientService.UpdateClient(moloniClient);
            }
        }

        #endregion
    }
}
