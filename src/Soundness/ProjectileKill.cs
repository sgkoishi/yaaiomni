using Terraria.ID;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using static Terraria.Utils;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private static TileActionAttempt WithPermissionCheck(TileActionAttempt action, TSPlayer player)
    {
        return (x, y) => player.HasBuildPermission(x, y) && action(x, y);
    }

    private void Soundness_ProjectileKill(On.Terraria.Projectile.orig_Kill orig, Projectile self)
    {
        if (this.config.Soundness.ProjectileKillMapEditRestriction)
        {
            if (self.type == ProjectileID.DirtBomb ||
                self.type == ProjectileID.DirtStickyBomb)
            {
                self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                    self.Center.ToTileCoordinates(),
                    3f,
                    WithPermissionCheck(DelegateMethods.SpreadDirt, TShockAPI.TShock.Players[self.owner]));
                self.active = false;
                return;
            }
            if (self.type == ProjectileID.WetRocket ||
                self.type == ProjectileID.WetGrenade ||
                self.type == ProjectileID.WetMine ||
                self.type == ProjectileID.WetSnowmanRocket ||
                self.type == ProjectileID.WetBomb)
            {
                self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                    self.Center.ToTileCoordinates(),
                    3f,
                    WithPermissionCheck(DelegateMethods.SpreadWater, TShockAPI.TShock.Players[self.owner]));
                self.active = false;
                return;
            }
            if (self.type == ProjectileID.LavaRocket ||
                self.type == ProjectileID.LavaGrenade ||
                self.type == ProjectileID.LavaMine ||
                self.type == ProjectileID.LavaSnowmanRocket ||
                self.type == ProjectileID.LavaBomb)
            {
                self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                    self.Center.ToTileCoordinates(),
                    3f,
                    WithPermissionCheck(DelegateMethods.SpreadLava, TShockAPI.TShock.Players[self.owner]));
                self.active = false;
                return;
            }
            if (self.type == ProjectileID.HoneyRocket ||
                self.type == ProjectileID.HoneyGrenade ||
                self.type == ProjectileID.HoneyMine ||
                self.type == ProjectileID.HoneySnowmanRocket ||
                self.type == ProjectileID.HoneyBomb)
            {
                self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                    self.Center.ToTileCoordinates(),
                    3f,
                    WithPermissionCheck(DelegateMethods.SpreadHoney, TShockAPI.TShock.Players[self.owner]));
                self.active = false;
                return;
            }
            if (self.type == ProjectileID.DryRocket ||
                self.type == ProjectileID.DryGrenade ||
                self.type == ProjectileID.DryMine ||
                self.type == ProjectileID.DrySnowmanRocket ||
                self.type == ProjectileID.DryBomb)
            {
                self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
                    self.Center.ToTileCoordinates(),
                    3.5f,
                    WithPermissionCheck(DelegateMethods.SpreadDry, TShockAPI.TShock.Players[self.owner]));
                self.active = false;
                return;
            }
        }
        orig(self);
    }
}
