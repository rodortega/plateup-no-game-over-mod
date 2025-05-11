using Kitchen;
using KitchenMods;

namespace NoGameOver
{
    public class PatchController : GameSystemBase, IModSystem
    {
        private static PatchController _instance;
        protected override void Initialise()
        {
            base.Initialise();
            _instance = this;
        }

        protected override void OnUpdate()
        {
        }

        internal static bool CustomOfferRestart(out bool shouldRunOriginal)
        {
            if (_instance == null || !_instance.TryGetSingleton(out SKitchenStatus kitchenStatus))
            {
                shouldRunOriginal = false;
                return true;
            }

            shouldRunOriginal = false;

            _instance.World.Add(new COfferRestartDay
            {
                Reason = LossReason.Patience
            });

            _instance.Set(new SKitchenStatus
            {
                RemainingLives = kitchenStatus.TotalLives,
                TotalLives = kitchenStatus.TotalLives
            });

            return true;
        }
    }
}