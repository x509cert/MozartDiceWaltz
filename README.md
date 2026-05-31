# Mozart Dice Waltz

A .NET/C# console implementation of Mozart's *Musikalisches Wuerfelspiel* ("Musical Dice Game"), K. 516f / K. Anh. 294d. The program rolls two dice for each of 16 positions, looks up the matching measure in the historical dice table, then plays or exports a unique waltz as MIDI.

## Background

Wolfgang Amadeus Mozart (1756-1791) was one of the central composers of the Classical era, known for music that combines formal elegance, melodic invention, and extraordinary craftsmanship. His catalog includes symphonies, concertos, operas, chamber music, sacred works, dances, and keyboard pieces that remain foundational to Western music.

The *Musikalisches Wuerfelspiel* is a musical parlor game associated with Mozart and published after his death. Instead of composing every note from scratch, the game provides a table of pre-composed measures. Players roll dice, consult the table, and assemble a coherent minuet or waltz from the selected measures. Because each position has 11 possible outcomes and there are 16 positions, the game can produce 11^16 combinations--about 45 quadrillion possible waltzes.

That makes the work a fascinating early example of procedural music: a rule-based system that can generate many valid pieces while still preserving a composer's style. Long before computers made generative art common, games like this showed how chance, structure, and authorship could coexist.

## Personal note

I wrote a version of this when I was 20 years old in BASIC and Z80 assembly on a Sega video console. It took me two weeks to write. This version took about 20 minutes with AI.

## Features

- Rolls a complete 16-position dice-game waltz.
- Supports reproducible rolls with a seed.
- Plays live MIDI through the Windows General MIDI synthesizer via NAudio.
- Exports the generated waltz as a standard `.mid` file.
- Supports custom tempo and AABB repeat count.
- Prints the selected dice rolls and source measure numbers.

## Requirements

- Windows, for live MIDI playback through `NAudio.Midi.MidiOut`.
- .NET 8 SDK (or later).
- NAudio 2.2.1, restored automatically by `dotnet restore`.

For better piano sound, install a software MIDI synth such as VirtualMIDISynth and load a piano soundfont. The application prefers devices named VirtualMIDISynth, Salamander, or FluidSynth when present, and falls back to the Microsoft GS Wavetable Synth.

## Build

```powershell
dotnet restore
dotnet build
```

## Run

Play a random waltz:

```powershell
dotnet run
```

Show the dice rolls and selected measures without playing:

```powershell
dotnet run -- --list
```

Use a reproducible seed:

```powershell
dotnet run -- --seed 42
```

Set the tempo:

```powershell
dotnet run -- --tempo 120
```

Export a MIDI file:

```powershell
dotnet run -- --midi waltz.mid
```

Combine options:

```powershell
dotnet run -- --seed 42 --tempo 120 --repeats 2 --midi waltz.mid
```

## How it works

1. The program rolls two six-sided dice for each of the 16 musical positions.
2. The sum of each roll selects a row in the historical dice table.
3. The current musical position selects the column.
4. The table cell identifies the measure number to use.
5. The selected measures are stitched together in AABB form and rendered as MIDI notes.

The dice sums are not uniformly distributed: 7 is the most likely result, while 2 and 12 are rare. That probability curve is part of the character of the generated pieces.

## Project structure

| File | Purpose |
| --- | --- |
| `Program.cs` | Command-line entry point, option handling, display, playback/export flow. |
| `DiceTable.cs` | Historical 2d6 lookup table for selecting measures. |
| `MeasureLibrary.cs` | MIDI note data for the measure catalog. |
| `WaltzGame.cs` | Orchestrates rolling, playback, and MIDI export. |
| `MidiPlayer.cs` | Real-time MIDI playback using NAudio. |
| `MidiFileWriter.cs` | Standard MIDI file generation. |
| `Models.cs` | Shared records and note-event types. |

## Notes

The historical attribution and cataloging of the dice game have been discussed in different sources over time. This project follows the common association with Mozart's K. 516f / K. Anh. 294d and focuses on making the musical idea playable and exportable from a modern console application.
