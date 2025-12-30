using Daybreak.Common.Features.Hooks;

namespace Refreshes.Content;

[Autoload(Side = ModSide.Client)]
internal sealed class RunningBoots
{
    [OnLoad]
    private static void ApplyHooks()
    {
        On_Player.RocketBootVisuals += On_PlayerOnRocketBootVisuals_UseCustomParticles;
    }

    private static void On_PlayerOnRocketBootVisuals_UseCustomParticles(On_Player.orig_RocketBootVisuals orig, Player self)
    {
        var isRocketBoots = self.rocketBoots == 1;
        var isSpectreBoots = self.rocketBoots == 2;
    }
}