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
        public int MitigationSlotPE;
        public int MitigationSlotPEAllowed;
        public int MitigationRejectedChat;
        public int MitigationRejectedConnection;
        public int MitigationRejectedSwapWhileUse;
        public int MitigationRejectedSicknessHeal;
        public int MitigationTerminatedConnection;
    }

    private void Command_DebugStat(CommandArgs args)
    {
        args.Player.SendInfoMessage(JsonConvert.SerializeObject(this.Statistics, Formatting.Indented));
    }
}
