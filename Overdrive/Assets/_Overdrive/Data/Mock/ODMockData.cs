/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODMockData - Static class providing realistic fake motorsport data for UI development and testing.
 ##
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Compile-time constants and static lists that populate every OD_UI component
/// with plausible motorsport data. Intended for Editor use (ODShowcaseBuilder) and
/// runtime prototyping. Contains no MonoBehaviour, no serialized state.
/// </summary>
public static class ODMockData
{
    // ─────────────────────────────────────────────────────────────────────────────
    // F1 Timing Tower (20 drivers, 5 columns)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Column definitions for the F1 timing tower.
    /// POS is accent (gold), NAME has flexible width.
    /// </summary>
    public static List<ODTableColumn> F1TimingColumns => new List<ODTableColumn>
    {
        new ODTableColumn { columnId = "pos",   header = "POS",   flexWidth = 0.5f, bold = true, isAccent = true,  alignment = TMPro.TextAlignmentOptions.Center },
        new ODTableColumn { columnId = "name",  header = "DRIVER",flexWidth = 2.0f, bold = true, isAccent = false, alignment = TMPro.TextAlignmentOptions.Left   },
        new ODTableColumn { columnId = "gap",   header = "GAP",   flexWidth = 1.0f,              isAccent = false, alignment = TMPro.TextAlignmentOptions.Right  },
        new ODTableColumn { columnId = "tires", header = "TIRE",  flexWidth = 0.7f,              isAccent = false, alignment = TMPro.TextAlignmentOptions.Center },
        new ODTableColumn { columnId = "laps",  header = "LAPS",  flexWidth = 0.7f,              isAccent = false, alignment = TMPro.TextAlignmentOptions.Center },
    };

    /// <summary>
    /// 20-driver F1 timing tower rows. Row IDs use three-letter driver codes.
    /// First column (pos), then name, gap, tire compound, laps completed.
    /// </summary>
    public static List<List<string>> F1TimingRows => new List<List<string>>
    {
        new List<string> { "1",  "VER  Verstappen",  "LEADER", "M",  "42" },
        new List<string> { "2",  "LEC  Leclerc",     "+2.841", "M",  "42" },
        new List<string> { "3",  "NOR  Norris",      "+4.117", "M",  "42" },
        new List<string> { "4",  "PIA  Piastri",     "+6.332", "H",  "42" },
        new List<string> { "5",  "SAI  Sainz",       "+9.558", "H",  "42" },
        new List<string> { "6",  "HAM  Hamilton",    "+12.03", "M",  "41" },
        new List<string> { "7",  "RUS  Russell",     "+14.77", "M",  "41" },
        new List<string> { "8",  "ALO  Alonso",      "+18.44", "H",  "41" },
        new List<string> { "9",  "STR  Stroll",      "+22.91", "H",  "41" },
        new List<string> { "10", "PER  Perez",       "+27.05", "S",  "41" },
        new List<string> { "11", "GAS  Gasly",       "+30.12", "M",  "40" },
        new List<string> { "12", "OCO  Ocon",        "+33.88", "M",  "40" },
        new List<string> { "13", "HUL  Hulkenberg",  "+37.40", "H",  "40" },
        new List<string> { "14", "MAG  Magnussen",   "+41.22", "H",  "40" },
        new List<string> { "15", "TSU  Tsunoda",     "+44.99", "S",  "39" },
        new List<string> { "16", "LAW  Lawson",      "+48.73", "M",  "39" },
        new List<string> { "17", "BEA  Bearman",     "+52.10", "H",  "39" },
        new List<string> { "18", "ANT  Antonelli",   "+55.87", "H",  "38" },
        new List<string> { "19", "BOR  Bortoleto",   "+1 LAP", "M",  "38" },
        new List<string> { "20", "DOO  Doohan",      "DNF",    "—",  "31" },
    };

    // ─────────────────────────────────────────────────────────────────────────────
    // WRC Standings (10 drivers, 4 columns — proves modularity across championships)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Column definitions for the WRC driver standings table.</summary>
    public static List<ODTableColumn> WRCStandingsColumns => new List<ODTableColumn>
    {
        new ODTableColumn { columnId = "pos",    header = "POS",         flexWidth = 0.5f, bold = true, isAccent = true,  alignment = TMPro.TextAlignmentOptions.Center },
        new ODTableColumn { columnId = "driver", header = "DRIVER",      flexWidth = 2.0f, bold = true, isAccent = false, alignment = TMPro.TextAlignmentOptions.Left   },
        new ODTableColumn { columnId = "nat",    header = "NAT",         flexWidth = 0.8f,              isAccent = false, alignment = TMPro.TextAlignmentOptions.Center },
        new ODTableColumn { columnId = "pts",    header = "PTS",         flexWidth = 0.8f, bold = true, isAccent = false, alignment = TMPro.TextAlignmentOptions.Right  },
    };

