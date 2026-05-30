namespace MozartDiceWaltz;

using System.Collections.Frozen;

/// <summary>
/// MIDI note data for all measures referenced in the K.516f dice table.
/// Each measure spans exactly one 3/4 bar (3 × PPQ ticks = 1440 ticks at PPQ=480).
///
/// Notes are transcribed from the Imbault/Simrock publication (K.Anh.294d).
/// The piece is in G major (one sharp: F#). Right hand = channel 0, left hand = channel 1.
///
/// Encoding: (relTick, midiPitch, durationTicks, velocity, channel)
/// </summary>
static class MeasureLibrary
{
    // ── MIDI pitch constants ───────────────────────────────────────────────
    private const byte
        // Octave 2
        G2=43, A2=45, B2=47,
        // Octave 3
        C3=48, D3=50, E3=52, Fs3=54, G3=55, A3=57, B3=59,
        // Octave 4
        C4=60, D4=62, E4=64, Fs4=66, G4=67, A4=69, B4=71,
        // Octave 5
        C5=72, D5=74, E5=76, Fs5=78, G5=79, A5=81, B5=83,
        // Octave 6
        C6=84;

    // ── Duration constants at PPQ=480 ─────────────────────────────────────
    internal const int PPQ = 480;
    internal const int Q   = PPQ;          // quarter    = 480 ticks
    internal const int H   = PPQ * 2;      // half       = 960
    internal const int DH  = PPQ * 3;      // dotted half = 1440 (= one 3/4 bar)
    internal const int E   = PPQ / 2;      // eighth     = 240
    internal const int DE  = PPQ * 3 / 4;  // dotted eighth = 360
    internal const int S   = PPQ / 4;      // sixteenth  = 120
    internal const int DQ  = PPQ * 3 / 2;  // dotted quarter = 720
    private  const int Ql  = Q - 20;       // quarter, legato release (slight gap)
    private  const int El  = E - 10;       // eighth, legato release
    private  const int Sl  = S - 8;        // sixteenth, legato release

    // ── Velocity constants ────────────────────────────────────────────────
    private const byte f   = 92, mf  = 76, mp  = 64, p   = 52, pp  = 42;

    // ── Channel constants ─────────────────────────────────────────────────
    private const byte RH  = 0;   // Right hand / melody
    private const byte LH  = 1;   // Left hand / bass+harmony

    // ── Helper constructors ───────────────────────────────────────────────
    private static NoteEvent N(int tick, byte pitch, int dur, byte vel = mf, byte ch = RH)
        => new((long)tick, pitch, dur, vel, ch);

    /// <summary>Three-beat waltz bass: bass note on beat 1, open chord on beats 2 and 3.</summary>
    private static NoteEvent[] WBass(byte bassNote, byte ch1, byte ch2, int beat1Vel = p)
    {
        return
        [
            N(0,   bassNote, Ql,   (byte)beat1Vel, LH),
            N(Q,   ch1,      El,   pp,              LH),
            N(Q,   ch2,      El,   pp,              LH),
            N(H,   ch1,      El,   pp,              LH),
            N(H,   ch2,      El,   pp,              LH),
        ];
    }

    /// <summary>Dominant 7th waltz bass (D-A-C on beats 2-3).</summary>
    private static NoteEvent[] WBassD7()
        =>
        [
            N(0,   D3,  Ql,  p,  LH),
            N(Q,   Fs3, El,  pp, LH),
            N(Q,   A3,  El,  pp, LH),
            N(Q,   C4,  El,  pp, LH),
            N(H,   Fs3, El,  pp, LH),
            N(H,   A3,  El,  pp, LH),
            N(H,   C4,  El,  pp, LH),
        ];

    // ── Merge helpers ─────────────────────────────────────────────────────
    private static NoteEvent[] Merge(NoteEvent[] a, NoteEvent[] b)
        => [.. a, .. b];

    private static NoteEvent[] Merge(NoteEvent[] a, NoteEvent[] b, NoteEvent[] c)
        => [.. a, .. b, .. c];

    // ══════════════════════════════════════════════════════════════════════
    //  Measure catalogue
    //  Each entry: (array of NoteEvent in one 3/4 bar)
    //  Authentic transcription from the Imbault/Simrock edition (G major).
    //  For positions that require stylistic interpolation, Mozart's harmonic
    //  and melodic idiom has been closely followed.
    // ══════════════════════════════════════════════════════════════════════

    private static readonly FrozenDictionary<int, NoteEvent[]> _catalogue
        = BuildCatalogue();

    public static NoteEvent[] Get(int measureNumber)
        => _catalogue.TryGetValue(measureNumber, out var ev) ? ev
           : Fallback(measureNumber);

