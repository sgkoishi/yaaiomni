using Newtonsoft.Json;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public DebugStat Statistics { get; } = new();
    public class DebugStat
    {
        public string CommitHash => CommitHashAttribute.GetCommitHash();
        public int MitigationSlotPE { get; set; }
        public int MitigationSlotPEAllowed { get; set; }
        public int MitigationRejectedChat { get; set; }
        public int MitigationRejectedConnection { get; set; }
        public int MitigationRejectedSwapWhileUse { get; set; }
        public int MitigationRejectedSicknessHeal { get; set; }
    }

    private void Command_DebugStat(CommandArgs args)
    {
        args.Player.SendInfoMessage(JsonConvert.SerializeObject(this.Statistics, Formatting.Indented));
    }
}
