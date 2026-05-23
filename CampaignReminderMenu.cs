using System;
using System.Numerics;
using ImGuiNET;

namespace CampaignReminder;

/// <summary>
/// Draws all custom settings UI inside DrawSettings():
///   • Build management section  (select / create build files)
///   • Campaign zone section     (collapsible acts, reward toggles, notes fields)
///
/// Each area row has two note inputs:
///   1. General notes  — always visible, saved in main settings JSON
///   2. Build notes    — only visible when a build is loaded, saved to build JSON
/// </summary>
public class CampaignReminderMenu(CampaignReminderSettings settings, BuildManager buildManager)
{
    // ── Colours ───────────────────────────────────────────────────────────────

    private static readonly Vector4 SectionHeader  = new(0.95f, 0.80f, 0.30f, 1f);   // gold
    private static readonly Vector4 ZoneColor      = new(0.85f, 0.85f, 0.85f, 1f);   // light grey
    private static readonly Vector4 RewardActive   = new(0.40f, 0.90f, 0.45f, 1f);   // green
    private static readonly Vector4 RewardDimmed   = new(0.50f, 0.50f, 0.50f, 0.55f);
    private static readonly Vector4 NoBeaconColor  = new(0.45f, 0.45f, 0.45f, 0.70f);
    private static readonly Vector4 BuildLabelColor = new(0.55f, 0.78f, 1.00f, 1f);  // soft blue
    private static readonly Vector4 BuildHintColor  = new(0.45f, 0.45f, 0.45f, 0.80f);

    // ── New-build creation state ──────────────────────────────────────────────

    private string _newBuildName = "";

    // ── Entry point ───────────────────────────────────────────────────────────

    public void DrawConfiguration()
    {
        DrawBuildManagementSection();
        ImGui.Spacing();
        ImGui.Spacing();
        DrawZoneConfigSection();
    }

    // ── Build management ─────────────────────────────────────────────────────

    private void DrawBuildManagementSection()
    {
        ImGui.TextColored(SectionHeader, "Build Notes");
        ImGui.Separator();
        ImGui.TextWrapped(
            "Select a build to attach per-zone build notes. " +
            "Build files are stored as JSON in the plugin's Builds folder.");
        ImGui.Spacing();

        // ── Active build dropdown ─────────────────────────────────────────────
        var hasBuilds = buildManager.AvailableFiles.Length > 0;

        ImGui.Text("Active build:");
        ImGui.SameLine();

        if (!hasBuilds)
        {
            ImGui.TextColored(NoBeaconColor, "(no build files found — create one below)");
        }
        else
        {
            var preview = buildManager.HasBuild
                ? BuildManager.DisplayName(buildManager.CurrentFileName)
                : "-- none selected --";

            ImGui.SetNextItemWidth(220f);
            if (ImGui.BeginCombo("##buildCombo", preview))
            {
                // "none" option
                var noneSelected = !buildManager.HasBuild;
                if (ImGui.Selectable("-- none --", noneSelected))
                {
                    settings.SelectedBuildFileName = "";
                    // Reload manager with empty name to clear
                    buildManager.Load("");
                }
                if (noneSelected) ImGui.SetItemDefaultFocus();

                ImGui.Separator();

                foreach (var file in buildManager.AvailableFiles)
                {
                    var isSelected = file == buildManager.CurrentFileName;
                    if (ImGui.Selectable(BuildManager.DisplayName(file), isSelected))
                    {
                        buildManager.Load(file);
                        settings.SelectedBuildFileName = file;
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (ImGui.Button("Refresh##buildList"))
                buildManager.Refresh();
        }

        // Status line
        if (buildManager.HasBuild)
        {
            ImGui.TextColored(BuildLabelColor,
                $"  Loaded: {buildManager.CurrentBuildName}  ({buildManager.CurrentFileName})");
        }

        ImGui.Spacing();

        // ── Create new build ──────────────────────────────────────────────────
        ImGui.TextColored(SectionHeader, "Create new build");
        ImGui.Separator();

        ImGui.Text("Name:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f);
        ImGui.InputText("##newBuildName", ref _newBuildName, 128);

        ImGui.SameLine();
        var canCreate = !string.IsNullOrWhiteSpace(_newBuildName);

        if (!canCreate) ImGui.BeginDisabled();
        if (ImGui.Button("Create & Select"))
        {
            var fileName = buildManager.CreateNew(_newBuildName);
            if (fileName != null)
            {
                settings.SelectedBuildFileName = fileName;
                _newBuildName = "";
            }
        }
        if (!canCreate) ImGui.EndDisabled();

        ImGui.TextColored(BuildHintColor,
            "Files are saved to the Builds/ subfolder of the plugin's config directory.");
    }

    // ── Zone configuration ────────────────────────────────────────────────────

    private void DrawZoneConfigSection()
    {
        ImGui.TextColored(SectionHeader, "Campaign Zone Configuration");
        ImGui.Separator();
        ImGui.TextWrapped(
            "Toggle the reward highlight, set general notes, and — when a build is loaded — " +
            "add build-specific notes per zone.");
        ImGui.Spacing();

        for (var actIdx = 0; actIdx < settings.Acts.Count; actIdx++)
        {
            var act = settings.Acts[actIdx];
            ImGui.PushID(actIdx);

            if (ImGui.CollapsingHeader($"{act.ActName}  ({act.Areas.Count} zones)##act"))
            {
                ImGui.Indent(12f);
                ImGui.Spacing();

                for (var areaIdx = 0; areaIdx < act.Areas.Count; areaIdx++)
                {
                    var area = act.Areas[areaIdx];
                    ImGui.PushID(areaIdx);

                    DrawAreaRow(area);

                    ImGui.Spacing();
                    ImGui.PopID();
                }

                ImGui.Unindent(12f);
                ImGui.Spacing();
            }

            ImGui.PopID();
        }
    }

    // ── Single area row ───────────────────────────────────────────────────────

    private void DrawAreaRow(AreaSettings area)
    {
        // ── Header: zone name + reward toggle ────────────────────────────────
        ImGui.TextColored(ZoneColor, area.AreaName);
        ImGui.SameLine(210f);

        if (string.IsNullOrEmpty(area.LeagueReward))
        {
            ImGui.TextColored(NoBeaconColor, "(no beacon)");
        }
        else
        {
            var highlight = area.HighlightLeagueReward;
            if (ImGui.Checkbox("##hl", ref highlight))
                area.HighlightLeagueReward = highlight;

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Highlight this reward in green in the overlay");

            ImGui.SameLine();
            ImGui.TextColored(
                area.HighlightLeagueReward ? RewardActive : RewardDimmed,
                area.LeagueReward);
        }

        // ── General notes ─────────────────────────────────────────────────────
        ImGui.TextColored(BuildHintColor, "  General:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);
        var notes = area.Notes ?? "";
        if (ImGui.InputText("##notes", ref notes, 512))
            area.Notes = notes;

        // ── Build-specific notes (only shown when a build is loaded) ──────────
        if (buildManager.HasBuild)
        {
            ImGui.TextColored(BuildLabelColor, $"  [{buildManager.CurrentBuildName}]:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1f);

            var buildNotes = buildManager.GetNotes(area.AreaName);
            if (ImGui.InputText("##buildnotes", ref buildNotes, 512))
                buildManager.SetNotes(area.AreaName, buildNotes);
        }
    }
}
