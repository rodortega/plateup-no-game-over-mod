using HarmonyLib;
using Kitchen;
using KitchenMods;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;

namespace NoGameOver
{
    public class Main : IModInitializer
    {
        public const string MOD_ID = "rod.PlateUp.noGameOver";
        public const string MOD_NAME = "No Game Over";
        public const string MOD_VERSION = "1.0.0";

        public void PostActivate(Mod mod)
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MOD_ID);
        }

        public void PreInject() { }
        public void PostInject() { }
    }

    public static class HalveMoneyTracker
    {
        public static bool ShouldHalveMoney = false;
    }

    [UpdateBefore(typeof(CheckGameOverFromLife))]
    public class AutoRestart : CheckGameOverFromLife, IModSystem
    {
        private EntityQuery Patience;
        [ReadOnly] private EntityQuery OriginalKitchenStatus;
        private EntityQuery CurrentKitchenStatus;

        protected override void Initialise()
        {
            base.Initialise();
            Patience = GetEntityQuery(typeof(CPatience));
            OriginalKitchenStatus = GetEntityQuery(ComponentType.ReadOnly<SKitchenStatus>());
            CurrentKitchenStatus = GetEntityQuery(ComponentType.ReadWrite<SKitchenStatus>());
        }

        protected override void OnUpdate()
        {
            SKitchenStatus status = OriginalKitchenStatus.GetSingleton<SKitchenStatus>();

            if (status.RemainingLives <= 0 && !Has<SPracticeMode>() && !RescuedByAppliance())
            {
                TriggerRestart();
            }
        }

        private bool RescuedByAppliance()
        {
            var status = OriginalKitchenStatus.GetSingleton<SKitchenStatus>();
            var rescueQuery = GetEntityQuery(new QueryHelper().All(typeof(CPreventGameOver)).None(typeof(CPreventGameOverConsumed)));

            if (!rescueQuery.IsEmpty)
            {
                EntityManager.AddComponent<CPreventGameOverConsumed>(rescueQuery.First());

                CurrentKitchenStatus.SetSingleton(new SKitchenStatus
                {
                    RemainingLives = 1,
                    TotalLives = status.TotalLives
                });

                NativeArray<Entity> patienceEntities = Patience.ToEntityArray(Allocator.Temp);
                try
                {
                    foreach (var entity in patienceEntities)
                    {
                        if (Require(entity, out CPatience patience))
                        {
                            patience.ResetTime();
                            Set(entity, patience);
                        }
                    }
                }
                finally
                {
                    patienceEntities.Dispose();
                }

                return true;
            }
            return false;
        }

        private void TriggerRestart()
        {
            HalveMoneyTracker.ShouldHalveMoney = true;

            base.World.Add(new COfferRestartDay
            {
                Reason = LossReason.Patience
            });

            var status = OriginalKitchenStatus.GetSingleton<SKitchenStatus>();
            CurrentKitchenStatus.SetSingleton(new SKitchenStatus
            {
                RemainingLives = status.TotalLives,
                TotalLives = status.TotalLives
            });
        }
    }

    public class HalveMoneyInPrepPhase : NightSystem, IModSystem
    {
        private EntityQuery MoneyQuery;

        protected override void Initialise()
        {
            base.Initialise();
            MoneyQuery = GetEntityQuery(ComponentType.ReadWrite<SMoney>());

            RequireSingletonForUpdate<SMoney>();
        }

        protected override void OnUpdate()
        {
            if (!HalveMoneyTracker.ShouldHalveMoney)
                return;


            if (MoneyQuery.IsEmptyIgnoreFilter)
                return;

            Entity moneyEntity = MoneyQuery.GetSingletonEntity();
            SMoney money = EntityManager.GetComponentData<SMoney>(moneyEntity);

            if (money.Amount <= 0)
                return;

            money.Amount /= 2;
            EntityManager.SetComponentData(moneyEntity, money);

            HalveMoneyTracker.ShouldHalveMoney = false;
        }
    }
}
