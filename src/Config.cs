namespace Chireiden.TShock.Omni;

public class Config
{
    public bool SyncVersion = true;
    public bool TrimMemory = true;
    public bool ShowConfig = true;
    public string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    public UpdateOptions SuppressUpdate = UpdateOptions.Silent;
    public DebugPacket DebugPacket = new ();
    public SoundnessFix Soundness = new ();
    public PermissionSettings Permission = new ();
}

public enum UpdateOptions
{
    Silent,
    Disabled,
    Default
}

public class DebugPacket
{
    public bool In = false;
    public bool Out = false;
}

public class SoundnessFix
{
    public bool ProjectileKillMapEditRestriction = true;
}

public class PermissionSettings
{
    public PermissionLogSettings Log = new ();
}

public class PermissionLogSettings
{
    public bool DoLog = true;
    public int LogCount = 50;
    public bool LogDuplicate = false;
    public double LogDistinctTime = 1;
    public bool LogStackTrace = false;
}