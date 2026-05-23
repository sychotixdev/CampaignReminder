using System.Collections.Generic;
using System.Drawing;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;

namespace CampaignReminder;

public class CampaignReminderSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(true);
    public ToggleNode ShowInTown { get; set; } = new(false);
    public ToggleNode ShowInHideout { get; set; } = new(false);

    // Window position & appearance — only editable in settings, not draggable in-game
    public RangeNode<int> WindowX { get; set; } = new(10, 0, 4000);
    public RangeNode<int> WindowY { get; set; } = new(200, 0, 4000);
    public RangeNode<int> WindowWidth { get; set; } = new(280, 50, 800);
    public RangeNode<int> BackgroundPadding { get; set; } = new(6, 0, 40);
    public ColorNode BackgroundColor { get; set; } = new(Color.FromArgb(200, 0, 0, 0));
    public ColorNode DefaultTextColor { get; set; } = new(Color.FromArgb(220, 220, 220));

    public ButtonNode ResetToDefault { get; set; } = new ButtonNode();

    /// <summary>
    /// File name (not full path) of the currently selected build note file.
    /// Empty string means no build is selected.
    /// Persisted so the same build is reloaded on next launch.
    /// </summary>
    public string SelectedBuildFileName { get; set; } = "";

    // Per-zone configuration, populated with defaults on first load
    public List<ActSettings> Acts { get; set; } = [];
}

public class ActSettings
{
    public string ActName { get; set; } = "";
    public List<AreaSettings> Areas { get; set; } = [];
}

public class AreaSettings
{
    public string AreaName { get; set; } = "";

    /// <summary>The Vaal Beacon campaign reward for this zone.</summary>
    public string LeagueReward { get; set; } = "";

    /// <summary>Whether the league reward line is highlighted green in the overlay.</summary>
    public bool HighlightLeagueReward { get; set; } = true;

    /// <summary>Free-form reminder text shown in the overlay when in this zone.</summary>
    public string Notes { get; set; } = "";
}
