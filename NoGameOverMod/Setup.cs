using Kitchen;
using KitchenMods;

namespace NoGameOver
{
    internal class Setup : RestaurantInitialisationSystem, IModSystem
    {
        protected override void Initialise()
        {
            base.Initialise();

            World.GetExistingSystem(typeof(CheckGameOverFromLife)).Enabled = false;
        }

        protected override void OnUpdate()
        {
        }
    }
}