using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using Terraria.ID;

namespace WorldReset
{
    [ApiVersion(2, 1)]
    public class WorldReset(Main game) : TerrariaPlugin(game)
    {
        public override string Name => "WorldReset";
        public override string Author => "Spctre";
        public override string Description => "Adds commands to reset and backup the world";
        public override Version Version => new(1, 0, 0);

        public static Configuration Config = Configuration.Reload();
        public static DatabaseManager DBManager = new(new SqliteConnection("Data Source=" + DatabaseManager.DatabasePath));

        public override void Initialize()
        {
            Handlers.InitializeHandlers(this);

            Commands.ChatCommands.Add(new Command("worldreset.worldreset", DeleteWorld, "worldreset"));
            Commands.ChatCommands.Add(new Command("worldreset.backup", BackupWorld, "backup"));
            Commands.ChatCommands.Add(new Command("worldreset.forcestop", ForceStop, "forcestop"));
            Commands.ChatCommands.Add(new Command("worldreset.reload", ReloadConfig, "wr-reload"));

            ServerApi.Hooks.WorldSave.Register(this, OnServerStarted);

            Configuration.Reload();

            TShock.Log.ConsoleInfo("======= WorldReset Plugin Initialized =======");
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                Handlers.DisposeHandlers(this);
            }
        }

        private void OnServerStarted(EventArgs args)
        {
            Main.GameMode = GameModeID.Master;

            TShock.Regions.AddRegion(Main.spawnTileX, Main.spawnTileY, 2, 1, "spawn", "Spctre", Main.worldID.ToString());
            TShock.Regions.GetRegionByName("spawn").DisableBuild = true;

            TShock.Warps.Add(272, 500, "locean");
            TShock.Warps.Add(8145, 463, "rocean");
            TShock.Warps.Add(4186, 2238, "hell");

            GenerateSpawnPlatform();
        }

        private void GenerateSpawnPlatform()
        {
            int spawnX = Main.spawnTileX;
            int spawnY = Main.spawnTileY;

            Main.tile[spawnX, spawnY].type = 226; // Lihzahrd Brick
            Main.tile[spawnX + 1, spawnY].type = 226;
            Main.tile[spawnX + 2, spawnY].type = 226;
        }

        public async void DeleteWorld(CommandArgs args)
        {
            await Task.Run(() => {
                BackupWorld();

                Thread.Sleep(3000);

                File.Delete(Main.worldPathName);
                Console.WriteLine("\nDeleting world...");

                Thread.Sleep(2000);

                TShock.Log.ConsoleInfo("Stopping server...");
                Process.GetCurrentProcess().Kill();
                return Task.CompletedTask;
            });          
        }

        private async void ForceStop(CommandArgs args)
        {
            TShock.Log.ConsoleInfo("Forcing server to stop...");

            Process? process = ProcessHelper.GetParentProcess(Process.GetCurrentProcess().Id);
            process.Kill();

            TShock.Utils.StopServer(true);

            long time = DateTimeOffset.Now.ToUnixTimeSeconds();
            await Task.Run(() => {
                while (TShock.ShuttingDown)
                {
                    if (DateTimeOffset.Now.ToUnixTimeSeconds() - time == Config.secondsUntilKillProcess)
                    {
                        TShock.Log.ConsoleInfo("Killing server process...");
                        Process.GetCurrentProcess().Kill();
                        break;
                    }
                }

                return Task.CompletedTask;
            });
        }

        private void BackupWorld(CommandArgs args)
        {
            BackupWorld();
        }

        private void BackupWorld()
        {
            TShock.Log.ConsoleInfo("Backing up world...");
            File.Copy(Main.worldPathName, $"{Main.worldPathName} {DateTime.Now.Month}-{DateTime.Now.Day}-{DateTime.Now.Year} {DateTime.Now.Hour}.{DateTime.Now.Minute}.wld", true);
        }

        private void ReloadConfig(CommandArgs args)
        {
            Configuration.Reload();
            args.Player.SendMessage("WorldReset configuration has been reloaded.", 0, 255, 0);
        }
    }
}

