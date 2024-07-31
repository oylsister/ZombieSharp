namespace ZombieSharp
{
    public class ZombieVoice
    {
        ZombieSharp _plugin;

        public ZombieVoice(ZombieSharp plugin)
        {
            _plugin = plugin;
        }

        public Dictionary<CCSPlayerController, bool> _ZombiePainList = new Dictionary<CCSPlayerController, bool>();
        public Dictionary<CCSPlayerController, bool> _ZombieMoanList = new Dictionary<CCSPlayerController, bool>();

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

            _plugin.EmitSound(pawn.Value, sound);
        }
    }
}
