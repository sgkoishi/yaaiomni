using Newtonsoft.Json;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
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
        public int MitigationCoinReduced;
        public int ModdedEarlyChatSpam;
        public int ModdedFakeName;
    }

    [Command("Admin.DebugStat", "_debugstat", Permission = "chireiden.omni.admin.debugstat")]
    private void Command_DebugStat(CommandArgs args)
    {
        args.Player.SendInfoMessage(JsonConvert.SerializeObject(this.Statistics, Formatting.Indented));
    }
}