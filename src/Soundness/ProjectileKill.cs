using Terraria;
using Terraria.ID;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private void MMHook_Soundness_ProjectileKill(On.Terraria.Projectile.orig_Kill orig, Projectile self)
    {
        if (this.config.Soundness.ProjectileKillMapEditRestriction)
        {
            if (self.type is ProjectileID.DirtBomb or
                ProjectileID.DirtStickyBomb)
            {
                self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                    self.Center.ToTileCoordinates(),
                    4.2f,
                    Utils.WithPermissionCheck(DelegateMethods.SpreadDirt, TShockAPI.TShock.Players[self.owner]));
                self.active = false;
                return;
            }
            if (self.type is ProjectileID.WetRocket or
                ProjectileID.WetGrenade or
                ProjectileID.WetMine or
                ProjectileID.WetSnowmanRocket or
                ProjectileID.WetBomb)
            {
                self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                    self.Center.ToTileCoordinates(),
                    3f,
                    Utils.WithPermissionCheck(DelegateMethods.SpreadWater, TShockAPI.TShock.Players[self.owner]));
                self.active = false;
                return;
            }
            if (self.type is ProjectileID.LavaRocket or
                ProjectileID.LavaGrenade or
                ProjectileID.LavaMine or
                ProjectileID.LavaSnowmanRocket or
                ProjectileID.LavaBomb)
            {
                self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                    self.Center.ToTileCoordinates(),
                    3f,
                    Utils.WithPermissionCheck(DelegateMethods.SpreadLava, TShockAPI.TShock.Players[self.owner]));
                self.active = false;
                return;
            }
            if (self.type is ProjectileID.HoneyRocket or
                ProjectileID.HoneyGrenade or
                ProjectileID.HoneyMine or
                ProjectileID.HoneySnowmanRocket or
                ProjectileID.HoneyBomb)
            {
                self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                    self.Center.ToTileCoordinates(),
                    3f,
                    Utils.WithPermissionCheck(DelegateMethods.SpreadHoney, TShockAPI.TShock.Players[self.owner]));
                self.active = false;
                return;
            }
            if (self.type is ProjectileID.DryRocket or
                ProjectileID.DryGrenade or
                ProjectileID.DryMine or
                ProjectileID.DrySnowmanRocket or
                ProjectileID.DryBomb)
            {
                self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                    self.Center.ToTileCoordinates(),
                    3.5f,
                    Utils.WithPermissionCheck(DelegateMethods.SpreadDry, TShockAPI.TShock.Players[self.owner]));
                self.active = false;
                return;
            }
        }
        orig(self);
    }
}