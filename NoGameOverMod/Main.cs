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

    [UpdateBefore(typeof(CheckGameOverFromLife))]
    public class AutoRestart : CheckGameOverFromLife, IModSystem
    {
        private EntityQuery Patience;
        [ReadOnly]
        private EntityQuery OriginalKitchenStatus;
        private EntityQuery CurrentKitchenStatus;
        private EntityQuery MoneyQuery;

        protected override void Initialise()
        {
            base.Initialise();

            Patience = GetEntityQuery(typeof(CPatience));
            OriginalKitchenStatus = GetEntityQuery(ComponentType.ReadOnly<SKitchenStatus>());
            CurrentKitchenStatus = GetEntityQuery(ComponentType.ReadWrite<SKitchenStatus>());
            MoneyQuery = GetEntityQuery(ComponentType.ReadWrite<SMoney>());
        }

        protected override void OnUpdate()
        {
            SKitchenStatus singleton = OriginalKitchenStatus.GetSingleton<SKitchenStatus>();

            if (singleton.RemainingLives <= 0 && !Has<SPracticeMode>() && !RescuedByAppliance())
            {
                ShowDayRestart();
            }
        }

        private bool RescuedByAppliance()
        {
            SKitchenStatus singleton = OriginalKitchenStatus.GetSingleton<SKitchenStatus>();
            EntityQuery entityQuery = GetEntityQuery(new QueryHelper().All(typeof(CPreventGameOver)).None(typeof(CPreventGameOverConsumed)));

            if (!entityQuery.IsEmpty)
            {
                base.EntityManager.AddComponent<CPreventGameOverConsumed>(entityQuery.First());
                CurrentKitchenStatus.SetSingleton(new SKitchenStatus
                {
                    RemainingLives = 1,
                    TotalLives = singleton.TotalLives
                });
                NativeArray<Entity> nativeArray = Patience.ToEntityArray(Allocator.Temp);
                try
                {
                    foreach (Entity item in nativeArray)
                    {
                        if (Require<CPatience>(item, out CPatience comp))
                        {
                            comp.ResetTime();
                            Set(item, comp);
                        }
                    }
                }
                finally
                {
                    nativeArray.Dispose();
                }

                return true;
            }
            return false;
        }

        private void ShowDayRestart()
        {
            SKitchenStatus singleton = OriginalKitchenStatus.GetSingleton<SKitchenStatus>();

            if (!MoneyQuery.IsEmptyIgnoreFilter)
            {
                Entity moneyEntity = MoneyQuery.GetSingletonEntity();
                SMoney money = EntityManager.GetComponentData<SMoney>(moneyEntity);

                int newAmount = money.Amount / 2;
                money.Amount = newAmount;
                
                EntityManager.SetComponentData(moneyEntity, money);
            }

            base.World.Add(new COfferRestartDay
            {
                Reason = LossReason.Patience
            });

            CurrentKitchenStatus.SetSingleton(new SKitchenStatus
            {
                RemainingLives = singleton.TotalLives,
                TotalLives = singleton.TotalLives
            });
        }
    }
}