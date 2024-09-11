using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static ZombieSharpAPI.IZombieSharpAPI;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public PlayerClassConfig PlayerClassDatas { get; private set; }

        public Dictionary<int, PlayerClientClass> ClientPlayerClass { get; set; } = new Dictionary<int, PlayerClientClass>();
        public Dictionary<int, CounterStrikeSharp.API.Modules.Timers.Timer> RegenTimer { get; set; } = new Dictionary<int, CounterStrikeSharp.API.Modules.Timers.Timer>();

        bool Human_Found = false;
        bool Zombie_Found = false;
        bool MotherZombie_Found = false;

        string Default_Human;
        string Default_Zombie;
        string Default_MotherZombie;

        public bool PlayerClassIntialize()
        {
            var configPath = Path.Combine(ModuleDirectory, "playerclasses.json");

            if (!File.Exists(configPath))
            {
                Logger.LogInformation("[Z:Sharp] Couldn't find playerclasses.json config file, Initial backup class!");
                PlayerClassDatas = new PlayerClassConfig();
                GetDefaultClass();
                return false;
            }

            Logger.LogInformation("[Z:Sharp] Loading playerclasses.json file.");
            PlayerClassDatas = JsonConvert.DeserializeObject<PlayerClassConfig>(File.ReadAllText(configPath));
            GetDefaultClass();
            return true;
        }

        private void GetDefaultClass()
        {
            foreach (var data in PlayerClassDatas.PlayerClasses)
            {
                if (data.Value.Default_Class)
                {
                    if (data.Value.Team == 1)
                    {
                        if (Human_Found)
                            continue;

                        else
                        {
                            Default_Human = data.Key;
                            Human_Found = true;
                        }
                    }
                    else
                    { 
                        if (Zombie_Found)
                            continue;

                        else
                        {
                            Default_Zombie = data.Key;
                            Zombie_Found = true;
                        }
                    }
                }

                if(data.Value.MotherZombie)
                {
                    if (MotherZombie_Found)
                        continue;

                    if (data.Value.Team != 0)
                        continue;

                    Default_MotherZombie = data.Key;
                    MotherZombie_Found = true;
                }
            }
        }

        public void PrecachePlayerModel(ResourceManifest mainfest)
        {
            foreach (PlayerClassData data in PlayerClassDatas.PlayerClasses.Values)
            {
                mainfest.AddResource(data.Model);
            }
        }

        public bool ApplyClientPlayerClass(CCSPlayerController client, string class_string, int team)
        {
            // stop regen timer first.
            RegenTimerStop(client);

            //Server.PrintToChatAll($"{client.PlayerName} Result = {PlayerClassDatas.PlayerClasses.ContainsKey(class_string)}");

            // if cannot find the class, then false so they can use the default value.
            if (!PlayerClassDatas.PlayerClasses.ContainsKey(class_string))
            {
                Logger.LogError($"Couldn't find {class_string} for {client.PlayerName}");
                return false;
            }

            var classData = PlayerClassDatas.PlayerClasses[class_string];

            // wrong team 
            if (classData.Team != team)
            {
                //Server.PrintToChatAll($"Try Apply class {class_string} for {client.PlayerName} but in the wrong team. TEAM= {team}, CTEAM= {classData.Team}");
                return false;
            }

            var clientPawn = client.PlayerPawn.Value;

            string applymodel;

            applymodel = classData.Model;

            AddTimer(0.1f, () =>
            {
                if(!string.IsNullOrEmpty(applymodel) && !string.IsNullOrWhiteSpace(applymodel))
                    clientPawn.SetModel(applymodel);
            });

            if (team == 0)
            {
                clientPawn.ArmorValue = 0;
                client.PawnHasHelmet = false;
            }

            clientPawn.Health = classData.Health;

            // This currently doesn't work properly need to find an altenative method that similar to m_flLaggedMovementValue
            clientPawn.VelocityModifier = classData.Speed / 250.0f;
            clientPawn.GravityScale = classData.Speed / 250.0f;

            if (classData.Regen_Interval > 0.0f && classData.Regen_Amount > 0)
            {
                RegenTimer[client.Slot] = AddTimer(classData.Regen_Interval, () =>
                {
                    if (!client.IsValid)
                    {
                        RegenTimer[client.Slot].Kill();
                        return;
                    }

                    if (!IsPlayerAlive(client))
                    {
                        RegenTimer[client.Slot].Kill();
                        return;
                    }

                    if (clientPawn.Health + classData.Regen_Amount > classData.Health)
                        clientPawn.Health = classData.Health;

                    else
                        clientPawn.Health += classData.Regen_Amount;
                },
                TimerFlags.REPEAT);
            }

            ClientPlayerClass[client.Slot].ActiveClass = class_string;
            return true;
        }

        public void RegenTimerStop(CCSPlayerController client)
        {
            if (!client.IsValid)
                return;

            // if can't find a player. then don't proceed.
            if (!RegenTimer.ContainsKey(client.Slot))
                return;

            if (RegenTimer[client.Slot] == null)
                return;

            RegenTimer[client.Slot].Kill();
        }

        public void PlayerClassMainMenu(CCSPlayerController client)
        {
            var mainmenu = new ChatMenu($" {Localizer["Class.MainMenu"]}");
            mainmenu.AddMenuOption(Localizer["Class.MainMenu.Zombie"], (client, option) => PlayerClassSelectMenu(client, 0));
            mainmenu.AddMenuOption(Localizer["Class.MainMenu.Human"], (client, option) => PlayerClassSelectMenu(client, 1));
            MenuManager.OpenChatMenu(client, mainmenu);
        }

        private void PlayerClassSelectMenu(CCSPlayerController client, int team)
        {
            string title;

            if (team == 0)
                title = $" {Localizer["Prefix"]} {Localizer["Class.ClassSelect.Zombie", PlayerClassDatas.PlayerClasses[ClientPlayerClass[client.Slot].ZombieClass].Name]}";

            else
                title = $" {Localizer["Prefix"]} {Localizer["Class.ClassSelect.Human", PlayerClassDatas.PlayerClasses[ClientPlayerClass[client.Slot].HumanClass].Name]}";

            var selectmenu = new ChatMenu(title);
            var menuhandle = (CCSPlayerController client, ChatMenuOption option) =>
            {
                if (option.Text == "Back")
                {
                    PlayerClassMainMenu(client);
                    return;
                }

                if (team == 0)
                {
                    ClientPlayerClass[client.Slot].ZombieClass = PlayerClassDatas.PlayerClasses.FirstOrDefault(x => x.Value.Name == option.Text).Key;
                }
                else
                {
                    ClientPlayerClass[client.Slot].HumanClass = PlayerClassDatas.PlayerClasses.FirstOrDefault(x => x.Value.Name == option.Text).Key;
                }

                var updateDB = new PlayerClassDB();

                updateDB.SteamID = client.AuthorizedSteamID.SteamId3;
                updateDB.ZClass = ClientPlayerClass[client.Slot].ZombieClass;
                updateDB.HClass = ClientPlayerClass[client.Slot].HumanClass;

                CreatePlayerSettings(updateDB).Wait();

                client.PrintToChat($" {Localizer["Prefix"]} {Localizer["Class.SelectSuccess"]}");
            };

            foreach (var playerclass in PlayerClassDatas.PlayerClasses)
            {
                if (playerclass.Value.Team == team)
                {
                    bool alreadyselected = playerclass.Key.Equals(ClientPlayerClass[client.Slot].HumanClass) || playerclass.Key.Equals(ClientPlayerClass[client.Slot].ZombieClass);
                    bool motherzombie = playerclass.Value.MotherZombie;
                    bool disable = !playerclass.Value.Enable;

                    selectmenu.AddMenuOption(playerclass.Value.Name, menuhandle, alreadyselected || motherzombie || disable);
                }
            }

            selectmenu.AddMenuOption("Back", menuhandle);

            MenuManager.OpenChatMenu(client, selectmenu);
        }

        private void PlayerClassesApplySpeedOnHurt(CCSPlayerController client)
        {
            if (client == null)
                return;

            if (!client.IsValid)
                return;

            if (!client.PawnIsAlive)
                return;

            if (!ClientPlayerClass.ContainsKey(client.Slot))
                return;

            var clientClass = ClientPlayerClass[client.Slot].ActiveClass;

            if (!PlayerClassDatas.PlayerClasses.ContainsKey(clientClass))
                return;

            var speed = PlayerClassDatas.PlayerClasses[ClientPlayerClass[client.Slot].ActiveClass].Speed;

            client.PlayerPawn.Value.VelocityModifier = speed / 250f;
        }
    }
}

public class PlayerClassConfig
{
    public Dictionary<string, PlayerClassData> PlayerClasses { get; set; } = new Dictionary<string, PlayerClassData>();

    public PlayerClassConfig()
    {
        PlayerClasses = new Dictionary<string, PlayerClassData>(StringComparer.OrdinalIgnoreCase)
        {
            { "human_default", new PlayerClassData("Human Config Default", "Default Class for human", true, true, 1, "", false, 100, 0.0f, 0, 0f, 250.0f, 0.0f, 3.0f, 1.0f) },
            { "zombie_default", new PlayerClassData("Zombie Config Default", "Default Class for zombie", true, true, 0, "", false, 8000, 10.0f, 100, 5f, 255.0f, 3.0f, 1.0f, 1.0f) },
            { "motherzombie", new PlayerClassData("Mother Zombie Config", "Mother Zombie Class", true, false, 0, "", true, 15000, 10.0f, 100, 5f, 255.0f, 3.0f, 1.0f, 1.0f) },
        };
    }
}

public class PlayerClientClass
{
    public string HumanClass { get; set; } = null;
    public string ZombieClass { get; set; } = null;
    public string ActiveClass { get; set; } = null;
}