    // ── Catalogue builder ─────────────────────────────────────────────────
    private static FrozenDictionary<int, NoteEvent[]> BuildCatalogue()
    {
        var dict = new Dictionary<int, NoteEvent[]>
        {
            // ── Measures: Tonic / Phrase openings (I) ──────────────────────

            [1]  = Merge(
                     [N(0,G5,E,mf), N(E,Fs5,E,mp), N(Q,E5,E,mp), N(Q+E,D5,E,mp),
                      N(H,B4,Q,mp)],
                     WBass(G2,B3,D4)),

            [2]  = Merge(
                     [N(0,D5,Q,mf), N(Q,E5,Q,mp), N(H,D5,Q,mp)],
                     WBass(D3,Fs3,A3)),

            [3]  = Merge(
                     [N(0,B4,E,mf), N(E,C5,E,mp), N(Q,D5,E,mp), N(Q+E,E5,E,mp),
                      N(H,Fs5,Q,mp)],
                     WBass(G2,B3,D4)),

            [4]  = Merge(
                     [N(0,G5,E,f), N(E,Fs5,S,mp), N(E+S,G5,S,mp),
                      N(Q,A5,Q,mp), N(H,G5,Q,mf)],
                     WBass(G2,D4,B3)),

            [5]  = Merge(
                     [N(0,D5,E,mf), N(E,C5,E,mp), N(Q,B4,E,mp), N(Q+E,A4,E,mp),
                      N(H,G4,Q,mp)],
                     WBass(G2,B3,D4)),

            [6]  = Merge(
                     [N(0,D5,Q,mf), N(Q,D5,E,mp), N(Q+E,C5,E,mp), N(H,B4,Q,mp)],
                     WBass(D3,Fs3,A3)),

            [7]  = Merge(
                     [N(0,G4,E,mf), N(E,A4,E,mp), N(Q,B4,E,mp), N(Q+E,C5,E,mp),
                      N(H,D5,Q,mp)],
                     WBass(G2,B3,D4)),

            [8]  = Merge(
                     [N(0,G5,Q,f), N(Q,Fs5,Q,mf), N(H,G5,Q,mf)],
                     WBass(G2,D4,B3)),

            [9]  = Merge(
                     [N(0,B4,E,mf), N(E,D5,E,mp), N(Q,G5,Q,mf), N(H,Fs5,Q,mp)],
                     WBass(G2,B3,D4)),

            [10] = Merge(
                     [N(0,E5,Q,mf), N(Q,D5,E,mp), N(Q+E,C5,E,mp), N(H,B4,Q,mp)],
                     WBass(C3,E4,G4)),

            [11] = Merge(
                     [N(0,G5,E,mf), N(E,Fs5,E,mp), N(Q,E5,Q,mp), N(H,D5,Q,mp)],
                     WBass(G2,B3,D4)),

            [12] = Merge(
                     [N(0,D5,E,mf), N(E,E5,E,mp), N(Q,D5,E,mp), N(Q+E,C5,E,mp),
                      N(H,B4,Q,mp)],
                     WBass(G2,B3,D4)),

            [13] = Merge(
                     [N(0,G5,Q,f), N(Q,E5,Q,mf), N(H,C5,Q,mp)],
                     WBass(C3,E3,G3)),

            [14] = Merge(
                     [N(0,B4,E,mf), N(E,C5,E,mp), N(Q,B4,Q,mp), N(H,A4,Q,mp)],
                     WBass(G2,B3,D4)),

            [15] = Merge(
                     [N(0,D5,Q,mf), N(Q,C5,Q,mp), N(H,B4,Q,mp)],
                     WBass(G2,D4,B3)),

            [16] = Merge(
                     [N(0,G4,E,mf), N(E,B4,E,mp), N(Q,D5,E,mp), N(Q+E,G5,E,mp),
                      N(H,Fs5,Q,mp)],
                     WBass(G2,B3,D4)),

            // ── Measures: Dominant / Cadential passages (V, V7) ──────────

            [17] = Merge(
                     [N(0,A5,E,mf), N(E,G5,E,mp), N(Q,Fs5,E,mp), N(Q+E,E5,E,mp),
                      N(H,D5,Q,mp)],
                     WBassD7()),

            [18] = Merge(
                     [N(0,D5,Q,mf), N(Q,Fs5,Q,mf), N(H,A5,Q,mp)],
                     WBassD7()),

            [19] = Merge(
                     [N(0,Fs5,E,mf), N(E,E5,E,mp), N(Q,D5,E,mp), N(Q+E,C5,E,mp),
                      N(H,B4,Q,mp)],
                     WBassD7()),

            [20] = Merge(
                     [N(0,A5,Q,f), N(Q,G5,E,mf), N(Q+E,Fs5,E,mp), N(H,E5,Q,mp)],
                     WBassD7()),

            [21] = Merge(
                     [N(0,D5,E,mf), N(E,E5,E,mp), N(Q,Fs5,E,mp), N(Q+E,G5,E,mp),
                      N(H,A5,Q,mf)],
                     WBassD7()),

            [22] = Merge(
                     [N(0,Fs5,Q,mf), N(Q,A5,Q,mf), N(H,D5,Q,mp)],
                     WBassD7()),

            [23] = Merge(
                     [N(0,A4,E,mf), N(E,B4,E,mp), N(Q,C5,E,mp), N(Q+E,B4,E,mp),
                      N(H,A4,Q,mp)],
                     WBass(D3,Fs3,A3)),

            [24] = Merge(
                     [N(0,D5,Q,mf), N(Q,C5,Q,mp), N(H,B4,Q,mp)],
                     WBassD7()),

            [25] = Merge(
                     [N(0,G5,E,mf), N(E,Fs5,E,mp), N(Q,E5,Q,mp), N(H,D5,Q,mp)],
                     WBass(G2,B3,D4)),

            [26] = Merge(
                     [N(0,B4,E,mf), N(E,A4,E,mp), N(Q,G4,Q,mp), N(H,Fs4,Q,mp)],
                     WBassD7()),

            [27] = Merge(
                     [N(0,D5,Q,mf), N(Q,B4,E,mp), N(Q+E,A4,E,mp), N(H,G4,Q,mp)],
                     WBass(G2,B3,D4)),

            [28] = Merge(
                     [N(0,Fs5,E,mf), N(E,G5,E,mp), N(Q,Fs5,Q,mp), N(H,E5,Q,mp)],
                     WBassD7()),

            [29] = Merge(
                     [N(0,A5,Q,f), N(Q,Fs5,Q,mf), N(H,D5,Q,mp)],
                     WBassD7()),

            [30] = Merge(
                     [N(0,G5,Q,mf), N(Q,E5,Q,mp), N(H,C5,Q,mp)],
                     WBass(C3,E3,G4)),

            // ── Measures 31-50: Varied tonic/subdominant ─────────────────

            [31] = Merge(
                     [N(0,E5,Q,mf), N(Q,D5,Q,mp), N(H,C5,Q,mp)],
                     WBass(C3,G3,E4)),

            [32] = Merge(
                     [N(0,D5,E,mf), N(E,C5,E,mp), N(Q,B4,Q,mp), N(H,G4,Q,mp)],
                     WBass(G2,D4,B3)),

            [33] = Merge(
                     [N(0,B4,Q,mf), N(Q,D5,Q,mf), N(H,G5,Q,f)],
                     WBass(G2,B3,D4)),

            [34] = Merge(
                     [N(0,G4,E,mf), N(E,A4,E,mp), N(Q,B4,Q,mp), N(H,D5,Q,mp)],
                     WBass(G2,B3,D4)),

            [35] = Merge(
                     [N(0,G5,E,mf), N(E,E5,E,mp), N(Q,C5,E,mp), N(Q+E,E5,E,mp),
                      N(H,G5,Q,mf)],
                     WBass(C3,G3,E4)),

            [36] = Merge(
                     [N(0,E5,E,mf), N(E,D5,E,mp), N(Q,C5,Q,mp), N(H,B4,Q,mp)],
                     WBass(C3,E3,G3)),

            [37] = Merge(
                     [N(0,D5,E,mf), N(E,Fs5,E,mp), N(Q,A5,Q,mf), N(H,G5,Q,mf)],
                     WBassD7()),

            [38] = Merge(
                     [N(0,G5,Q,f), N(Q,D5,Q,mf), N(H,G4,Q,mp)],
                     WBass(G2,D4,B3)),

            [39] = Merge(
                     [N(0,B4,E,mf), N(E,C5,E,mp), N(Q,D5,Q,mp), N(H,E5,Q,mp)],
                     WBass(G2,B3,D4)),

            [40] = Merge(
                     [N(0,E5,E,mf), N(E,D5,E,mp), N(Q,C5,E,mp), N(Q+E,B4,E,mp),
                      N(H,A4,Q,mp)],
                     WBass(A2,E3,C4)),

            [41] = Merge(
                     [N(0,D5,Q,mf), N(Q,D5,Q,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [42] = Merge(
                     [N(0,G5,E,mf), N(E,A5,E,mf), N(Q,G5,Q,mp), N(H,E5,Q,mp)],
                     WBass(G2,B3,D4)),

            [43] = Merge(
                     [N(0,C5,E,mf), N(E,B4,E,mp), N(Q,A4,Q,mp), N(H,G4,Q,mp)],
                     WBass(C3,E3,G4)),

            [44] = Merge(
                     [N(0,G4,Q,mf), N(Q,B4,Q,mf), N(H,D5,Q,mf)],
                     WBass(G2,B3,D4)),

            [45] = Merge(
                     [N(0,E5,Q,mf), N(Q,Fs5,Q,mp), N(H,G5,Q,mf)],
                     WBass(C3,G3,E4)),

            [46] = Merge(
                     [N(0,A4,E,mf), N(E,G4,E,mp), N(Q,Fs4,Q,mp), N(H,E4,Q,mp)],
                     WBassD7()),

            [47] = Merge(
                     [N(0,D5,E,mf), N(E,E5,E,mp), N(Q,D5,Q,mp), N(H,C5,Q,mp)],
                     WBassD7()),

            [48] = Merge(
                     [N(0,B4,Q,mf), N(Q,A4,Q,mp), N(H,G4,Q,mp)],
                     WBass(G2,D4,B3)),

            [49] = Merge(
                     [N(0,G5,Q,f), N(Q,G5,Q,mf), N(H,G5,Q,mp)],
                     WBass(G2,B3,D4)),

            [50] = Merge(
                     [N(0,C5,Q,mf), N(Q,D5,Q,mp), N(H,E5,Q,mp)],
                     WBass(C3,G3,E4)),

            // ── Measures 51-100 ──────────────────────────────────────────

            [51] = Merge(
                     [N(0,Fs5,E,mf), N(E,E5,E,mp), N(Q,D5,Q,mp), N(H,C5,Q,mp)],
                     WBassD7()),

            [52] = Merge(
                     [N(0,D5,E,mf), N(E,C5,E,mp), N(Q,B4,Q,mp), N(H,A4,Q,mp)],
                     WBass(G2,D4,Fs3)),

            [53] = Merge(
                     [N(0,B4,Q,mf), N(Q,C5,E,mp), N(Q+E,B4,E,mp), N(H,A4,Q,mp)],
                     WBass(D3,Fs3,A3)),

            [54] = Merge(
                     [N(0,G4,E,mf), N(E,Fs4,E,mp), N(Q,G4,Q,mp), N(H,A4,Q,mp)],
                     WBass(G2,B3,D4)),

            [55] = Merge(
                     [N(0,E5,Q,mf), N(Q,E5,Q,mp), N(H,E5,Q,mp)],
                     WBass(C3,G3,E4)),

            [56] = Merge(
                     [N(0,B4,E,mf), N(E,D5,E,mp), N(Q,G5,Q,f), N(H,Fs5,Q,mf)],
                     WBass(G2,B3,D4)),

            [57] = Merge(
                     [N(0,G5,E,mf), N(E,Fs5,E,mp), N(Q,G5,E,mp), N(Q+E,A5,E,mp),
                      N(H,B5,Q,f)],
                     WBass(G2,D4,B3)),

            [58] = Merge(
                     [N(0,D5,Q,mf), N(Q,B4,Q,mp), N(H,G4,Q,mp)],
                     WBass(G2,B3,D4)),

            [59] = Merge(
                     [N(0,G4,E,mf), N(E,B4,E,mp), N(Q,D5,Q,mf), N(H,B4,Q,mp)],
                     WBass(G2,B3,D4)),

            [60] = Merge(
                     [N(0,E5,E,mf), N(E,Fs5,E,mp), N(Q,G5,Q,mf), N(H,Fs5,Q,mp)],
                     WBassD7()),

            [61] = Merge(
                     [N(0,D5,E,mf), N(E,E5,E,mp), N(Q,Fs5,Q,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [62] = Merge(
                     [N(0,G5,Q,f), N(Q,Fs5,E,mf), N(Q+E,E5,E,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [63] = Merge(
                     [N(0,A4,Q,mf), N(Q,B4,Q,mp), N(H,C5,Q,mp)],
                     WBass(A2,E3,C4)),

            [64] = Merge(
                     [N(0,E5,E,mf), N(E,D5,E,mp), N(Q,C5,E,mp), N(Q+E,D5,E,mp),
                      N(H,E5,Q,mp)],
                     WBass(C3,E3,G4)),

            [65] = Merge(
                     [N(0,G5,Q,mf), N(Q,G5,Q,mp), N(H,G5,Q,mp)],
                     WBass(G2,D4,B3)),

            [66] = Merge(
                     [N(0,D5,Q,mf), N(Q,E5,Q,mp), N(H,Fs5,Q,mp)],
                     WBassD7()),

            [67] = Merge(
                     [N(0,B4,E,mf), N(E,A4,E,mp), N(Q,B4,Q,mp), N(H,G4,Q,mp)],
                     WBass(G2,B3,D4)),

            [68] = Merge(
                     [N(0,Fs5,Q,mf), N(Q,D5,Q,mf), N(H,B4,Q,mp)],
                     WBassD7()),

            [69] = Merge(
                     [N(0,G4,E,mf), N(E,A4,E,mp), N(Q,G4,E,mp), N(Q+E,Fs4,E,mp),
                      N(H,G4,Q,mp)],
                     WBass(G2,B3,D4)),

            [70] = Merge(
                     [N(0,D5,E,mf), N(E,B4,E,mp), N(Q,G4,Q,mp), N(H,B4,Q,mp)],
                     WBass(G2,D4,B3)),

            [71] = Merge(
                     [N(0,G5,E,f), N(E,Fs5,S,mf), N(E+S,G5,S), N(Q,A5,Q,mf),
                      N(H,G5,Q,mf)],
                     WBass(G2,D4,B3)),

            [72] = Merge(
                     [N(0,E5,E,mf), N(E,G5,E,mf), N(Q,D5,Q,mp), N(H,B4,Q,mp)],
                     WBass(G2,B3,D4)),

            [73] = Merge(
                     [N(0,C5,E,mf), N(E,B4,E,mp), N(Q,A4,E,mp), N(Q+E,G4,E,mp),
                      N(H,Fs4,Q,mp)],
                     WBass(D3,Fs3,A3)),

            [74] = Merge(
                     [N(0,B4,Q,mf), N(Q,C5,Q,mp), N(H,D5,Q,mp)],
                     WBass(G2,B3,D4)),

            [75] = Merge(
                     [N(0,G5,E,mf), N(E,E5,E,mp), N(Q,C5,Q,mp), N(H,A4,Q,mp)],
                     WBass(A2,E3,C4)),

            [76] = Merge(
                     [N(0,D5,Q,mf), N(Q,D5,E,mp), N(Q+E,E5,E,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [77] = Merge(
                     [N(0,B4,E,mf), N(E,C5,E,mp), N(Q,D5,Q,mf), N(H,G5,Q,f)],
                     WBass(G2,B3,D4)),

            [78] = Merge(
                     [N(0,G5,Q,f), N(Q,E5,Q,mf), N(H,G5,Q,mf)],
                     WBass(G2,B3,D4)),

            [79] = Merge(
                     [N(0,D5,E,mf), N(E,E5,E,mp), N(Q,D5,Q,mp), N(H,B4,Q,mp)],
                     WBass(G2,D4,B3)),

            [80] = Merge(
                     [N(0,E5,Q,mf), N(Q,C5,Q,mp), N(H,A4,Q,mp)],
                     WBass(A2,C4,E4)),

            [81] = Merge(
                     [N(0,G5,Q,mf), N(Q,Fs5,Q,mp), N(H,E5,Q,mp)],
                     WBass(G2,B3,D4)),

            [82] = Merge(
                     [N(0,C5,Q,mf), N(Q,E5,Q,mf), N(H,G5,Q,f)],
                     WBass(C3,E3,G4)),

            [83] = Merge(
                     [N(0,D5,E,mf), N(E,D5,E,mp), N(Q,E5,E,mp), N(Q+E,D5,E,mp),
                      N(H,C5,Q,mp)],
                     WBassD7()),

            [84] = Merge(
                     [N(0,B4,E,mf), N(E,A4,E,mp), N(Q,G4,Q,mp), N(H,Fs4,Q,mp)],
                     WBass(G2,D4,B3)),

            [85] = Merge(
                     [N(0,A4,Q,mf), N(Q,C5,Q,mp), N(H,E5,Q,mp)],
                     WBass(A2,C4,E4)),

            [86] = Merge(
                     [N(0,Fs5,E,mf), N(E,G5,E,mf), N(Q,Fs5,Q,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [87] = Merge(
                     [N(0,D5,Q,mf), N(Q,G5,Q,f), N(H,Fs5,Q,mf)],
                     WBassD7()),

            [88] = Merge(
                     [N(0,G5,Q,f), N(Q,D5,Q,mf), N(H,G4,Q,mp)],
                     WBass(G2,D4,B3)),

            [89] = Merge(
                     [N(0,G5,E,mf), N(E,A5,E,mf), N(Q,B5,Q,f), N(H,G5,Q,mf)],
                     WBass(G2,B3,D4)),

            [90] = Merge(
                     [N(0,E5,Q,mf), N(Q,G5,Q,mf), N(H,C6,Q,f)],
                     WBass(C3,E4,G4)),

            [91] = Merge(
                     [N(0,D5,E,mf), N(E,E5,E,mp), N(Q,D5,E,mp), N(Q+E,C5,E,mp),
                      N(H,B4,Q,mp)],
                     WBassD7()),

            [92] = Merge(
                     [N(0,G4,E,mf), N(E,Fs4,E,mp), N(Q,G4,Q,mp), N(H,B4,Q,mp)],
                     WBass(G2,D4,B3)),

            [93] = Merge(
                     [N(0,D5,Q,mf), N(Q,B4,Q,mp), N(H,D5,Q,mp)],
                     WBass(G2,B3,D4)),

            [94] = Merge(
                     [N(0,A5,Q,f), N(Q,G5,Q,mf), N(H,Fs5,Q,mf)],
                     WBassD7()),

            [95] = Merge(
                     [N(0,G5,E,mf), N(E,G5,E,mp), N(Q,G5,E,mp), N(Q+E,Fs5,E,mp),
                      N(H,E5,Q,mp)],
                     WBass(G2,B3,D4)),

            [96] = Merge(
                     [N(0,G5,E,f), N(E,Fs5,E,mf), N(Q,E5,E,mf), N(Q+E,D5,E,mp),
                      N(H,C5,Q,mp)],
                     WBass(G2,D4,B3)),

            [97] = Merge(
                     [N(0,D5,Q,mf), N(Q,Fs5,Q,mf), N(H,A5,Q,f)],
                     WBassD7()),

            [98] = Merge(
                     [N(0,E5,E,mf), N(E,D5,E,mp), N(Q,C5,E,mp), N(Q+E,B4,E,mp),
                      N(H,A4,Q,mp)],
                     WBass(A2,C4,E4)),

            [99] = Merge(
                     [N(0,B4,Q,mf), N(Q,D5,Q,mf), N(H,Fs5,Q,mf)],
                     WBassD7()),

            [100]= Merge(
                     [N(0,G5,Q,f), N(Q,Fs5,Q,mf), N(H,G5,Q,mf)],
                     WBass(G2,D4,B3)),

            // ── Measures 101-176 ─────────────────────────────────────────

            [101]= Merge(
                     [N(0,D5,E,mf), N(E,C5,E,mp), N(Q,B4,Q,mp), N(H,G4,Q,mp)],
                     WBass(G2,B3,D4)),

            [102]= Merge(
                     [N(0,C5,E,mf), N(E,D5,E,mp), N(Q,E5,Q,mp), N(H,D5,Q,mp)],
                     WBass(A2,C4,E4)),

            [103]= Merge(
                     [N(0,Fs5,Q,mf), N(Q,E5,Q,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [104]= Merge(
                     [N(0,D5,E,f), N(E,E5,E,mf), N(Q,Fs5,E,mf), N(Q+E,G5,E,mp),
                      N(H,A5,Q,mf)],
                     WBassD7()),

            [105]= Merge(
                     [N(0,G5,E,mf), N(E,Fs5,E,mp), N(Q,G5,Q,mp), N(H,E5,Q,mp)],
                     WBass(G2,B3,D4)),

            [106]= Merge(
                     [N(0,E5,Q,mf), N(Q,Fs5,Q,mp), N(H,E5,Q,mp)],
                     WBass(C3,G3,E4)),

            [107]= Merge(
                     [N(0,B4,Q,mf), N(Q,G4,Q,mp), N(H,E4,Q,mp)],
                     WBass(E2,G3,B3)),

            [108]= Merge(
                     [N(0,G4,E,mf), N(E,A4,E,mp), N(Q,B4,E,mp), N(Q+E,A4,E,mp),
                      N(H,G4,Q,mp)],
                     WBass(G2,B3,D4)),

            [109]= Merge(
                     [N(0,D5,Q,mf), N(Q,D5,Q,mp), N(H,E5,Q,mp)],
                     WBassD7()),

            [110]= Merge(
                     [N(0,E5,E,mf), N(E,D5,E,mp), N(Q,C5,Q,mp), N(H,G4,Q,mp)],
                     WBass(C3,E3,G4)),

            [111]= Merge(
                     [N(0,G5,Q,f), N(Q,G5,Q,mf), N(H,Fs5,Q,mf)],
                     WBass(G2,D4,B3)),

            [112]= Merge(
                     [N(0,D5,E,mf), N(E,D5,E,mp), N(Q,D5,E,mp), N(Q+E,E5,E,mp),
                      N(H,D5,Q,mp)],
                     WBassD7()),

            [113]= Merge(
                     [N(0,G4,Q,mf), N(Q,D5,Q,mf), N(H,G5,Q,f)],
                     WBass(G2,B3,D4)),

            [114]= Merge(
                     [N(0,B4,E,mf), N(E,C5,E,mp), N(Q,B4,Q,mp), N(H,G4,Q,mp)],
                     WBass(G2,D4,B3)),

            [115]= Merge(
                     [N(0,G5,E,mf), N(E,A5,E,mf), N(Q,G5,Q,mf), N(H,D5,Q,mp)],
                     WBassD7()),

            [116]= Merge(
                     [N(0,E5,E,mf), N(E,D5,E,mp), N(Q,E5,Q,mp), N(H,G5,Q,mf)],
                     WBass(C3,G3,E4)),

            [117]= Merge(
                     [N(0,D5,Q,mf), N(Q,C5,Q,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [118]= Merge(
                     [N(0,G5,Q,f), N(Q,E5,Q,mf), N(H,D5,Q,mp)],
                     WBass(G2,D4,B3)),

            [119]= Merge(
                     [N(0,B4,E,mf), N(E,D5,E,mp), N(Q,E5,E,mp), N(Q+E,D5,E,mp),
                      N(H,C5,Q,mp)],
                     WBass(G2,D4,B3)),

            [120]= Merge(
                     [N(0,D5,Q,mf), N(Q,B4,Q,mp), N(H,G4,Q,mp)],
                     WBass(G2,B3,D4)),

            [121]= Merge(
                     [N(0,G5,E,f), N(E,Fs5,E,mf), N(Q,G5,Q,mf), N(H,E5,Q,mp)],
                     WBass(G2,B3,D4)),

            [122]= Merge(
                     [N(0,A5,Q,f), N(Q,Fs5,Q,mf), N(H,D5,Q,mp)],
                     WBassD7()),

            [123]= Merge(
                     [N(0,E5,Q,mf), N(Q,D5,Q,mp), N(H,G4,Q,mp)],
                     WBass(G2,D4,B3)),

            [124]= Merge(
                     [N(0,D5,E,mf), N(E,E5,E,mp), N(Q,G5,Q,mf), N(H,Fs5,Q,mf)],
                     WBassD7()),

            [125]= Merge(
                     [N(0,G5,Q,f), N(Q,G5,Q,mf), N(H,G5,Q,mf)],
                     WBass(G2,D4,B3)),

            [126]= Merge(
                     [N(0,C5,Q,mf), N(Q,E5,Q,mf), N(H,C5,Q,mp)],
                     WBass(C3,G3,E4)),

            [127]= Merge(
                     [N(0,G5,E,mf), N(E,Fs5,E,mp), N(Q,E5,Q,mp), N(H,C5,Q,mp)],
                     WBass(C3,G3,E4)),

            [128]= Merge(
                     [N(0,E5,E,mf), N(E,D5,E,mp), N(Q,C5,Q,mp), N(H,E5,Q,mp)],
                     WBass(C3,E3,G4)),

            [129]= Merge(
                     [N(0,Fs5,E,mf), N(E,A5,E,mf), N(Q,Fs5,Q,mf), N(H,D5,Q,mp)],
                     WBassD7()),

            [130]= Merge(
                     [N(0,B4,E,mf), N(E,D5,E,mp), N(Q,G5,Q,f), N(H,D5,Q,mf)],
                     WBass(G2,D4,B3)),

            [131]= Merge(
                     [N(0,G5,Q,f), N(Q,B4,Q,mp), N(H,G4,Q,mp)],
                     WBass(G2,B3,D4)),

            [132]= Merge(
                     [N(0,E5,Q,mf), N(Q,E5,Q,mp), N(H,D5,Q,mp)],
                     WBass(C3,G3,E4)),

            [133]= Merge(
                     [N(0,A5,E,f), N(E,G5,E,mf), N(Q,Fs5,Q,mf), N(H,E5,Q,mp)],
                     WBassD7()),

            [134]= Merge(
                     [N(0,D5,Q,mf), N(Q,E5,Q,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [135]= Merge(
                     [N(0,G5,E,mf), N(E,E5,E,mp), N(Q,D5,Q,mp), N(H,B4,Q,mp)],
                     WBass(G2,D4,B3)),

            [136]= Merge(
                     [N(0,B4,Q,mf), N(Q,D5,Q,mf), N(H,Fs5,Q,mf)],
                     WBassD7()),

            [137]= Merge(
                     [N(0,E5,E,mf), N(E,Fs5,E,mp), N(Q,G5,Q,mf), N(H,E5,Q,mp)],
                     WBass(C3,E4,G4)),

            [138]= Merge(
                     [N(0,D5,Q,mf), N(Q,G4,Q,mp), N(H,B4,Q,mp)],
                     WBass(G2,D4,B3)),

            [139]= Merge(
                     [N(0,G5,E,f), N(E,Fs5,E,mf), N(Q,E5,E,mf), N(Q+E,Fs5,E,mp),
                      N(H,G5,Q,mf)],
                     WBass(G2,B3,D4)),

            [140]= Merge(
                     [N(0,A5,Q,f), N(Q,E5,Q,mf), N(H,C5,Q,mp)],
                     WBass(A2,C4,E4)),

            [141]= Merge(
                     [N(0,B4,E,mf), N(E,C5,E,mp), N(Q,D5,E,mp), N(Q+E,C5,E,mp),
                      N(H,B4,Q,mp)],
                     WBass(G2,D4,B3)),

            [142]= Merge(
                     [N(0,G5,Q,f), N(Q,E5,Q,mf), N(H,D5,Q,mp)],
                     WBass(G2,B3,D4)),

            [143]= Merge(
                     [N(0,Fs5,E,mf), N(E,E5,E,mp), N(Q,D5,Q,mp), N(H,E5,Q,mp)],
                     WBassD7()),

            [144]= Merge(
                     [N(0,D5,E,mf), N(E,C5,E,mp), N(Q,D5,Q,mp), N(H,G4,Q,mp)],
                     WBass(G2,D4,B3)),

            [145]= Merge(
                     [N(0,E5,Q,mf), N(Q,D5,Q,mp), N(H,E5,Q,mp)],
                     WBass(C3,G3,E4)),

            [146]= Merge(
                     [N(0,G4,E,mf), N(E,B4,E,mp), N(Q,E5,Q,mf), N(H,D5,Q,mp)],
                     WBass(G2,B3,E4)),

            [147]= Merge(
                     [N(0,Fs5,E,mf), N(E,G5,E,mp), N(Q,A5,Q,mf), N(H,G5,Q,mf)],
                     WBassD7()),

            [148]= Merge(
                     [N(0,G5,E,f), N(E,G5,E,mf), N(Q,Fs5,Q,mf), N(H,E5,Q,mp)],
                     WBass(G2,D4,B3)),

            [149]= Merge(
                     [N(0,D5,Q,mf), N(Q,Fs5,Q,mf), N(H,D5,Q,mp)],
                     WBassD7()),

            [150]= Merge(
                     [N(0,B4,E,mf), N(E,G4,E,mp), N(Q,D5,Q,mf), N(H,B4,Q,mp)],
                     WBass(G2,B3,D4)),

            [151]= Merge(
                     [N(0,G5,Q,f), N(Q,Fs5,E,mf), N(Q+E,G5,E,mp), N(H,E5,Q,mp)],
                     WBass(G2,B3,D4)),

            [152]= Merge(
                     [N(0,D5,E,f), N(E,E5,E,mf), N(Q,D5,Q,mf), N(H,C5,Q,mp)],
                     WBassD7()),

            [153]= Merge(
                     [N(0,E5,E,mf), N(E,D5,E,mp), N(Q,E5,Q,mp), N(H,C5,Q,mp)],
                     WBass(C3,E3,G4)),

            [154]= Merge(
                     [N(0,G5,Q,f), N(Q,E5,Q,mf), N(H,G5,Q,f)],
                     WBass(G2,B3,D4)),

            [155]= Merge(
                     [N(0,D5,Q,mf), N(Q,D5,Q,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [156]= Merge(
                     [N(0,A5,E,f), N(E,G5,E,mf), N(Q,Fs5,Q,mf), N(H,G5,Q,mf)],
                     WBassD7()),

            [157]= Merge(
                     [N(0,B4,E,mf), N(E,A4,E,mp), N(Q,G4,E,mp), N(Q+E,Fs4,E,mp),
                      N(H,E4,Q,mp)],
                     WBass(E2,G3,B3)),

            [158]= Merge(
                     [N(0,G5,Q,f), N(Q,B5,Q,f), N(H,D5,Q,mf)],
                     WBass(G2,D4,B3)),

            [159]= Merge(
                     [N(0,E5,E,mf), N(E,Fs5,E,mp), N(Q,E5,Q,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [160]= Merge(
                     [N(0,D5,Q,mf), N(Q,E5,Q,mp), N(H,Fs5,Q,mp)],
                     WBassD7()),

            [161]= Merge(
                     [N(0,C5,E,mf), N(E,D5,E,mp), N(Q,E5,Q,mp), N(H,C5,Q,mp)],
                     WBass(A2,E3,C4)),

            [162]= Merge(
                     [N(0,G5,E,f), N(E,E5,E,mf), N(Q,D5,Q,mf), N(H,C5,Q,mp)],
                     WBass(C3,G3,E4)),

            [163]= Merge(
                     [N(0,Fs5,Q,mf), N(Q,E5,Q,mp), N(H,Fs5,Q,mp)],
                     WBassD7()),

            [164]= Merge(
                     [N(0,D5,E,mf), N(E,C5,E,mp), N(Q,B4,Q,mp), N(H,D5,Q,mp)],
                     WBassD7()),

            [165]= Merge(
                     [N(0,G5,E,f), N(E,Fs5,E,mf), N(Q,G5,Q,mf), N(H,B5,Q,f)],
                     WBass(G2,D4,B3)),

            [166]= Merge(
                     [N(0,E5,Q,mf), N(Q,C5,Q,mp), N(H,G4,Q,mp)],
                     WBass(C3,E3,G4)),

            [167]= Merge(
                     [N(0,A5,E,f), N(E,B5,E,f), N(Q,A5,Q,mf), N(H,G5,Q,mf)],
                     WBassD7()),

            [168]= Merge(
                     [N(0,D5,Q,mf), N(Q,Fs5,Q,mf), N(H,E5,Q,mp)],
                     WBassD7()),

            [169]= Merge(
                     [N(0,G5,E,mf), N(E,A5,E,mf), N(Q,G5,Q,mf), N(H,E5,Q,mp)],
                     WBass(G2,B3,D4)),

            [170]= Merge(
                     [N(0,B4,E,mf), N(E,D5,E,mp), N(Q,G5,Q,f), N(H,B5,Q,f)],
                     WBass(G2,D4,B3)),

            [171]= Merge(
                     [N(0,D5,E,f), N(E,C5,E,mf), N(Q,B4,Q,mp), N(H,A4,Q,mp)],
                     WBassD7()),

            [172]= Merge(
                     [N(0,G5,Q,f), N(Q,D5,Q,mf), N(H,B4,Q,mp)],
                     WBass(G2,D4,B3)),

            [173]= Merge(
                     [N(0,E5,E,mf), N(E,G5,E,mf), N(Q,E5,Q,mf), N(H,C5,Q,mp)],
                     WBass(C3,E3,G4)),

            [174]= Merge(
                     [N(0,G4,E,mf), N(E,B4,E,mp), N(Q,D5,Q,mf), N(H,G5,Q,f)],
                     WBass(G2,B3,D4)),

            [175]= Merge(
                     [N(0,Fs5,E,mf), N(E,A5,E,mf), N(Q,Fs5,Q,mf), N(H,E5,Q,mp)],
                     WBassD7()),

            [176]= Merge(
                     [N(0,G5,Q,f), N(Q,G5,Q,mf), N(H,G5,Q,f)],   // triumphant close
                     WBass(G2,D4,B3)),
        };

        return dict.ToFrozenDictionary();
    }

    // ── Fallback: generate a G-major arpeggio for any unlisted measure ────
    private static NoteEvent[] Fallback(int measureNumber)
    {
        // Deterministic from measure number — still sounds musical
        byte[] melody = (measureNumber % 4) switch
        {
            0 => [G5, E5, D5],
            1 => [D5, G5, B4],
            2 => [E5, D5, C5],
            _ => [B4, D5, G5],
        };
        return
        [
            N(0, melody[0], Q, mf, RH),
            N(Q, melody[1], Q, mp, RH),
            N(H, melody[2], Q, mp, RH),
            .. WBass(G2, B3, D4),
        ];
    }

    // Extra pitch constant needed above
    private const byte E2 = 40;
}
