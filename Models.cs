namespace MozartDiceWaltz;

// ── Options ───────────────────────────────────────────────────────────────
/// <summary>Command-line configuration. Primary constructor + pattern matching parse.</summary>
sealed record Options(
    int?   Seed,
    int    Tempo,
    int    Repeats,
    bool   Verbose,
    bool   ListOnly,
    string? MidiOutput)
{
    public static Options Parse(ReadOnlySpan<string> args)
    {
        int?   seed     = null;
        int    tempo    = 138;
        int    repeats  = 2;
        bool   verbose  = false;
        bool   listOnly = false;
        string? midi    = null;

        for (int i = 0; i < args.Length; i++)
        {
            (seed, tempo, repeats, verbose, listOnly, midi) = args[i] switch
            {
                "--seed"   when i + 1 < args.Length => (int.Parse(args[++i]), tempo,  repeats, verbose,  listOnly, midi),
                "--tempo"  when i + 1 < args.Length => (seed, int.Parse(args[++i]),   repeats, verbose,  listOnly, midi),
                "--repeats"when i + 1 < args.Length => (seed, tempo, int.Parse(args[++i]),     verbose,  listOnly, midi),
                "--midi"   when i + 1 < args.Length => (seed, tempo,  repeats, verbose,  listOnly, args[++i]),
                "-v" or "--verbose"                  => (seed, tempo,  repeats, true,     listOnly, midi),
                "--list"                             => (seed, tempo,  repeats, true,     true,     midi),
                _                                    => (seed, tempo,  repeats, verbose,  listOnly, midi),
            };
        }

        return new(seed, tempo, repeats, verbose, listOnly, midi);
    }
}

// ── Dice roll result ──────────────────────────────────────────────────────
/// <summary>Immutable result of one complete dice game: 16 positions, each with die values and selected measure.</summary>
sealed record WaltzRoll(IReadOnlyList<(int Die1, int Die2, int Measure)> Details)
{
    /// <summary>The sequence of 16 measure numbers that will be performed.</summary>
    public IEnumerable<int> Measures => Details.Select(d => d.Measure);
}

// ── MIDI note event ───────────────────────────────────────────────────────
/// <summary>Single MIDI note event with absolute tick time for later scheduling.</summary>
readonly record struct NoteEvent(
    long AbsTick,
    byte Pitch,
    int  DurTicks,
    byte Velocity,
    byte Channel);

// ── Chord type enum ───────────────────────────────────────────────────────
enum ChordQuality { Major, Minor, Dom7, Maj7, Dim }
