namespace MozartDiceWaltz;

/// <summary>
/// Orchestrates a complete performance of the Musikalisches Würfelspiel:
///   1. Roll the dice (16 positions × 2d6)
///   2. Render either live via MIDI or export to a .mid file
/// </summary>
sealed class WaltzGame(int bpm = 138, int repeats = 2)
{
    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Perform all 16 dice rolls and return the result.</summary>
    public WaltzRoll Roll(Random rng) => DiceTable.PerformGame(rng);

    /// <summary>Play the waltz live through the Windows GM synthesizer.</summary>
    public async Task PlayAsync(
        WaltzRoll roll,
        CancellationToken ct = default,
        IProgress<int>? onMeasureStart = null)
    {
        using var player = CreatePlayer();
        await player.PlayMeasuresAsync(
            roll.Measures,
            bpm,
            repeats,
            ct,
            onMeasureStart);
    }

    /// <summary>
    /// Blocking overload for convenience.
    /// Wraps PlayAsync in a new task so it can run from a sync context.
    /// </summary>
    public void Play(WaltzRoll roll)
        => PlayAsync(roll).GetAwaiter().GetResult();

    /// <summary>Write the waltz to a standard MIDI file.</summary>
    public void ExportMidi(WaltzRoll roll, string path)
    {
        byte[] bytes = MidiFileWriter.Build(roll, bpm, repeats);
        File.WriteAllBytes(path, bytes);
    }

    // ── Internals ─────────────────────────────────────────────────────────

    private static MidiPlayer CreatePlayer()
    {
        // Prefer device "VirtualMIDISynth" if present (better piano quality);
        // fall back to device 0 (Microsoft GS Wavetable Synth).
        int deviceIndex = FindBestMidiDevice();
        return new MidiPlayer(deviceIndex);
    }

    private static int FindBestMidiDevice()
    {
        int count = NAudio.Midi.MidiOut.NumberOfDevices;
        for (int i = 0; i < count; i++)
        {
            string name = NAudio.Midi.MidiOut.DeviceInfo(i).ProductName;
            if (name.Contains("VirtualMIDISynth", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Salamander",       StringComparison.OrdinalIgnoreCase) ||
                name.Contains("FluidSynth",       StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return 0; // Microsoft GS Wavetable Synth
    }
}
