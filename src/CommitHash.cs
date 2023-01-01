using System.Reflection;

namespace Chireiden;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public class CommitHashAttribute : Attribute
{
    public string CommitHash { get; }
    public CommitHashAttribute(string value)
    {
        this.CommitHash = value;
    }

    public static string GetCommitHash()
    {
        var attr = Assembly.GetExecutingAssembly().GetCustomAttribute<CommitHashAttribute>();
        return attr?.CommitHash ?? string.Empty;
    }
}