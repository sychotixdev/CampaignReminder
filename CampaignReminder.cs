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
///   3. General reminder notes configured for this zone
///   4. Build-specific notes from the currently loaded build file
///
/// Window position and size are controlled exclusively through Settings,
/// not by dragging, ensuring it stays out of the way during gameplay.
///
/// Build note files live at:  ConfigDirectory/Builds/build_*.json
/// </summary>
public class CampaignReminder : BaseSettingsPlugin<CampaignReminderSettings>
{
    // ── State ────────────────────────────────────────────────────────────────

    private AreaSettings _currentAreaSettings;   // null when zone is not in our list
    private string       _currentAreaName = "";  // always set, even when settings entry absent
    private BuildManager _buildManager;
    private CampaignReminderMenu _menu;

    // ── ImGui window flags — non-interactive overlay ──────────────────────────

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

        // Build manager lives in ConfigDirectory/Builds/
        _buildManager = new BuildManager(ConfigDirectory);

        // Restore the previously selected build (if it still exists)
        if (!string.IsNullOrEmpty(Settings.SelectedBuildFileName))
            _buildManager.Load(Settings.SelectedBuildFileName);

        _menu = new CampaignReminderMenu(Settings, _buildManager);
        AreaChange(GameController.Area.CurrentArea);
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        _currentAreaName     = area?.Name ?? "";
        _currentAreaSettings = FindAreaSettings(_currentAreaName);
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

        // ── Content flags ────────────────────────────────────────────────────
        var hasAdvice     = adviceText != null;
        var hasReward     = _currentAreaSettings != null
                            && !string.IsNullOrEmpty(_currentAreaSettings.LeagueReward);
        var hasNotes      = !string.IsNullOrEmpty(_currentAreaSettings?.Notes);
        var buildNotes    = _buildManager.GetNotes(_currentAreaName);
        var hasBuildNotes = !string.IsNullOrEmpty(buildNotes);

        if (!hasAdvice && !hasReward && !hasNotes && !hasBuildNotes)
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

        var anySoFar = false;

        // 1. Level advice
        if (hasAdvice)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, adviceColor);
            ImGui.TextWrapped(adviceText);
            ImGui.PopStyleColor();
            anySoFar = true;
        }

        // 2. League / Vaal Beacon reward
        if (hasReward)
        {
            if (anySoFar) ImGui.Separator();
            var rewardColor = _currentAreaSettings.HighlightLeagueReward
                ? new Vector4(0.35f, 0.95f, 0.35f, 1f)   // bright green
                : fgV;
            ImGui.PushStyleColor(ImGuiCol.Text, rewardColor);
            ImGui.TextWrapped($"League: {_currentAreaSettings.LeagueReward}");
            ImGui.PopStyleColor();
            anySoFar = true;
        }

        // 3. General notes
        if (hasNotes)
        {
            if (anySoFar) ImGui.Separator();
            ImGui.PushStyleColor(ImGuiCol.Text, fgV);
            ImGui.TextWrapped(_currentAreaSettings.Notes);
            ImGui.PopStyleColor();
            anySoFar = true;
        }

        // 4. Build-specific notes — shown last, visually distinct (light blue)
        if (hasBuildNotes)
        {
            if (anySoFar) ImGui.Separator();

            // Dim label so the notes text itself stands out
            var labelColor = new Vector4(0.55f, 0.75f, 1.00f, 0.70f);
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
            ImGui.TextWrapped($"[{_buildManager.CurrentBuildName}]");
            ImGui.PopStyleColor();

            var buildColor = new Vector4(0.75f, 0.90f, 1.00f, 1.00f);
            ImGui.PushStyleColor(ImGuiCol.Text, buildColor);
            ImGui.TextWrapped(buildNotes);
            ImGui.PopStyleColor();
        }

        ImGui.End();

        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Level-progress advice based on the XP gap formula used in PoE 2.
    ///
    ///   maxGap = 3 + floor(playerLevel / 16)
    ///   diff   = areaLevel - playerLevel
    ///
    ///   diff &lt;= 0            → overlevelled      → KILL LESS MONSTERS  (green)
    ///   diff &gt; maxGap + 1    → too underlevelled → GRIND LAST ZONE     (red)
    ///   diff == maxGap        → at XP boundary   → KILL MORE MONSTERS  (orange)
    ///   otherwise             → sweet spot        → (nothing shown)
    /// </summary>
    private static (string text, Vector4 color) GetLevelAdvice(int playerLevel, int areaLevel)
    {
        var maxGap = 3 + playerLevel / 16;
        var diff   = areaLevel - playerLevel;

        return diff switch
        {
            <= 0                     => ("  KILL LESS MONSTERS", new Vector4(0.35f, 0.95f, 0.35f, 1f)),
            _ when diff > maxGap + 1 => ("  GRIND LAST ZONE",    new Vector4(0.95f, 0.25f, 0.25f, 1f)),
            _ when diff >= maxGap    => ("  KILL MORE MONSTERS", new Vector4(1.00f, 0.60f, 0.10f, 1f)),
            _                        => (null, Vector4.Zero),
        };
    }

    /// <summary>
    /// Case-insensitive lookup of an AreaSettings by the in-game area name.
    /// Returns null when the zone is not in our configured list.
    /// </summary>
    private AreaSettings FindAreaSettings(string areaName)
    {
        if (string.IsNullOrEmpty(areaName)) return null;

        foreach (var act in Settings.Acts)
            foreach (var area in act.Areas)
                if (string.Equals(area.AreaName, areaName, StringComparison.OrdinalIgnoreCase))
                    return area;

        return null;
    }
}
