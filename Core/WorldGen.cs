using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    [Command("Admin.TryTileFrame", "trytileframe", Permission = "chireiden.omni.admin.trytileframe")]
    private void Command_TryTileFrame(CommandArgs args)
    {
        var granularityX = 1000;
        var granularityY = 1000;
        var startX = 0;
        var startY = 0;
        if (args.Parameters.Count > 0)
        {
            if (int.TryParse(args.Parameters[0], out var x))
            {
                startX = x;
                while (x % granularityX != 0)
                {
                    granularityX /= 10;
                }
            }
            if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out var y))
            {
                startY = y;
                while (y % granularityY != 0)
                {
                    granularityY /= 10;
                }
            }
        }
        args.Player.SendInfoMessage($"Testing tile frame {startX},{startY}, granularity {granularityX},{granularityY}");
        args.Player.SendInfoMessage("You may experience lag.");
        for (var i = startX; i < Terraria.Main.maxTilesX; i++)
        {
            if (i % granularityX == 0)
            {
                args.Player.SendInfoMessage($"Testing tile frame {i}...");
            }
            var start = i == startX ? startY : 0;
            for (var j = start; j < Terraria.Main.maxTilesY; j++)
            {
                if (j % granularityY == 0)
                {
                    args.Player.SendInfoMessage($"Testing tile frame {i},{j}...");
                }
                Terraria.WorldGen.TileFrame(i, j);
            }
        }
        args.Player.SendInfoMessage("TileFrame completed without error.");
    }

    private bool _inspectTileFrame = false;
    [Command("Admin.InspectTileFrame", "inspecttileframe", Permission = "chireiden.omni.admin.inspecttileframe",
        HelpText = "DO NOT USE UNLESS YOU KNOW WHAT YOU ARE DOING")]
    private void Command_InspectTileFrame(CommandArgs args)
    {
        if (this._inspectTileFrame)
        {
            args.Player.SendErrorMessage("Already inspecting tile frame.");
            return;
        }

        this._inspectTileFrame = true;
        this.Detour(
            nameof(this.Detour_InspectTileFrame),
            typeof(Terraria.WorldGen)
                .GetMethod(nameof(Terraria.WorldGen.TileFrame), _bfany),
            this.Detour_InspectTileFrame);
        args.Player.SendInfoMessage("Inspecting tile frame, you may experience lag.");
    }

    private AsyncLocal<int> _frameCount = new AsyncLocal<int>();
    private bool _worldgenHalting = false;
    private HashSet<ulong> _haltSource = new HashSet<ulong>();
    private int _dumpCounter;

    private void Detour_InspectTileFrame(Action<int, int, bool, bool> orig, int i, int j, bool resetFrame, bool noBreak)
    {
        var frames = this._frameCount.Value;
        if (this._worldgenHalting)
        {
            if (frames > 3)
            {
                return;
            }
            else
            {
                this._worldgenHalting = false;
                var mtg = this.config.Mitigation.Value;
                if (!mtg.DisableAllMitigation)
                {
                    if (mtg.DumpMapOnStackOverflowWorldGen)
                    {
                        using var ms = new MemoryStream();
                        using var bw = new BinaryWriter(ms);
                        {
                            Terraria.IO.WorldFile.SaveWorld_Version2(bw);
                        }
                        File.WriteAllBytes(Path.Combine(TShockAPI.TShock.SavePath, $"dumpmap_{DateTime.UtcNow:yyyyMMddHHmmss}_{this._dumpCounter++}.wld"), ms.ToArray());
                        TShockAPI.TShock.Log.ConsoleError($"Detour_InspectTileFrame: Dump crashing map to dumpmap.wld.bak ({TShockAPI.TShock.SavePath})");
                    }
                    if (mtg.ClearOverflowWorldGenStackTrace)
                    {
                        foreach (var item in this._haltSource)
                        {
                            var ti = (int) (item >> 32);
                            var tj = (int) item;
                            var array = new byte[TerrariaApi.Server.HeapTile.kHeapTileSize];
                            var ht = new TerrariaApi.Server.HeapTile(array, 0, 0);
                            ht.CopyFrom(Terraria.Main.tile[ti, tj]);
                            TShockAPI.TShock.Log.ConsoleError($"Detour_InspectTileFrame: Clearing tile {ti},{tj} (content: {Convert.ToHexString(array)})");
                            Terraria.Main.tile[ti, tj].Clear(Terraria.DataStructures.TileDataType.All);
                        }
                        this._haltSource.Clear();
                    }
                }
            }
        }

        const int WARN_LIMIT = 130;
        if (frames >= WARN_LIMIT)
        {
            TShockAPI.TShock.Log.ConsoleDebug($"Detour_InspectTileFrame: {frames} frames at {i}, {j}");

            const int HALT_LIMIT = 150;
            if (frames == HALT_LIMIT)
            {
                this._haltSource.Add((((ulong) i) << 32) | ((uint) j));
            }
            else if (frames > HALT_LIMIT)
            {
                TShockAPI.TShock.Log.ConsoleError($"Detour_InspectTileFrame: {frames} frames with source of {string.Join(", ", this._haltSource)}.");
                this._worldgenHalting = true;
                var st = new System.Diagnostics.StackTrace(false);
                TShockAPI.TShock.Log.ConsoleDebug($"Detour_InspectTileFrame Trace: {st}");
                return;
            }
        }

        this._frameCount.Value += 1;
        orig(i, j, resetFrame, noBreak);
        this._frameCount.Value -= 1;
    }
}