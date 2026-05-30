namespace MozartDiceWaltz;

using NAudio.Midi;
using System.Diagnostics;

/// <summary>
/// Real-time playback via NAudio's MidiOut, targeting the Windows GM synthesizer.
///
/// MIDI device selection:
///   Device 0 is usually "Microsoft GS Wavetable Synth" — a decent GM piano.
///
/// For highest-fidelity piano sound, install VirtualMIDISynth (free) and load
/// the Salamander Grand Piano soundfont.  VirtualMIDISynth registers itself as
/// a MIDI device and will appear as device index 0 or 1.
/// → https://coolsoft.altervista.org/en/virtualmidisynth
/// → https://sfzformats.com/salamander
///
/// Both channels use Program 0 (Acoustic Grand Piano, GM standard).
/// Velocity, articulation and pedal-sustain effects are encoded per note.
/// </summary>
sealed class MidiPlayer : IDisposable
{
    private readonly MidiOut   _out;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private bool _disposed;

    public MidiPlayer(int deviceIndex = 0)
    {
        _out = new MidiOut(deviceIndex);

        // GM reset + piano on both channels
        SendProgramChange(channel: 0, patch: 0);   // Acoustic Grand Piano — RH
        SendProgramChange(channel: 1, patch: 0);   // Acoustic Grand Piano — LH

        // Lower reverb/chorus so the dry piano tone stays clear
        SendControlChange(channel: 0, controller: 91, value: 20); // reverb
        SendControlChange(channel: 1, controller: 91, value: 20);
        SendControlChange(channel: 0, controller: 93, value:  0); // chorus off
        SendControlChange(channel: 1, controller: 93, value:  0);

        // Set expression to full
        SendControlChange(channel: 0, controller: 11, value: 127);
        SendControlChange(channel: 1, controller: 11, value: 100); // LH slightly softer
    }

    // ── Core playback ──────────────────────────────────────────────────────
    public async Task PlayMeasuresAsync(
        IEnumerable<int> measureNumbers,
        int bpm,
        int repeats,
        CancellationToken ct = default,
        IProgress<int>? onMeasureStart = null)
    {
        double tickMs = 60_000.0 / bpm / MeasureLibrary.PPQ; // ms per MIDI tick

        var sequence = ExpandWithRepeats(measureNumbers.ToArray(), repeats).ToArray();

        // Pre-schedule all note events with absolute millisecond timestamps
        // and the sequence index (measure slot) each event belongs to.
        var events = ScheduleEvents(sequence, tickMs);

        // Precision playback loop — fires events within ≈1ms of target time
        long startMs = _clock.ElapsedMilliseconds;
        int  lastReportedSlot = -1;

        foreach (var (targetMs, rawMsg, slot) in events)
        {
            ct.ThrowIfCancellationRequested();

            // Report the measure index once, at its first event
            if (onMeasureStart is not null && slot != lastReportedSlot)
            {
                lastReportedSlot = slot;
                onMeasureStart.Report(slot);
            }

            long now = _clock.ElapsedMilliseconds - startMs;
            long wait = targetMs - now;

            if (wait > 2)
                await Task.Delay((int)(wait - 1), ct); // sleep most of wait

            // Spin for sub-ms precision
            while (_clock.ElapsedMilliseconds - startMs < targetMs) { }

            _out.Send(rawMsg);
        }

        // Let final notes ring for their full duration
        await Task.Delay(500, ct);
        AllNotesOff();
    }

    // ── Event scheduling ──────────────────────────────────────────────────
    private static List<(long ms, int msg, int slot)> ScheduleEvents(
        IEnumerable<int> measures,
        double tickMs)
    {
        var scheduled = new List<(long ms, int msg, int slot)>();
        long cursorTicks = 0L;
        int  slot = 0;

        foreach (int measureNum in measures)
        {
            var notes = MeasureLibrary.Get(measureNum);

            foreach (var ev in notes)
            {
                long onTick  = cursorTicks + ev.AbsTick;
                long offTick = onTick + ev.DurTicks;

                int onMsg  = NoteOn( ev.Channel, ev.Pitch, ev.Velocity);
                int offMsg = NoteOff(ev.Channel, ev.Pitch);

                scheduled.Add(((long)(onTick  * tickMs), onMsg,  slot));
                scheduled.Add(((long)(offTick * tickMs), offMsg, slot));
            }

            cursorTicks += MeasureLibrary.DH;
            slot++;
        }

        return scheduled
            .OrderBy(e => e.ms)
            .ThenBy(e => (e.msg & 0xF0) == 0x80 ? 0 : 1) // NoteOff before NoteOn at same ms
            .ToList();
    }

    // ── MIDI message helpers ───────────────────────────────────────────────
    private static int NoteOn(byte channel, byte pitch, byte velocity)
        => (0x90 | channel) | (pitch << 8) | (velocity << 16);

    private static int NoteOff(byte channel, byte pitch)
        => (0x80 | channel) | (pitch << 8) | (0 << 16);

    private void SendProgramChange(int channel, int patch)
        => _out.Send(new MidiMessage((0xC0 | channel) | (patch << 8)).RawData);

    private void SendControlChange(int channel, int controller, int value)
        => _out.Send(new MidiMessage((int)(0xB0 | channel) | (controller << 8) | (value << 16)).RawData);

    private void AllNotesOff()
    {
        for (int ch = 0; ch < 2; ch++)
            _out.Send(new MidiMessage((int)(0xB0 | ch) | (123 << 8)).RawData); // CC 123: all notes off
    }

    // ── Repeat logic (AABB form) ───────────────────────────────────────────
    private static IEnumerable<int> ExpandWithRepeats(int[] measures, int repeats)
    {
        int half = measures.Length / 2;
        for (int r = 0; r < repeats; r++)
            foreach (int m in measures[..half]) yield return m;
        for (int r = 0; r < repeats; r++)
            foreach (int m in measures[half..]) yield return m;
    }

    // ── Device enumeration ────────────────────────────────────────────────
    public static void ListDevices()
    {
        Console.WriteLine("\n  MIDI Output Devices:");
        for (int i = 0; i < MidiOut.NumberOfDevices; i++)
        {
            var caps = MidiOut.DeviceInfo(i);
            Console.WriteLine($"    [{i}] {caps.ProductName}");
        }
        Console.WriteLine();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        AllNotesOff();
        _out.Dispose();
    }
}
