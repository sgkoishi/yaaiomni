using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void Hook_Lava_KillTile(On.Terraria.WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem)
    {
        var shouldHandle = false;
        var strtg = this.config.LavaHandler;
        if (strtg.Enabled)
        {
            if (Terraria.Main.tile[i, j].type == Terraria.ID.TileID.Hellstone && !strtg.AllowHellstone)
            {
                shouldHandle = true;
            }
            else if (Terraria.Main.tile[i, j].type == Terraria.ID.TileID.CrispyHoneyBlock && !strtg.AllowCrispyHoneyBlock)
            {
                shouldHandle = true;
            }
        }
        orig(i, j, fail, effectOnly, noItem);
        if (shouldHandle)
        {
            if (Terraria.Main.tile[i, j].liquid == 128 && Terraria.Main.tile[i, j].lava())
            {
                Terraria.Main.tile[i, j].liquid = 0;
                Terraria.Main.tile[i, j].lava(false);
            }
        }
    }

    private void Hook_Lava_HitEffect(On.Terraria.NPC.orig_HitEffect orig, Terraria.NPC self, int hitDirection, double dmg)
    {
        var shouldHandle = false;
        var strtg = this.config.LavaHandler;
        if (strtg.Enabled)
        {
            if (self.type == Terraria.ID.NPCID.LavaSlime && !strtg.AllowLavaSlime)
            {
                shouldHandle = true;
            }
            else if (self.type == Terraria.ID.NPCID.Lavabat && !strtg.AllowLavabat)
            {
                shouldHandle = true;
            }
            else if (self.type == Terraria.ID.NPCID.Hellbat && !strtg.AllowHellbat)
            {
                shouldHandle = true;
            }
        }
        orig(self, hitDirection, dmg);
        if (shouldHandle)
        {
            var i = (int) self.Center.X / 16;
            var j = (int) self.Center.Y / 16;
            if (Terraria.Main.tile[i, j].liquid >= 50 && Terraria.Main.tile[i, j].lava())
            {
                Terraria.Main.tile[i, j].liquid = 0;
                Terraria.Main.tile[i, j].lava(false);
            }
        }
    }
}
