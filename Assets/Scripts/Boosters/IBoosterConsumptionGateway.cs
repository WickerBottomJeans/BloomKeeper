using System.Threading.Tasks;
using DefaultNamespace;

namespace Boosters
{
    public interface IBoosterConsumptionGateway
    {
        Task<(ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, BoosterInventoryData inventory)> ConsumeBooster(PlayFabAuthSession authSession, string boosterConsumptionIdempotencyKey, BoosterType boosterType);
    }
}
