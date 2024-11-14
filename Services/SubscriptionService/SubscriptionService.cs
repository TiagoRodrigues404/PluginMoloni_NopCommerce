using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Nop.Core.Domain.Payments;
using Nop.Services.Configuration;
using Nop.Services.Messages;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Stripe;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using Nop.Services.Common;

namespace Nop.Plugin.Misc.Moloni.Services.SubscriptionService
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly INotificationService _notificationService;
        private readonly IPluginService _pluginService;
        private readonly PaymentSettings _paymentSettings;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly ISettingService _settingService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MoloniDefaults paymentDefaults;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

        public SubscriptionService(INotificationService notificationService,
                                   IPluginService pluginService,
                                   PaymentSettings paymentSettings,
                                   IPaymentPluginManager paymentPluginManager,
                                   ISettingService settingService,
                                   IHttpContextAccessor httpContextAccessor,
                                   IMemoryCache cache)
        {
            _notificationService = notificationService;
            _pluginService = pluginService;
            _paymentSettings = paymentSettings;
            _paymentPluginManager = paymentPluginManager;
            _settingService = settingService;
            _httpContextAccessor = httpContextAccessor;
            paymentDefaults = new MoloniDefaults();
            _cache = cache;
        }

        public async Task<bool> CheckSubscription(string email)
        {
            try
            {
                var isValidSubscription = await SubscriptionValid(email);

                if (isValidSubscription)
                {
                    _notificationService.SuccessNotification("Subscrição verificada com sucesso. O produto correto foi identificado e o plugin foi ativado!");
                    return isValidSubscription;
                }
                else
                {
                    _notificationService.ErrorNotification("Subscrição inválida ou produto correto não encontrado.");
                    return isValidSubscription;
                }
            }
            catch (StripeException ex)
            {
                _notificationService.ErrorNotification($"Erro ao verificar a subscrição: {ex.StripeError.Message}");
                return false;
            }
        }

        public async Task<bool> SubscriptionValid(string email)
        {
            if (_cache.TryGetValue(email, out string token))
                if (IsTokenValid(token, out bool isSubscriptionValid, out DateTime lastChecked))
                    if (DateTime.UtcNow - lastChecked < _cacheDuration)
                        return isSubscriptionValid;

            // Faz a chamada à Stripe se não houver token ou o token tiver expirado
            var defaults = new MoloniDefaults();
            StripeConfiguration.ApiKey = defaults.StripeApiKey;
            var customerService = new CustomerService();
            var options = new CustomerListOptions
            {
                Email = email,
                Limit = 1
            };
            var customers = await customerService.ListAsync(options);

            if (customers.Any())
            {
                var customer = customers.First();
                var subscriptionService = new Stripe.SubscriptionService();
                var subscriptions = await subscriptionService.ListAsync(new SubscriptionListOptions
                {
                    Customer = customer.Id
                });
                var activeSubscription = subscriptions.FirstOrDefault(s => s.Status == "active");

                if (activeSubscription != null)
                {
                    var correctProductId = defaults.StripeProduct;
                    var hasCorrectProduct = activeSubscription.Items.Data.Any(item => item.Price.ProductId == correctProductId);

                    if (hasCorrectProduct)
                    {
                        var newToken = GenerateSubscriptionToken(true, DateTime.UtcNow);
                        _cache.Set(email, newToken, _cacheDuration);
                        return true;
                    }
                }
            }

            var invalidToken = GenerateSubscriptionToken(false, DateTime.UtcNow);
            _cache.Set(email, invalidToken, _cacheDuration);

            return false;
        }

        public void GenerateTokenAndSave(string email, bool valid)
        {
            var token = GenerateSubscriptionToken(valid, DateTime.UtcNow);
            _cache.Set(email, token, _cacheDuration);
        }

        private string GenerateSubscriptionToken(bool isValid, DateTime lastChecked)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(paymentDefaults.JWTSecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim("subscriptionValid", isValid.ToString()),
            new Claim("lastChecked", lastChecked.ToString("o"))
        };

            var token = new JwtSecurityToken(
                issuer: $"{request.Scheme}://{request.Host}",
                audience: $"{request.Scheme}://{request.Host}",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6),
                signingCredentials: credentials);

            var generatedToken = new JwtSecurityTokenHandler().WriteToken(token);

            return generatedToken;
        }

        private bool IsTokenValid(string token, out bool isSubscriptionValid, out DateTime lastChecked)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(paymentDefaults.JWTSecretKey));

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = $"{request.Scheme}://{request.Host}",
                    ValidAudience = $"{request.Scheme}://{request.Host}",
                    IssuerSigningKey = securityKey,
                    ValidateLifetime = false
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                isSubscriptionValid = bool.Parse(jwtToken.Claims.First(x => x.Type == "subscriptionValid").Value);
                lastChecked = DateTime.Parse(jwtToken.Claims.First(x => x.Type == "lastChecked").Value);

                return true;
            }
            catch (Exception ex)
            {
                isSubscriptionValid = false;
                lastChecked = DateTime.MinValue;
                return false;
            }
        }
    }
}
