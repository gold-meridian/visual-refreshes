using System.Reflection;
using Daybreak.Common.Features.Hooks;

namespace Refreshes.Content;

[Autoload(Side = ModSide.Client)]
internal static class SandstormInABottle
{
    [OnLoad]
    private static void ApplyHooks()
    {
        MonoModHooks.Add(
            typeof(SandstormInABottleJump).GetMethod("OnStarted", BindingFlags.Public | BindingFlags.Instance)!,
            OnStarted_SpawnOurSandstormParticles
        );
    }

    private static void OnStarted_SpawnOurSandstormParticles(SandstormInABottleJump _, Player player, ref bool playSound)
    {

    }

    [ModPlayerHooks.CanShowExtraJumpVisuals]
    public static bool CancelVanillaVisuals(ExtraJump jump)
    {
        return jump != ExtraJump.SandstormInABottle;
    }
}