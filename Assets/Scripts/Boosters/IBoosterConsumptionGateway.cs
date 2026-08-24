using System.Threading.Tasks;
using DefaultNamespace;

namespace Boosters
{
    public interface IBoosterConsumptionGateway
    {
        Task<(ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, PlayerInventoryData playerInventory)> ConsumeBooster(PlayFabAuthSession authSession, string boosterConsumptionIdempotencyKey, BoosterType boosterType);
    }
}
