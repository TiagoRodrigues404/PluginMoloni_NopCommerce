namespace Nop.Plugin.Misc.Moloni.Services.SubscriptionService
{
    public interface ISubscriptionService
    {
        Task<bool> CheckSubscription(string email);
        Task<bool> SubscriptionValid(string email);
        void GenerateTokenAndSave(string email, bool valid);
    }
}
