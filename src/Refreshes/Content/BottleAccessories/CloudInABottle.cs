using Daybreak.Common.Features.Hooks;
using MonoMod.RuntimeDetour;

namespace Refreshes.Content.BottleAccessories;

internal sealed class CloudInABottleModifications {
    //cancels out dust trail
    [ModPlayerHooks.CanShowExtraJumpVisuals]
    public static bool CancelVanillaVisuals(ExtraJump jump) => jump != ExtraJump.CloudInABottle;
    
    private static Hook _hook = null!;
    private delegate void orig_OnStarted(CloudInABottleJump self, Player player, ref bool playSound);

    //cancels out initial cloud puff
    [OnLoad]
    private static void HookCloud() => _hook = new Hook(typeof(CloudInABottleJump).GetMethod("OnStarted")!, (orig_OnStarted _, CloudInABottleJump _, Player _, ref bool _) => { });
    
    [OnUnload]
    private static void Unhook() => _hook.Dispose();
}