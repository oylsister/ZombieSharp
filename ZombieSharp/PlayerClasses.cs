using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public PlayerClassConfig PlayerClassDatas { get; private set; }

        public Dictionary<int, PlayerClientClass> ClientPlayerClass { get; set; } = new Dictionary<int, PlayerClientClass>();
        public Dictionary<int, CounterStrikeSharp.API.Modules.Timers.Timer> RegenTimer { get; set; } = new Dictionary<int, CounterStrikeSharp.API.Modules.Timers.Timer>();

        public bool PlayerClassIntialize()
        {
            var configPath = Path.Combine(ModuleDirectory, "playerclasses.json");

            if (!File.Exists(configPath))
            {
                Logger.LogInformation("[Z:Sharp] Couldn't find playerclasses.json config file, Initial backup class!");
                PlayerClassDatas = new PlayerClassConfig();
                return false;
            }

            Logger.LogInformation("[Z:Sharp] Loading playerclasses.json file.");
            PlayerClassDatas = JsonConvert.DeserializeObject<PlayerClassConfig>(File.ReadAllText(configPath));
            return true;
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

            // if cannot find the class, then false so they can use the default value.
            if (!PlayerClassDatas.PlayerClasses.ContainsKey(class_string))
            {
                //Server.PrintToChatAll($"Couldn't find {class_string} for {client.PlayerName}");
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

            if (string.IsNullOrEmpty(classData.Model))
            {
                if (team == 0)
                    applymodel = @"characters\models\tm_phoenix\tm_phoenix.vmdl";

                else
                    applymodel = @"characters\models\ctm_sas\ctm_sas.vmdl";
            }
            else
            {
                applymodel = classData.Model;
            }

            AddTimer(0.1f, () =>
            {
                clientPawn.SetModel(applymodel);
            });

            if (team == 0)
            {
                clientPawn.ArmorValue = 0;
                client.PawnHasHelmet = false;
            }

            clientPawn.Health = classData.Health;

            // This currently doesn't work properly need to find an altenative method that similar to m_flLaggedMovementValue
            // clientPawn.VelocityModifier = classData.Speed / 250.0f;
            // clientPawn.GravityScale = classData.Speed / 250.0f;

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
            var mainmenu = new ChatMenu($"{ChatColors.DarkBlue}[Z:Sharp] Player Class Main Menu");
            mainmenu.AddMenuOption("Zombie Class", (client, option) => PlayerClassSelectMenu(client, 0));
            mainmenu.AddMenuOption("Human Class", (client, option) => PlayerClassSelectMenu(client, 1));
            MenuManager.OpenChatMenu(client, mainmenu);
        }

        private void PlayerClassSelectMenu(CCSPlayerController client, int team)
        {
            string title;

            if (team == 0)
                title = $" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Zombie Class Selection (Current: {ChatColors.Green}{PlayerClassDatas.PlayerClasses[ClientPlayerClass[client.Slot].ZombieClass].Name}{ChatColors.Default})";

            else
                title = $" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} Human Class Selection (Current: {ChatColors.Green}{PlayerClassDatas.PlayerClasses[ClientPlayerClass[client.Slot].HumanClass].Name}{ChatColors.Default})";

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

                client.PrintToChat($" {ChatColors.Green}[Z:Sharp]{ChatColors.Default} You have selected a new class, the class you have selected will be applied in the next spawn!");
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
    }
}

public class PlayerClassConfig
{
    public Dictionary<string, PlayerClassData> PlayerClasses { get; set; } = new Dictionary<string, PlayerClassData>();

    public PlayerClassConfig()
    {
        PlayerClasses = new Dictionary<string, PlayerClassData>(StringComparer.OrdinalIgnoreCase)
        {
            { "human_default", new PlayerClassData("Human Config Default", "Default Class for human", true, 1, "", false, 100, 0.0f, 0, 250.0f, 0.0f, 3.0f, 1.0f) },
            { "zombie_default", new PlayerClassData("Zombie Config Default", "Default Class for zombie", true, 0, "", false, 8000, 10.0f, 100, 255.0f, 3.0f, 1.0f, 1.0f) },
            { "motherzombie", new PlayerClassData("Mother Zombie Config", "Mother Zombie Class", true, 0, "", false, 15000, 10.0f, 100, 255.0f, 3.0f, 1.0f, 1.0f) },
        };
    }
}

public class PlayerClientClass
{
    public string HumanClass { get; set; } = null;
    public string ZombieClass { get; set; } = null;
    public string ActiveClass { get; set; } = null;
}

public class PlayerClassData
{
    public PlayerClassData(string name, string desc, bool enable, int team, string model, bool motherzombie, int hp, float regen_interval, int regen_amount, float speed, float knockback, float jump_height, float jump_distance)
    {
        Name = name;
        Description = desc;
        Enable = enable;
        Team = team;
        Model = model;
        MotherZombie = motherzombie;
        Health = hp;
        Regen_Interval = regen_interval;
        Regen_Amount = regen_amount;
        Speed = speed;
        Knockback = knockback;
        Jump_Height = jump_height;
        Jump_Distance = jump_distance;
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public bool Enable { get; set; }
    public int Team { get; set; }
    public string Model { get; set; }

    public bool MotherZombie { get; set; }

    public int Health { get; set; }
    public float Regen_Interval { get; set; }
    public int Regen_Amount { get; set; }

    public float Speed { get; set; }
    public float Knockback { get; set; }
    public float Jump_Height { get; set; }
    public float Jump_Distance { get; set; }
}
