namespace MozartDiceWaltz;

/// <summary>
/// The original Imbault table (Paris, ~1792) for K. 516f / K. Anh. 294d.
/// Rows  = dice sum 2-12 (index = sum - 2, so index 0 = sum of 2)
/// Cols  = measure position 1-16 (index = position - 1)
/// Value = pre-composed measure number from Mozart's catalogue (1-176)
///
/// Probability distribution of sums with 2d6:
///   Sum=7 has prob 6/36=16.7% (most likely), sums 2 and 12 each 1/36=2.8%.
///   The waltz has 11^16 ≈ 4.5×10^16 possible compositions.
/// </summary>
static class DiceTable
{
    public static readonly int[,] Minuet =
    {
        //  pos: 1    2    3    4    5    6    7    8    9   10   11   12   13   14   15   16
        {   96,  22, 141,  41, 105, 122,  11,  30,  70, 121,  26,   9, 112,  49, 109,  14 }, // sum=2
        {   32,   6, 128,  63, 146,  46, 134,  81, 117,  39, 126,  56, 174,  18, 116,  83 }, // sum=3
        {   69,  95, 158,  13, 153,  55, 110,  24,  66, 139,  15, 132,  73,  58, 145,  79 }, // sum=4
        {   40,  17, 113,  85, 161,   2, 159, 100,  90, 176,   7,  34,  67, 160,  52, 170 }, // sum=5
        {  148,  74, 163,  45,  80,  97,  36, 107,  25, 143,  64, 125,  76, 136,   1,  93 }, // sum=6
        {  104, 157,  27, 167, 154,  68, 118,  91, 138,  71, 150,  29, 101, 162,  23, 151 }, // sum=7
        {  152,  60, 171,  53,  99, 133,  21, 127,  16, 155,  57, 175,  43, 168,  89, 172 }, // sum=8
        {  119,  84, 114,  50, 140,  86, 169,  94, 120,  88,  48, 166,  51, 115,  72, 111 }, // sum=9
        {   98, 142,  42, 156,  75, 129,  62, 123,  65,  77,  19,  82, 137,  38, 149,   8 }, // sum=10
        {    3,  87, 165,  61, 135,  47, 147,  33, 102,   4,  31, 164, 144,  59, 173,  78 }, // sum=11
        {   54, 130,  10, 103,  28,  37, 106,   5,  35,  20, 108,  92,  12, 124,  44, 131 }, // sum=12
    };

    /// <summary>Roll 2d6, return the selected measure number for the given position (0-based).</summary>
    public static int RollPosition(Random rng, int positionIndex)
    {
        int d1  = rng.Next(1, 7);
        int d2  = rng.Next(1, 7);
        int row = d1 + d2 - 2; // map sum 2-12 → index 0-10
        return Minuet[row, positionIndex];
    }

    /// <summary>Perform a full 16-position dice game, returning the roll record.</summary>
    public static WaltzRoll PerformGame(Random rng)
    {
        var details = Enumerable.Range(0, 16)
            .Select(pos =>
            {
                int d1  = rng.Next(1, 7);
                int d2  = rng.Next(1, 7);
                int row = d1 + d2 - 2;
                return (d1, d2, Minuet[row, pos]);
            })
            .ToList();

        return new WaltzRoll(details);
    }
}
