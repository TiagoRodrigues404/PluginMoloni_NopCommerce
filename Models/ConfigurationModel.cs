using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Moloni.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Stripe Email")]
        public string StripeEmail { get; set; }

        [NopResourceDisplayName("Client ID")]
        public string ClientId { get; set; }

        [NopResourceDisplayName("Client Secret")]
        public string ClientSecret { get; set; }

        [NopResourceDisplayName("Username")]
        public string Username { get; set; }

        [NopResourceDisplayName("Password")]
        public string Password { get; set; }

        [NopResourceDisplayName("Redirect URL")]
        public string RedirectURI { get; set; }

        [NopResourceDisplayName("Company ID")]
        public string CompanyId { get; set; }

        [NopResourceDisplayName("Encryption Key")]
        public string _key { get; set; }

        [NopResourceDisplayName("Encryption IV")]
        public string _iv { get; set; }
    }
}