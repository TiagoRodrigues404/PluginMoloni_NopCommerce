using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.MoloniClientService;
using Nop.Plugin.Misc.Moloni.Services.MoloniInvoiceReceiptService;
using Nop.Plugin.Misc.Moloni.Services.MoloniMiscServices;
using Nop.Plugin.Misc.Moloni.Services.MoloniOrderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniPaymentService;
using Nop.Plugin.Misc.Moloni.Services.MoloniReturnsService;
using Nop.Plugin.Misc.Moloni.Services.SubscriptionService;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Orders;
using System.Diagnostics;
using Nop.Plugin.Misc.Moloni;

public class OrderController : IConsumer<OrderPlacedEvent>,
                               IConsumer<OrderPaidEvent>,
                               IConsumer<OrderRefundedEvent>
{
    #region Fields

    private readonly IMoloniOrderService _moloniOrderService;
    private readonly IMoloniClientService _moloniClientService;
    private readonly IMoloniMiscServices _moloniMiscServices;
    private readonly IMoloniPaymentService _moloniPaymentService;
    private readonly IMoloniInvoiceReceiptService _moloniInvoiceReceiptService;
    private readonly ICustomerService _customerService;
    private readonly IOrderService _orderServiceNopCommerce;
    private readonly IProductService _productService;
    private readonly IMoloniReturnsService _moloniReturnsService;
    private readonly IStoreContext _storeContext;
    private readonly ISettingService _settingService;
    private readonly ISubscriptionService _subscriptionService;

    #endregion

    #region Ctor

    public OrderController(IMoloniOrderService moloniOrderService,
                           IMoloniClientService moloniClientService,
                           IMoloniMiscServices moloniMiscServices,
                           IMoloniPaymentService moloniPaymentService,
                           IMoloniInvoiceReceiptService moloniInvoiceReceiptService,
                           ICustomerService customerService,
                           IOrderService orderServiceNopCommerce,
                           IProductService productService,
                           IMoloniReturnsService moloniReturnsService,
                           IStoreContext storeContext,
                           ISettingService settingService,
                           ISubscriptionService subscriptionService
        )
    {
        _moloniOrderService = moloniOrderService;
        _moloniClientService = moloniClientService;
        _moloniMiscServices = moloniMiscServices;
        _moloniPaymentService = moloniPaymentService;
        _moloniInvoiceReceiptService = moloniInvoiceReceiptService;
        _customerService = customerService; 
        _orderServiceNopCommerce = orderServiceNopCommerce;
        _productService = productService;
        _moloniReturnsService = moloniReturnsService;
        _storeContext = storeContext;
        _settingService = settingService;
        _subscriptionService = subscriptionService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Captura o evento de colocação de um pedido no NopCommerce e sincroniza o pedido com o sistema Moloni.
    /// Este método executa os seguintes passos:
    /// 1. Obtém os dados do cliente usando o ID do cliente associado ao pedido.
    /// 2. Busca os itens do pedido (produtos) e constrói uma lista de produtos com as suas referências e quantidades.
    /// 3. Determina a moeda usada no pedido e encontra o código ISO 4217 correspondente no Moloni.
    /// 4. Cria uma encomenda no Moloni utilizando o cliente, produtos, moeda e ID do pedido do NopCommerce.
    /// 5. Regista o resultado da criação da encomenda para verificação e debugging.
    /// </summary>
    /// <param name="eventMessage">Mensagem do evento que contém o pedido colocado.</param>
    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
        var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

        if (subscriptionActive)
        {
            var order = eventMessage.Order;

            // Obter dados do cliente
            var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
            var moloniClient = await _moloniClientService.GetClientByEmailAsync(customer.Email);

            // Obter itens do pedido (produtos)
            var orderItems = await _orderServiceNopCommerce.GetOrderItemsAsync(order.Id);
            var productsList = new List<ReceivedProducts>();

            foreach (var item in orderItems)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                productsList.Add(new ReceivedProducts
                {
                    Reference = product.Sku,
                    Quantity = item.Quantity
                });
            }

            // Obter moeda
            var currencyCode = order.CustomerCurrencyCode;
            var allCurrencies = await _moloniMiscServices.GetCurrencies();
            var currency = allCurrencies.FirstOrDefault(c => c.iso4217.ToLower().Equals(currencyCode.ToLower()));

            var result = await _moloniOrderService.GetItemsAndMakeOrder(moloniClient, productsList, currency.currency_id, order.Id);

            Debug.WriteLine($"Resultado a inserir encomenda: {result}");
        }  
    }

    /// <summary>
    /// Captura o evento de pagamento de um pedido e gera uma fatura no sistema Moloni para o pedido pago.
    /// Este método executa os seguintes passos:
    /// 1. Obtém os dados do cliente e verifica se o cliente existe no Moloni.
    /// 2. Busca os itens do pedido e cria uma lista de produtos com referências e quantidades.
    /// 3. Obtém o método de pagamento utilizado no pedido e verifica ou cria o método correspondente no Moloni.
    /// 4. Cria uma lista de pagamentos com detalhes do pagamento, data e valor.
    /// 5. Encontra a encomenda no Moloni utilizando o ID do pedido para garantir que a fatura seja associada à encomenda correta.
    /// 6. Cria uma fatura/recibo no Moloni com o cliente, produtos, pagamentos e documento associado, registrando o resultado para debugging.
    /// </summary>
    /// <param name="eventMessage">Mensagem do evento que contém o pedido pago.</param>
    public async Task HandleEventAsync(OrderPaidEvent eventMessage)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
        var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

        if (subscriptionActive)
        {
            var order = eventMessage.Order;

            // Obter dados do cliente
            var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
            var moloniClient = await _moloniClientService.GetClientByEmailAsync(customer.Email);

            // Obter itens do pedido (produtos)
            var orderItems = await _orderServiceNopCommerce.GetOrderItemsAsync(order.Id);
            var productsList = new List<ReceivedProducts>();

            foreach (var item in orderItems)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                productsList.Add(new ReceivedProducts
                {
                    Reference = product.Sku,
                    Quantity = item.Quantity
                });
            }

            // Obter o método de pagamento utilizado
            var paymentMethod = order.PaymentMethodSystemName;

            if (paymentMethod.StartsWith("Payments."))
                paymentMethod = paymentMethod.Substring(9);

            var payment = await _moloniPaymentService.GetPayment(paymentMethod);

            var paymentId = 0;
            if (payment == null)
                paymentId = await _moloniPaymentService.InsertPaymentMethod(paymentMethod);

            var paymentList = new List<Payment>
            {
                new Payment
                {
                    date = DateTime.Now,
                    payment_method_id = payment?.payment_method_id ?? paymentId,
                    value = (float)order.OrderTotal,
                    notes = ""
                }
            };

            var totalPaid = order.OrderTotal;

            // Obter encomenda
            var moloniOrder = await _moloniOrderService.GetPurchaseOrder(null, moloniClient.customer_id, null, null, $"PO{order.Id}");

            var associatedDocument = new AssociatedDocument
            {
                associated_id = moloniOrder.document_id,
                value = (float)moloniOrder.exchange_total_value
            };

            var result = await _moloniInvoiceReceiptService.GetItemsAndCreateInvoiceReceipt(moloniClient, productsList, paymentList, order.Id, associatedDocument, 0, 0);

            Debug.WriteLine($"Resultado a emitir fatura: {result}");
        }
    }

    /// <summary>
    /// Captura o evento de reembolso de um pedido e gera uma devolução no sistema Moloni.
    /// Este método executa os seguintes passos:
    /// 1. Obtém os dados do cliente e verifica a existência do cliente no Moloni.
    /// 2. Busca os produtos do pedido reembolsado e cria uma lista com as referências e quantidades.
    /// 3. Determina a moeda utilizada no pedido e identifica o código ISO 4217 correspondente no Moloni.
    /// 4. Obtém o método de pagamento utilizado no reembolso e cria um pagamento temporário com o valor reembolsado.
    /// 5. Localiza o recibo original no Moloni associado ao pedido e cria um documento associado para referenciar o reembolso.
    /// 6. Cria a devolução no Moloni, associando o pagamento e o documento original, e registra o resultado para debugging.
    /// </summary>
    /// <param name="eventMessage">Mensagem do evento que contém o pedido reembolsado.</param>
    public async Task HandleEventAsync(OrderRefundedEvent eventMessage)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var MoloniSettings = await _settingService.LoadSettingAsync<MoloniSettings>(storeScope);
        var subscriptionActive = await _subscriptionService.SubscriptionValid(MoloniSettings.StripeEmail);

        if (subscriptionActive)
        {
            var order = eventMessage.Order;
            var amount = eventMessage.Amount;

            // Obter dados do cliente
            var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
            var moloniClient = await _moloniClientService.GetClientByEmailAsync(customer.Email);

            // Obter listagem de produtos reembolsados
            var orderItems = await _orderServiceNopCommerce.GetOrderItemsAsync(order.Id);
            var productsList = new List<ReceivedProducts>();

            foreach (var item in orderItems)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                productsList.Add(new ReceivedProducts
                {
                    Reference = product.Sku,
                    Quantity = item.Quantity
                });
            };

            // Obter moeda
            var currencyCode = order.CustomerCurrencyCode;
            var allCurrencies = await _moloniMiscServices.GetCurrencies();
            var currency = allCurrencies.FirstOrDefault(c => c.iso4217.ToLower().Equals(currencyCode.ToLower()));

            // Obter o método de pagamento utilizado
            var paymentMethod = order.PaymentMethodSystemName;

            if (paymentMethod.StartsWith("Payments."))
                paymentMethod = paymentMethod.Substring(9);

            var payment = await _moloniPaymentService.GetPayment(paymentMethod);
            var tempPayment = new Payment
            {
                date = DateTime.Now,
                payment_method_id = payment.payment_method_id,
                value = (float)amount,
                notes = ""
            };

            // Obter o recibo
            var receipts = await _moloniInvoiceReceiptService.GetInvoiceReceipt(null, moloniClient.customer_id, null, null, $"IR{order.Id}");

            var associatedDocument = new AssociatedDocument
            {
                associated_id = receipts.document_id,
                value = (float)receipts.exchange_total_value
            };

            var result = await _moloniReturnsService.CreateNewReturn(tempPayment, associatedDocument, moloniClient, currency.currency_id);

            Debug.WriteLine($"Resultado a efetuar devoluçao: {result}");
        }
    }

    #endregion
}