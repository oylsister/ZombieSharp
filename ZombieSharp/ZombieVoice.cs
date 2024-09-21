namespace ZombieSharp
{
    public partial class ZombieSharp
    {
        public Dictionary<CCSPlayerController, double> ClientVoiceData = new Dictionary<CCSPlayerController, double>();
        public Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer> ClientMoanTimer = new Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer>();

        public void ZombieVoiceOnClientPutInServer(CCSPlayerController controller)
        {
            ClientVoiceData.Add(controller, 0f);
            ClientMoanTimer.Add(controller, null);
        }

        public void ZombieVoiceOnClientDisconnect(CCSPlayerController controller)
        {
            if(ClientVoiceData.ContainsKey(controller))
                ClientVoiceData.Remove(controller);

            if(ClientMoanTimer.ContainsKey(controller))
                ClientMoanTimer.Remove(controller);
        }

        public void ZombiePain(CCSPlayerController client)
        {
            EmitZombieSound(client, "zr.amb.zombie_pain");
        }

        public void ZombieMoan(CCSPlayerController client)
        {
            EmitZombieSound(client, "zr.amb.zombie_voice_idle");
        }

        public void ZombieScream(CCSPlayerController client)
        {
            EmitZombieSound(client, "zr.amb.scream");
        }

        public void ZombieDie(CCSPlayerController client)
        {
            EmitZombieSound(client, "zr.amb.zombie_die");
        }

        public void EmitZombieSound(CCSPlayerController client, string sound)
        {
            if(client == null || !client.PawnIsAlive)
            {
                return;
            }

            var pawn = client.PlayerPawn;

            EmitSound(pawn.Value, sound);
        }
    }
}