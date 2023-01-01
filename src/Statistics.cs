using Newtonsoft.Json;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public DebugStat Statistics { get; } = new();
    public class DebugStat
    {
        public string CommitHash => Chireiden.CommitHashAttribute.GetCommitHash();
        public int MitigationSlotPE { get; set; }
        public int MitigationSlotPEAllowed { get; set; }
    }

    private void DebugStatCommand(CommandArgs args)
    {
        args.Player.SendInfoMessage(JsonConvert.SerializeObject(this.Statistics, Formatting.Indented));
    }
}