    /// <summary>Top 10 WRC driver standings rows.</summary>
    public static List<List<string>> WRCStandingsRows => new List<List<string>>
    {
        new List<string> { "1",  "Ogier S.",      "FRA", "187" },
        new List<string> { "2",  "Evans E.",      "NZL", "164" },
        new List<string> { "3",  "Neuville T.",   "BEL", "152" },
        new List<string> { "4",  "Tänak O.",      "EST", "141" },
        new List<string> { "5",  "Fourmaux A.",   "FRA", "128" },
        new List<string> { "6",  "Lappi E.",      "FIN", "113" },
        new List<string> { "7",  "Katsuta T.",    "JPN", "98"  },
        new List<string> { "8",  "Mikkelsen A.",  "NOR", "84"  },
        new List<string> { "9",  "Solberg O.",    "NOR", "72"  },
        new List<string> { "10", "Rovanperä K.",  "FIN", "60"  },
    };

    // ─────────────────────────────────────────────────────────────────────────────
    // Formula E Energy Data (10 drivers, 4 columns)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Column definitions for the Formula E energy status table.</summary>
    public static List<ODTableColumn> FormulaEColumns => new List<ODTableColumn>
    {
        new ODTableColumn { columnId = "pos",    header = "POS",       flexWidth = 0.5f, bold = true, isAccent = true,  alignment = TMPro.TextAlignmentOptions.Center },
        new ODTableColumn { columnId = "driver", header = "DRIVER",    flexWidth = 2.0f, bold = true, isAccent = false, alignment = TMPro.TextAlignmentOptions.Left   },
        new ODTableColumn { columnId = "energy", header = "ENERGY %",  flexWidth = 1.0f,              isAccent = false, alignment = TMPro.TextAlignmentOptions.Right  },
        new ODTableColumn { columnId = "atk",    header = "ATTACK",    flexWidth = 0.8f,              isAccent = false, alignment = TMPro.TextAlignmentOptions.Center },
    };

    /// <summary>Top 10 Formula E energy status rows (attack mode = times used this race).</summary>
    public static List<List<string>> FormulaERows => new List<List<string>>
    {
        new List<string> { "1",  "Cassidy N.",    "81%", "2×" },
        new List<string> { "2",  "Wehrlein P.",   "78%", "2×" },
        new List<string> { "3",  "Vergne J-E.",   "75%", "1×" },
        new List<string> { "4",  "Nato N.",       "72%", "2×" },
        new List<string> { "5",  "Evans M.",      "69%", "1×" },
        new List<string> { "6",  "Bird S.",       "66%", "2×" },
        new List<string> { "7",  "Da Costa A.",   "63%", "1×" },
        new List<string> { "8",  "Gunther M.",    "60%", "0×" },
        new List<string> { "9",  "Ticktum D.",    "57%", "1×" },
        new List<string> { "10", "Rowland O.",    "54%", "2×" },
    };

    // ─────────────────────────────────────────────────────────────────────────────
    // ODDriverCard telemetry
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Leclerc (#16, Scuderia Ferrari) telemetry cells — 6 metrics for the driver card grid.</summary>
    public static List<ODDriverCard.TelemetryCellData> LeclercTelemetry => new List<ODDriverCard.TelemetryCellData>
    {
        new ODDriverCard.TelemetryCellData { label = "INTERVAL", value = "+0.148" },
        new ODDriverCard.TelemetryCellData { label = "KM/H",     value = "312"    },
        new ODDriverCard.TelemetryCellData { label = "GEAR",     value = "7"      },
        new ODDriverCard.TelemetryCellData { label = "TIRES",    value = "M"      },
        new ODDriverCard.TelemetryCellData { label = "DRS",      value = "ON"     },
        new ODDriverCard.TelemetryCellData { label = "SECTOR",   value = "S3"     },
    };

    /// <summary>Verstappen (#1, Oracle Red Bull Racing) telemetry cells.</summary>
    public static List<ODDriverCard.TelemetryCellData> VerstappenTelemetry => new List<ODDriverCard.TelemetryCellData>
    {
        new ODDriverCard.TelemetryCellData { label = "INTERVAL", value = "LEADER" },
        new ODDriverCard.TelemetryCellData { label = "KM/H",     value = "318"    },
        new ODDriverCard.TelemetryCellData { label = "GEAR",     value = "8"      },
        new ODDriverCard.TelemetryCellData { label = "TIRES",    value = "M"      },
        new ODDriverCard.TelemetryCellData { label = "DRS",      value = "OFF"    },
        new ODDriverCard.TelemetryCellData { label = "SECTOR",   value = "S1"     },
    };

    // ─────────────────────────────────────────────────────────────────────────────
    // Media controls state
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Default live state for ODMediaControls showcase — badge shows LIVE, slider locked.</summary>
    public static bool  MockIsLive    = true;
    /// <summary>0.0 when live (no recorded progress), otherwise 0–1 position in replay.</summary>
    public static float MockProgress  = 0.0f;

    // ─────────────────────────────────────────────────────────────────────────────
    // Session info
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Displayed in session header banners (e.g. ODCard title or overlay label).</summary>
    public static string MockSessionTitle  = "F1 · RACE";
    /// <summary>Lap counter string for timing overlays.</summary>
    public static string MockLapInfo       = "LAP 42 / 58";
    /// <summary>Championship branding line.</summary>
    public static string MockChampionship  = "FORMULA 1";
}
