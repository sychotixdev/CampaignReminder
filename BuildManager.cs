using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CampaignReminder;

// ── Data classes ─────────────────────────────────────────────────────────────

/// <summary>
/// Root object serialized to each build JSON file.
/// Areas is a flat list keyed by AreaName — the Act grouping lives only in
/// the campaign settings, not here, keeping build files simple.
/// </summary>
public class BuildFile
{
    public string BuildName { get; set; } = "";
    public List<BuildAreaNotes> Areas { get; set; } = [];
}

public class BuildAreaNotes
{
    public string AreaName { get; set; } = "";
    public string Notes { get; set; } = "";
}

// ── Manager ──────────────────────────────────────────────────────────────────

/// <summary>
/// Handles discovery, loading, saving, and in-memory access of build note files.
///
/// Files live at: &lt;ConfigDirectory&gt;/Builds/build_*.json
/// One file = one build. The BuildName inside the JSON is the human-readable
/// display name; the file name is derived from it on creation.
/// </summary>
public class BuildManager
{
    // ── Constants ────────────────────────────────────────────────────────────

    private const string SubDir      = "Builds";
    private const string FilePrefix  = "build_";
    private const string FilePattern = "build_*.json";

    // ── State ────────────────────────────────────────────────────────────────

    private readonly string _buildsDir;
    private BuildFile _current;

    /// <summary>Fast lookup: AreaName (case-insensitive) → BuildAreaNotes.</summary>
    private readonly Dictionary<string, BuildAreaNotes> _index =
        new(StringComparer.OrdinalIgnoreCase);

    // ── Public surface ───────────────────────────────────────────────────────

    /// <summary>File names (not full paths) of every build_*.json found in the Builds folder.</summary>
    public string[] AvailableFiles { get; private set; } = [];

    /// <summary>File name of the currently loaded build, empty when none.</summary>
    public string CurrentFileName { get; private set; } = "";

    public bool   HasBuild         => _current != null;
    public string CurrentBuildName => _current?.BuildName ?? "";

    // ── Constructor ──────────────────────────────────────────────────────────

    public BuildManager(string configDirectory)
    {
        _buildsDir = Path.Combine(configDirectory, SubDir);
        Directory.CreateDirectory(_buildsDir);
        Refresh();
    }

    // ── File list ────────────────────────────────────────────────────────────

    /// <summary>Re-scan the Builds folder and update AvailableFiles.</summary>
    public void Refresh()
    {
        AvailableFiles = Directory
            .GetFiles(_buildsDir, FilePattern)
            .Select(Path.GetFileName)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // ── Load / Save ──────────────────────────────────────────────────────────

    /// <summary>Load a build by file name. Silently ignores missing / malformed files.</summary>
    public void Load(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;

        var path = Path.Combine(_buildsDir, fileName);
        if (!File.Exists(path)) return;

        try
        {
            _current       = JsonConvert.DeserializeObject<BuildFile>(File.ReadAllText(path)) ?? new BuildFile();
            CurrentFileName = fileName;
            RebuildIndex();
        }
        catch { /* malformed JSON — leave current build unchanged */ }
    }

    /// <summary>Persist the current build to disk. No-op when nothing is loaded.</summary>
    public void Save()
    {
        if (_current == null || string.IsNullOrEmpty(CurrentFileName)) return;
        try
        {
            File.WriteAllText(
                Path.Combine(_buildsDir, CurrentFileName),
                JsonConvert.SerializeObject(_current, Formatting.Indented));
        }
        catch { /* swallow write errors in game context */ }
    }

    // ── Create ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Create a new build file, save it, refresh the list, and make it current.
    /// Returns the new file name on success, or null if the name was unusable.
    /// </summary>
    public string CreateNew(string buildName)
    {
        if (string.IsNullOrWhiteSpace(buildName)) return null;

        // Sanitise: keep letters, digits, spaces (→ underscore), hyphens
        var safe = new string(buildName
            .Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_')
            .ToArray()).Trim('_');

        if (string.IsNullOrEmpty(safe)) safe = "unnamed";

        // Avoid collision: append _2, _3 … if needed
        var baseName = safe;
        var attempt  = 1;
        string fileName;
        do
        {
            fileName = $"{FilePrefix}{(attempt == 1 ? baseName : baseName + "_" + attempt)}.json";
            attempt++;
        } while (File.Exists(Path.Combine(_buildsDir, fileName)));

        _current        = new BuildFile { BuildName = buildName };
        CurrentFileName  = fileName;
        Save();
        Refresh();
        return fileName;
    }

    // ── Notes access ─────────────────────────────────────────────────────────

    /// <summary>Get build-specific notes for an area. Returns "" when nothing stored.</summary>
    public string GetNotes(string areaName)
    {
        if (string.IsNullOrEmpty(areaName) || !HasBuild) return "";
        return _index.TryGetValue(areaName, out var e) ? e.Notes : "";
    }

    /// <summary>
    /// Set build-specific notes for an area, creating the entry if needed.
    /// Saves to disk immediately (files are small; saving on every edit is fine).
    /// </summary>
    public void SetNotes(string areaName, string notes)
    {
        if (string.IsNullOrEmpty(areaName) || !HasBuild) return;

        if (!_index.TryGetValue(areaName, out var entry))
        {
            entry = new BuildAreaNotes { AreaName = areaName };
            _index[areaName] = entry;
            _current.Areas.Add(entry);
        }

        if (entry.Notes == notes) return;   // unchanged — skip disk write
        entry.Notes = notes;
        Save();
    }

    // ── Display helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Strips the "build_" prefix and ".json" extension for UI display.
    /// e.g. "build_FireBuild.json" → "FireBuild"
    /// </summary>
    public static string DisplayName(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName) ?? fileName;
        return stem.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase)
            ? stem[FilePrefix.Length..]
            : stem;
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void RebuildIndex()
    {
        _index.Clear();
        if (_current?.Areas == null) return;
        foreach (var e in _current.Areas)
            if (!string.IsNullOrEmpty(e.AreaName))
                _index[e.AreaName] = e;
    }
}
