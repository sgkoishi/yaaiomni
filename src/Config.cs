public class Config
{
    public bool SyncVersion = true;
    public bool TrimMemory = true;
    public UpdateOptions SuppressUpdate = UpdateOptions.Silent;
}

public enum UpdateOptions
{
    Silent,
    Disabled,
    Default
}