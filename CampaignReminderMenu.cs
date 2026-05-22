using System.Numerics;
using ImGuiNET;

namespace CampaignReminder;

/// <summary>
/// Draws the per-zone configuration UI inside DrawSettings().
/// Shows collapsible Act headers, each containing its zones with
/// a reward checkbox and a free-form notes field.
/// </summary>
public class CampaignReminderMenu(CampaignReminderSettings settings)
{
    private static readonly Vector4 HeaderColor  = new(0.95f, 0.80f, 0.30f, 1f);
    private static readonly Vector4 ZoneColor    = new(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Vector4 RewardActive = new(0.40f, 0.90f, 0.45f, 1f);
    private static readonly Vector4 RewardDimmed = new(0.50f, 0.50f, 0.50f, 0.55f);
    private static readonly Vector4 NoBeaconColor = new(0.45f, 0.45f, 0.45f, 0.70f);

    public void DrawConfiguration()
    {
        ImGui.Spacing();
        ImGui.TextColored(HeaderColor, "Campaign Zone Configuration");
        ImGui.Separator();
        ImGui.TextWrapped(
            "Per-zone settings: enable or disable the League reward display, " +
            "and optionally add reminder notes that will appear in the overlay.");
        ImGui.Spacing();

        for (var actIdx = 0; actIdx < settings.Acts.Count; actIdx++)
        {
            var act = settings.Acts[actIdx];
            ImGui.PushID(actIdx);

            // Collapsible act header
            var headerOpen = ImGui.CollapsingHeader(act.ActName + $"  ({act.Areas.Count} zones)##act");
            if (headerOpen)
            {
                ImGui.Indent(12f);
                ImGui.Spacing();

                for (var areaIdx = 0; areaIdx < act.Areas.Count; areaIdx++)
                {
                    var area = act.Areas[areaIdx];
                    ImGui.PushID(areaIdx);

                    // ── Zone name ────────────────────────────────────────────
                    ImGui.TextColored(ZoneColor, area.AreaName);

                    // ── Reward row ───────────────────────────────────────────
                    ImGui.SameLine(200f);  // right-align to a consistent column

                    if (string.IsNullOrEmpty(area.LeagueReward))
                    {
                        // No beacon — show static label, no checkbox
                        ImGui.TextColored(NoBeaconColor, "(no beacon)");
                    }
                    else
                    {
                        var showReward = area.HighlightLeagueReward;
                        if (ImGui.Checkbox("##showReward", ref showReward))
                            area.HighlightLeagueReward = showReward;

                        ImGui.SameLine();
                        ImGui.TextColored(
                            area.HighlightLeagueReward ? RewardActive : RewardDimmed,
                            area.LeagueReward);
                    }

                    // ── Notes text input ─────────────────────────────────────
                    ImGui.SetNextItemWidth(-1f);
                    var notes = area.Notes ?? "";
                    if (ImGui.InputText("##notes", ref notes, 512))
                        area.Notes = notes;

                    ImGui.Spacing();
                    ImGui.PopID();
                }

                ImGui.Unindent(12f);
                ImGui.Spacing();
            }

            ImGui.PopID();
        }
    }
}
