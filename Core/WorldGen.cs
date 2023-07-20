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
        for (int i = startX; i < Terraria.Main.maxTilesX; i++)
        {
            if (i % granularityX == 0)
            {
                args.Player.SendInfoMessage($"Testing tile frame {i}...");
            }
            var start = i == startX ? startY : 0;
            for (int j = start; j < Terraria.Main.maxTilesY; j++)
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

    private class Rect
    {
        public int x1;
        public int x2;
        public int y1;
        public int y2;
    }

    private List<Rect> _tileBlocks = new List<Rect>();

#if USE_ASYNCLOCAL_STACK_COUNT
    private AsyncLocal<int> _frameCount = new AsyncLocal<int>();
#endif

    private void Detour_InspectTileFrame(Action<int, int, bool, bool> orig, int i, int j, bool resetFrame, bool noBreak)
    {
#if USE_ASYNCLOCAL_STACK_COUNT
        var frames = this._frameCount.Value;
        const int LIMIT = 100;
#else
        var st = new System.Diagnostics.StackTrace(false);
        var frames = st.FrameCount;
        const int LIMIT = 400;
#endif
        // Would be better to count the frames of TileFrame, but this is already too slow
        if (frames >= LIMIT)
        {
            TShockAPI.TShock.Log.ConsoleInfo($"Detour_InspectTileFrame: {frames} frames at {i}, {j}");

            var included = false;
            foreach (var block in this._tileBlocks)
            {
                if (block.x1 <= i && block.x2 >= i && block.y1 <= j && block.y2 >= j)
                {
                    included = true;
                    block.x1 = Math.Min(block.x1, i - 10);
                    block.x2 = Math.Max(block.x2, i + 10);
                    block.y1 = Math.Min(block.y1, j - 10);
                    block.y2 = Math.Max(block.y2, j + 10);
                }
            }

            if (!included)
            {
                this._tileBlocks.Add(new Rect { x1 = i - 10, x2 = i + 10, y1 = j - 10, y2 = j + 10 });
            }

            if (frames > LIMIT * 2)
            {
                TShockAPI.TShock.Log.ConsoleError($"Detour_InspectTileFrame: {frames} frames, recorded {this._tileBlocks.Count} blocks.");
                foreach (var block in this._tileBlocks)
                {
                    TShockAPI.TShock.Log.ConsoleError($"Detour_InspectTileFrame: {block.x1}, {block.x2}, {block.y1}, {block.y2}");
                }
                return;
            }
        }

#if USE_ASYNCLOCAL_STACK_COUNT
        this._frameCount.Value += 1;
        orig(i, j, resetFrame, noBreak);
        this._frameCount.Value -= 1;
#else
        orig(i, j, resetFrame, noBreak);
#endif
    }
}