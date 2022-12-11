namespace Chireiden.TShock.Omni;

public class Unreachable : Exception
{
    public Unreachable()
    {
    }

    public Unreachable(string? message) : base(message)
    {
    }

    public Unreachable(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}