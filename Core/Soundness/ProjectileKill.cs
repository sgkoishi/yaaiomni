using Terraria;
using Terraria.ID;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private void MMHook_Soundness_ProjectileKill(On.Terraria.Projectile.orig_Kill orig, Projectile self)
    {
        if (this.config.Soundness.Value.ProjectileKillMapEditRestriction)
        {
            switch (self.type)
            {
                case ProjectileID.DirtBomb:
                case ProjectileID.DirtStickyBomb:
                    self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                        self.Center.ToTileCoordinates(), 4.2f,
                    Utils.WithPermissionCheck(DelegateMethods.SpreadDirt, TShockAPI.TShock.Players[self.owner]));
                    self.active = false;
                    return;
                case ProjectileID.WetRocket:
                case ProjectileID.WetGrenade:
                case ProjectileID.WetMine:
                case ProjectileID.WetSnowmanRocket:
                case ProjectileID.WetBomb:
                    self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                        self.Center.ToTileCoordinates(), 3f,
                    Utils.WithPermissionCheck(DelegateMethods.SpreadWater, TShockAPI.TShock.Players[self.owner]));
                    self.active = false;
                    return;
                case ProjectileID.LavaRocket:
                case ProjectileID.LavaGrenade:
                case ProjectileID.LavaMine:
                case ProjectileID.LavaSnowmanRocket:
                case ProjectileID.LavaBomb:
                    self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                        self.Center.ToTileCoordinates(), 3f,
                    Utils.WithPermissionCheck(DelegateMethods.SpreadLava, TShockAPI.TShock.Players[self.owner]));
                    self.active = false;
                    return;
                case ProjectileID.HoneyRocket:
                case ProjectileID.HoneyGrenade:
                case ProjectileID.HoneyMine:
                case ProjectileID.HoneySnowmanRocket:
                case ProjectileID.HoneyBomb:
                    self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                        self.Center.ToTileCoordinates(), 3f,
                        Utils.WithPermissionCheck(DelegateMethods.SpreadHoney, TShockAPI.TShock.Players[self.owner]));
                    self.active = false;
                    return;
                case ProjectileID.DryRocket:
                case ProjectileID.DryGrenade:
                case ProjectileID.DryMine:
                case ProjectileID.DrySnowmanRocket:
                case ProjectileID.DryBomb:
                    self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                        self.Center.ToTileCoordinates(), 3.5f,
                    Utils.WithPermissionCheck(DelegateMethods.SpreadDry, TShockAPI.TShock.Players[self.owner]));
                    self.active = false;
                    return;
            }
        }
        orig(self);
    }
}