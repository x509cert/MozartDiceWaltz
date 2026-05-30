/*
 ┌───────────────────────────────────────────────────────────────────────────┐
 │  Mozart's Musikalisches Würfelspiel  (K. 516f / K. Anh. 294d,  ~1787)    │
 │  ─────────────────────────────────────────────────────────────────────    │
 │  A musical dice game: roll 2d6 sixteen times to select pre-composed       │
 │  waltz measures from Mozart's manuscript. With 11^16 ≈ 4.5 × 10^16       │
 │  permutations, every performance is effectively unique.                   │
 │                                                                            │
 │  Audio: NAudio 2.2.1 → Windows GM Synthesizer (Acoustic Grand Piano)     │
 │  For highest fidelity, route MIDI to VirtualMIDISynth + Salamander        │
 │  Grand Piano soundfont: https://sfzformats.com/salamander                 │
 │                                                                            │
 │  Build & run:                                                              │
 │    dotnet add package NAudio --version 2.2.1                              │
 │    dotnet run                        – roll dice and perform               │
 │    dotnet run -- --seed 42           – reproducible roll                  │
 │    dotnet run -- --tempo 120         – custom tempo (default 138)         │
 │    dotnet run -- --midi waltz.mid    – export MIDI file                   │
 │    dotnet run -- --list              – show rolls without playing          │
 └───────────────────────────────────────────────────────────────────────────┘
*/

using MozartDiceWaltz;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var opts  = Options.Parse(args);
var game  = new WaltzGame(opts.Tempo, opts.Repeats);
var roll  = game.Roll(opts.Seed is int s ? new Random(s) : Random.Shared);

Banner();
PrintRoll(roll, opts);

if (opts.MidiOutput is string path)
{
    game.ExportMidi(roll, path);
    Console.WriteLine($"  ✓ MIDI exported → {path}");
}
else if (!opts.ListOnly)
{
    // Print the roll table (always shown during playback)
    PrintRollTable(roll, opts);

    var expanded = ExpandedSlots(roll, opts.Repeats);
    int total    = expanded.Count;

    Console.WriteLine($"  ♩ Playing at ♩={opts.Tempo} BPM · {opts.Repeats}× repeats (AABB) · {total} measures");
    Console.WriteLine("  (Ctrl+C to stop)\n");

    // Two fixed display lines rewritten in place with \r — no absolute cursor
    // positions, so console scrolling can never invalidate them.
    //   Line 1: ▶  section / position / dice / bar number / slot counter
    //   Line 2: block progress bar + percentage
    const int barWidth = 32;
    Console.Write("  ▶ …\n  […]");   // seed both lines before playback starts

    var progress = new Progress<int>(slot =>
    {
        if (slot >= expanded.Count) return;
        var (posIndex, d1, d2, measure, section, rep) = expanded[slot];

        int filled = (int)Math.Round((double)(slot + 1) / total * barWidth);
        string bar = new string('█', filled) + new string('░', barWidth - filled);
        int pct    = (slot + 1) * 100 / total;

        // Move up to line 1, overwrite, then go back down to line 2
        Console.Write("\x1b[1A");    // cursor up 1  → land on detail line
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"\r  ▶  {section}{rep}  pos {posIndex+1,2}  {d1}+{d2}={d1+d2,2}  bar {measure,3}   [{slot+1,3}/{total}]  ");
        Console.ResetColor();
        Console.Write($"\n\r  [{bar}] {pct,3}%  ");   // line 2
    });

    await game.PlayAsync(roll, default, progress);

    Console.WriteLine("\n\n  ✓ Done.");
}

static void Banner()
{
    Console.WriteLine("╔══════════════════════════════════════════════════╗");
    Console.WriteLine("║  🎲  Musikalisches Würfelspiel  (K. 516f)  🎹    ║");
    Console.WriteLine("╚══════════════════════════════════════════════════╝\n");
}

static void PrintRoll(WaltzRoll roll, Options opts)
{
    if (!opts.Verbose && !opts.ListOnly) return;
    PrintRollTable(roll, opts);
}

static void PrintRollTable(WaltzRoll roll, Options opts)
{
    Console.WriteLine($"  Seed   : {opts.Seed?.ToString() ?? "(random)"}");
    Console.WriteLine($"  Tempo  : ♩= {opts.Tempo} BPM");
    Console.WriteLine($"  Repeats: {opts.Repeats}× (AABB form)\n");
    Console.WriteLine("  Pos  Dice   Sum  Measure");
    Console.WriteLine("  ───  ─────  ───  ───────");
    for (int i = 0; i < 16; i++)
    {
        (int d1, int d2, int m) = roll.Details[i];
        Console.WriteLine($"  [{i+1,2}]  {d1}+{d2}  →  {d1+d2,2}   bar {m,3}");
    }
    Console.WriteLine();
}

/// <summary>
/// Mirrors MidiPlayer's ExpandWithRepeats: A-section × repeats, then B-section × repeats.
/// Returns (positionIndex 0-15, d1, d2, measure, section label, repeat number).
/// </summary>
static List<(int pos, int d1, int d2, int measure, string section, int rep)>
    ExpandedSlots(WaltzRoll roll, int repeats)
{
    var result = new List<(int, int, int, int, string, int)>();
    int half = roll.Details.Count / 2; // 8

    for (int r = 1; r <= repeats; r++)
        for (int i = 0; i < half; i++)
        {
            (int d1, int d2, int m) = roll.Details[i];
            result.Add((i, d1, d2, m, "A", r));
        }

    for (int r = 1; r <= repeats; r++)
        for (int i = half; i < roll.Details.Count; i++)
        {
            (int d1, int d2, int m) = roll.Details[i];
            result.Add((i, d1, d2, m, "B", r));
        }

    return result;
}
