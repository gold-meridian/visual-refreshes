namespace Refreshes.Common;

/// <summary>
///     An enumeration containing every vanilla tree style ID.  These correspond
///     to branches and tree tops.
/// </summary>
public enum VanillaTreeStyle
{
    Forest1 = 0,
    Forest2 = 6,
    Forest3 = 7,
    Forest4 = 8,
    Forest5 = 9,
    Forest6 = 10,

    Boreal1 = 4,
    Boreal2 = 12,
    Boreal3 = 16,
    Boreal4 = 17,
    Boreal5 = 18,

    Jungle1 = 2,
    Jungle2 = 11,
    Jungle3 = 13,

    Corruption1 = 1,

    Crimson1 = 5,

    Hallow1 = 3,
    Hallow2 = 19,
    Hallow3 = 20,

    GlowingMushroom1 = 14,

    // Beach and Oasis have frames for evil/hallow variants, too.
    Beach1 = 15,
    Oasis = 21,

    Topaz1 = 22,
    Amethyst1 = 23,
    Sapphire1 = 24,
    Emerald1 = 25,
    Ruby1 = 26,
    Diamond1 = 27,
    Amber1 = 28,

    VanitySakura1 = 29,
    VanityWillow1 = 30,

    Ash1 = 31,

    Count = 32,
}

/// <summary>
///     Common groupings of <see cref="VanillaTreeStyle" />s.
/// </summary>
public static class VanillaTreeStyles
{
    public static VanillaTreeStyle[] Forest { get; } =
    [
        VanillaTreeStyle.Forest1,
        VanillaTreeStyle.Forest2,
        VanillaTreeStyle.Forest3,
        VanillaTreeStyle.Forest4,
        VanillaTreeStyle.Forest5,
        VanillaTreeStyle.Forest6,
    ];

    public static VanillaTreeStyle[] Boreal { get; } =
    [
        VanillaTreeStyle.Boreal1,
        VanillaTreeStyle.Boreal2,
        VanillaTreeStyle.Boreal3,
        VanillaTreeStyle.Boreal4,
        VanillaTreeStyle.Boreal5,
    ];

    public static VanillaTreeStyle[] Jungle { get; } =
    [
        VanillaTreeStyle.Jungle1,
        VanillaTreeStyle.Jungle2,
        VanillaTreeStyle.Jungle3,
    ];

    public static VanillaTreeStyle[] Corruption { get; } =
    [
        VanillaTreeStyle.Corruption1,
    ];

    public static VanillaTreeStyle[] Crimson { get; } =
    [
        VanillaTreeStyle.Crimson1,
    ];

    public static VanillaTreeStyle[] Hallow { get; } =
    [
        VanillaTreeStyle.Hallow1,
        VanillaTreeStyle.Hallow2,
        VanillaTreeStyle.Hallow3,
    ];

    public static VanillaTreeStyle[] GlowingMushroom { get; } =
    [
        VanillaTreeStyle.GlowingMushroom1,
    ];

    public static VanillaTreeStyle[] Beach { get; } =
    [
        VanillaTreeStyle.Beach1,
    ];

    public static VanillaTreeStyle[] Oasis { get; } =
    [
        VanillaTreeStyle.Oasis,
    ];

    public static VanillaTreeStyle[] Gem { get; } =
    [
        VanillaTreeStyle.Topaz1,
        VanillaTreeStyle.Amethyst1,
        VanillaTreeStyle.Sapphire1,
        VanillaTreeStyle.Emerald1,
        VanillaTreeStyle.Ruby1,
        VanillaTreeStyle.Diamond1,
        VanillaTreeStyle.Amber1,
    ];

    public static VanillaTreeStyle[] Vanity { get; } =
    [
        VanillaTreeStyle.VanitySakura1,
        VanillaTreeStyle.VanityWillow1,
    ];

    public static VanillaTreeStyle[] Ash { get; } =
    [
        VanillaTreeStyle.Ash1,
    ];
}
