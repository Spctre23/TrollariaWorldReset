using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace WorldReset
{
    public class Handlers
    {
        public static void InitializeHandlers(TerrariaPlugin registrator)
        {
            GeneralHooks.ReloadEvent += OnReload;
        }

        public static void DisposeHandlers(TerrariaPlugin deregistrator)
        {
            GeneralHooks.ReloadEvent -= OnReload;
        }

        private static void OnReload(ReloadEventArgs args)
        {
            WorldReset.Config = Configuration.Reload();
        }
    }
}