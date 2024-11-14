using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.Moloni;

/// <summary>
/// Represents a plugin settings
/// </summary>
public class MoloniSettings : ISettings
{
    public string StripeEmail { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string RedirectURI { get; set; }
    public string CompanyId { get; set; }
    public string _key { get; set; }
    public string _iv { get; set; }
}