using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace ZombieSharp.Plugin;

public class RoundEnd
{
    private static ILogger<ZombieSharp>? _logger;
    private static ZombieSharp? _core;

    public RoundEnd(ZombieSharp core, ILogger<ZombieSharp> logger)
    {
        _core = core;
        _logger = logger;
    }

    public static CounterStrikeSharp.API.Modules.Timers.Timer? TerminateRoundTimer;
    public static bool RoundEnded = false;

    public static void RoundEndKillTimer()
    {
        if(TerminateRoundTimer != null)
        {
            TerminateRoundTimer.Kill();
            TerminateRoundTimer = null;
        }
    }

    public static void RoundEndOnRoundStart()
    {
        RoundEndKillTimer();
        RoundEnded = false;
    }

    public static void RoundEndOnRoundEnd()
    {
        RoundEndKillTimer();
        RoundEnded = true;
    }

    public static void RoundEndOnRoundFreezeEnd()
    {
        RoundEndKillTimer();

        var convar = ConVar.Find("mp_roundtime");

        if(convar == null)
        {
            _logger?.LogError("[RoundEndOnRoundFreezeEnd] Cannot find mp_roundtime convar!");
            return;
        }

        var timer = convar.GetPrimitiveValue<float>();

        _logger?.LogInformation($"[RoundEndOnRoundFreezeEnd] Round time: {timer}");

        if(GameSettings.Settings == null)
        {
            _logger?.LogError("[RoundEndOnRoundFreezeEnd] Game Settings is null! Cannot terminate round!");
            return;
        }

        TerminateRoundTimer = _core?.AddTimer(timer * 60, () => {

            CsTeam winner = CsTeam.None;

            if(GameSettings.Settings.TimeoutWinner == 0)
                winner = CsTeam.Terrorist;

            else if(GameSettings.Settings.TimeoutWinner == 1)
                winner = CsTeam.CounterTerrorist;

            TerminateRound(winner);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    public static void CheckGameStatus()
    {
        var ct = Utilities.GetPlayers().Where(player => player.TeamNum == 3 && player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE).Count();
        var t = Utilities.GetPlayers().Where(player => player.TeamNum == 2 && player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE).Count();

        // Server.PrintToChatAll($"CT = {ct} | T = {t}");

        if(ct == 0 && t > 0)
            TerminateRound(CsTeam.Terrorist);

        else if(t == 0 && ct > 0)
            TerminateRound(CsTeam.CounterTerrorist);

        else if(t == 0 && ct == 0)
            TerminateRound();
    }

    public static void TerminateRound(CsTeam team = CsTeam.None, float duration = 5f)
    {
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;

        if(gameRules == null)
        {
            _logger?.LogError("[TerminateRound] Gamerules is invalid cannot terminated round!");
            return;
        }

        RoundEndReason reason;

        if(team == CsTeam.Terrorist)
            reason = RoundEndReason.TerroristsWin;

        else if(team == CsTeam.CounterTerrorist)
            reason = RoundEndReason.CTsWin;

        else
            reason = RoundEndReason.RoundDraw;

        gameRules.TerminateRound(duration, reason);

        if(team != CsTeam.None && team != CsTeam.Spectator)
            UpdateTeamScore(team);
    }

    public static void UpdateTeamScore(CsTeam team, int score = 1)
    {
        var teamManagers = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

        foreach (var teamManager in teamManagers)
        {
            if ((int)team == teamManager.TeamNum)
            {
                teamManager.Score += score;
                Utilities.SetStateChanged(teamManager, "CTeam", "m_iScore");
            }
        }
    }
}