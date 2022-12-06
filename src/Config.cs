﻿namespace Chireiden.TShock.Omni;

public class Config
{
    public bool SyncVersion = true;
    public bool TrimMemory = true;
    public bool ShowConfig = true;
    public UpdateOptions SuppressUpdate = UpdateOptions.Silent;
    public DebugPacket DebugPacket = new DebugPacket();
    public SoundnessFix Soundness = new SoundnessFix();
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