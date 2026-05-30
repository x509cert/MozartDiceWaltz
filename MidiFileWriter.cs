namespace MozartDiceWaltz;

using System.Buffers.Binary;

/// <summary>
/// Writes a standard MIDI 1.0 file (Format 1, multi-track).
/// No external dependencies — pure BCL byte manipulation.
///
/// Track layout:
///   Track 0: tempo and time signature meta-events
///   Track 1: right hand / melody  (MIDI channel 0)
///   Track 2: left hand / bass     (MIDI channel 1)
/// </summary>
static class MidiFileWriter
{
    private const int Ppq = MeasureLibrary.PPQ; // 480 ticks/quarter

    /// <summary>
    /// Render a full waltz roll to a standard MIDI byte stream.
    /// </summary>
    public static byte[] Build(WaltzRoll roll, int bpm, int repeats)
    {
        int microPerBeat = 60_000_000 / bpm;

        // Flatten all note events into two track streams
        var rhEvents = new List<(long abs, byte[] data)>();
        var lhEvents = new List<(long abs, byte[] data)>();

        long cursor = 0L;

        // Build measure sequence with repeats (AABB form)
        var sequence = BuildRepeatSequence(roll.Details.Select(d => d.Measure).ToArray(), repeats);

        foreach (int measureNum in sequence)
        {
            var notes = MeasureLibrary.Get(measureNum);

            foreach (var ev in notes.OrderBy(e => e.AbsTick))
            {
                long absOn  = cursor + ev.AbsTick;
                long absOff = absOn  + ev.DurTicks;
                byte ch     = ev.Channel;

                // NoteOn
                byte[] on  = [( byte)(0x90 | ch), ev.Pitch, ev.Velocity];
                // NoteOff (using NoteOn velocity=0 for running-status efficiency)
                byte[] off = [(byte)(0x90 | ch), ev.Pitch, 0];

                if (ch == 0) { rhEvents.Add((absOn, on));  rhEvents.Add((absOff, off)); }
                else         { lhEvents.Add((absOn, on));  lhEvents.Add((absOff, off)); }
            }

            cursor += MeasureLibrary.DH; // advance by one 3/4 bar
        }

        byte[] tempoTrack  = BuildTempoTrack(microPerBeat);
        byte[] rhTrack     = BuildNoteTrack(rhEvents, 0);
        byte[] lhTrack     = BuildNoteTrack(lhEvents, 1);

        return BuildFile(numTracks: 3, ppq: Ppq, tempoTrack, rhTrack, lhTrack);
    }

    // ── Repeat logic: AABB ────────────────────────────────────────────────
    private static IEnumerable<int> BuildRepeatSequence(int[] measures, int repeats)
    {
        int half = measures.Length / 2; // 8 + 8

        for (int r = 0; r < repeats; r++)
        {
            foreach (int m in measures[..half])  yield return m; // A
        }
        for (int r = 0; r < repeats; r++)
        {
            foreach (int m in measures[half..])  yield return m; // B
        }
    }

    // ── Tempo track (track 0) ─────────────────────────────────────────────
    private static byte[] BuildTempoTrack(int microPerBeat)
    {
        var track = new List<byte>();

        // Time signature: 3/4, clocks-per-click = 24, 32nd-notes-per-beat = 8
        WriteVlq(track, 0);
        track.AddRange([0xFF, 0x58, 0x04, 0x03, 0x02, 0x18, 0x08]);

        // Tempo: FF 51 03 tt tt tt
        WriteVlq(track, 0);
        track.Add(0xFF); track.Add(0x51); track.Add(0x03);
        track.Add((byte)(microPerBeat >> 16));
        track.Add((byte)(microPerBeat >>  8));
        track.Add((byte)(microPerBeat >>  0));

        // End of track
        WriteVlq(track, 0);
        track.AddRange([0xFF, 0x2F, 0x00]);

        return WrapTrackChunk(track);
    }

    // ── Note track builder ────────────────────────────────────────────────
    private static byte[] BuildNoteTrack(List<(long abs, byte[] data)> events, int channel)
    {
        var track = new List<byte>();

        // Program Change: piano (patch 0) at tick 0
        WriteVlq(track, 0);
        track.Add((byte)(0xC0 | channel));
        track.Add(0x00); // Acoustic Grand Piano

        // Sort events by absolute tick, ties: NoteOn before NoteOff
        var sorted = events
            .OrderBy(e => e.abs)
            .ThenBy(e => e.data[2] == 0 ? 1 : 0) // NoteOff last on same tick
            .ToList();

        long prev = 0;
        foreach (var (abs, data) in sorted)
        {
            WriteVlq(track, (int)(abs - prev));
            track.AddRange(data);
            prev = abs;
        }

        WriteVlq(track, 0);
        track.AddRange([0xFF, 0x2F, 0x00]);

        return WrapTrackChunk(track);
    }

    // ── MIDI file header ──────────────────────────────────────────────────
    private static byte[] BuildFile(int numTracks, int ppq, params byte[][] tracks)
    {
        var file = new List<byte>();

        // MThd header
        file.AddRange("MThd"u8.ToArray());
        file.AddRange(BigEndian32(6));
        file.AddRange(BigEndian16(1));              // Format 1
        file.AddRange(BigEndian16((ushort)numTracks));
        file.AddRange(BigEndian16((ushort)ppq));

        foreach (var t in tracks)
            file.AddRange(t);

        return [.. file];
    }

    private static byte[] WrapTrackChunk(List<byte> data)
    {
        var chunk = new List<byte>();
        chunk.AddRange("MTrk"u8.ToArray());
        chunk.AddRange(BigEndian32(data.Count));
        chunk.AddRange(data);
        return [.. chunk];
    }

    // ── VLQ (variable-length quantity) encoding ───────────────────────────
    private static void WriteVlq(List<byte> buf, int value)
    {
        if (value < 0) value = 0;

        Span<byte> tmp = stackalloc byte[4];
        int count = 0;
        do
        {
            tmp[count++] = (byte)(value & 0x7F);
            value >>= 7;
        } while (value > 0);

        for (int i = count - 1; i > 0; i--)
            buf.Add((byte)(tmp[i] | 0x80));
        buf.Add(tmp[0]);
    }

    private static byte[] BigEndian32(int v)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(b, v);
        return b.ToArray();
    }

    private static byte[] BigEndian16(ushort v)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(b, v);
        return b.ToArray();
    }
}
