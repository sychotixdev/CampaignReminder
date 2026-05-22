using System;
using System.Numerics;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ImGuiNET;

namespace CampaignReminder;

/// <summary>
/// Campaign Reminder plugin for ExileCore2 / Path of Exile 2.
///
/// Displays a small locked overlay showing:
///   1. A level-advice line based on player level vs. area level
///   2. The current zone's Vaal Beacon reward (if enabled)
///   3. Any free-form notes configured for this zone
///
/// Window position and size are controlled exclusively through Settings,
/// not by dragging, ensuring it stays out of the way during gameplay.
/// </summary>
public class CampaignReminder : BaseSettingsPlugin<CampaignReminderSettings>
{
    // ── State ────────────────────────────────────────────────────────────────

    private AreaSettings _currentAreaSettings;   // null when zone not in our list
    private CampaignReminderMenu _menu;

    // ── ImGui window flags — non-interactive, always-on-top overlay ──────────

    private const ImGuiWindowFlags OverlayFlags =
          ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoScrollWithMouse
        | ImGuiWindowFlags.AlwaysAutoResize
        | ImGuiWindowFlags.NoInputs           // mouse passes through
        | ImGuiWindowFlags.NoCollapse
        | ImGuiWindowFlags.NoBringToFrontOnFocus
        | ImGuiWindowFlags.NoFocusOnAppearing;

    // ── Plugin lifecycle ─────────────────────────────────────────────────────

    public override bool Initialise()
    {
        // Populate default zone data on first run (or after a settings reset)
        if (Settings.Acts == null || Settings.Acts.Count == 0)
            Settings.Acts = CampaignData.GetDefaultActs();

        Settings.ResetToDefault.OnPressed += () =>
        {
            Settings.Acts = CampaignData.GetDefaultActs();
        };

        _menu = new CampaignReminderMenu(Settings);
        AreaChange(GameController.Area.CurrentArea);
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        _currentAreaSettings = FindAreaSettings(area?.Name);
    }

    public override void DrawSettings()
    {
        base.DrawSettings();
        _menu?.DrawConfiguration();
    }

    // ── Render ───────────────────────────────────────────────────────────────

    public override void Render()
    {
        if (!Settings.Enable)
            return;

        var area = GameController.Area.CurrentArea;
        if (area == null || GameController.IsLoading || !GameController.InGame)
            return;

        if (area.IsTown && !Settings.ShowInTown)
            return;

        if (area.IsHideout && !Settings.ShowInHideout)
            return;

        // ── Level advice ─────────────────────────────────────────────────────
        string  adviceText  = null;
        Vector4 adviceColor = Vector4.One;

        try
        {
            var player = GameController.IngameState.Data.LocalPlayer?.GetComponent<Player>();
            if (player != null)
                (adviceText, adviceColor) = GetLevelAdvice(player.Level, area.Area.AreaLevel);
        }
        catch { /* component may be unavailable between transitions */ }

        // ── Decide whether anything is worth showing ─────────────────────────
        var hasAdvice = adviceText != null;
        var hasReward = !string.IsNullOrEmpty(_currentAreaSettings.LeagueReward);
        var hasNotes  = !string.IsNullOrEmpty(_currentAreaSettings?.Notes);

        if (!hasAdvice && !hasReward && !hasNotes)
            return;

        // ── Style setup ──────────────────────────────────────────────────────
        var bg  = Settings.BackgroundColor.Value;
        var bgV = new Vector4(bg.R / 255f, bg.G / 255f, bg.B / 255f, bg.A / 255f);

        var fg  = Settings.DefaultTextColor.Value;
        var fgV = new Vector4(fg.R / 255f, fg.G / 255f, fg.B / 255f, fg.A / 255f);

        var pad = (float)Settings.BackgroundPadding.Value;

        ImGui.PushStyleColor(ImGuiCol.WindowBg, bgV);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(pad, pad));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6f, 5f));

        ImGui.SetNextWindowPos(
            new Vector2(Settings.WindowX, Settings.WindowY),
            ImGuiCond.Always);
        ImGui.SetNextWindowSize(
            new Vector2(Settings.WindowWidth, 0),
            ImGuiCond.Always);

        // ── Window content ───────────────────────────────────────────────────
        ImGui.Begin("##CampaignReminder", OverlayFlags);

        // 1. Level advice line (only shown when there is one)
        if (hasAdvice)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, adviceColor);
            ImGui.TextWrapped(adviceText);
            ImGui.PopStyleColor();
        }

        // 2. League / Vaal Beacon reward
        if (hasReward)
        {
            if (hasAdvice) ImGui.Separator();

            Vector4 rewardColor = fgV;
            if (_currentAreaSettings.HighlightLeagueReward)
            {
                rewardColor = new Vector4(0.35f, 0.95f, 0.35f, 1f); // bright green for highlighted zones
            }

            ImGui.PushStyleColor(ImGuiCol.Text, rewardColor);
            ImGui.TextWrapped($"League: {_currentAreaSettings!.LeagueReward}");
            ImGui.PopStyleColor();
        }

        // 3. Free-form notes
        if (hasNotes)
        {
            if (hasAdvice || hasReward) ImGui.Separator();
            ImGui.PushStyleColor(ImGuiCol.Text, fgV);
            ImGui.TextWrapped(_currentAreaSettings!.Notes);
            ImGui.PopStyleColor();
        }

        ImGui.End();

        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Determine the level-progress advice line to display.
    ///
    /// Threshold = 3 + floor(playerLevel / 16)
    /// This is the maximum level gap that still yields full XP in the zone.
    ///
    ///   diff = areaLevel - playerLevel
    ///
    ///   diff &lt;= 0            → overlevelled     → KILL LESS MONSTERS  (green)
    ///   diff == maxGap        → at XP boundary  → KILL MORE MONSTERS  (orange)
    ///   diff &gt; maxGap + 1    → too underlevelled → GRIND LAST ZONE     (red)
    ///   otherwise             → sweet spot       → (no line shown)
    /// </summary>
    private static (string text, Vector4 color) GetLevelAdvice(int playerLevel, int areaLevel)
    {
        var maxGap = 3 + playerLevel / 16;
        var diff   = areaLevel - playerLevel;

        return diff switch
        {
            <= 0                         => ("  KILL LESS MONSTERS",  new Vector4(0.35f, 0.95f, 0.35f, 1f)),
            _ when diff > maxGap + 1     => ("  GRIND LAST ZONE",     new Vector4(0.95f, 0.25f, 0.25f, 1f)),
            _ when diff >= maxGap -1    => ("  KILL MORE MONSTERS",  new Vector4(1.00f, 0.60f, 0.10f, 1f)),
            _                            => (null, Vector4.Zero),
        };
    }

    /// <summary>
    /// Case-insensitive lookup of an AreaSettings by the in-game area name.
    /// Returns null when the zone is not in our configured list.
    /// </summary>
    private AreaSettings FindAreaSettings(string areaName)
    {
        if (string.IsNullOrEmpty(areaName))
            return null;

        foreach (var act in Settings.Acts)
            foreach (var area in act.Areas)
                if (string.Equals(area.AreaName, areaName, StringComparison.OrdinalIgnoreCase))
                    return area;

        return null;
    }
}
