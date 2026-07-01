namespace API.Common.Cli;

/// <summary>
/// Base class for CLI commands. Discovered via reflection and registered
/// by their <see cref="CommandAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CommandAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
}

/// <summary>
/// Parameter to a CLI command. Positional arguments are passed in order;
/// named arguments use --key value syntax.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ArgAttribute(int position = -1, string? flag = null) : Attribute
{
    public int Position { get; } = position;
    public string? Flag { get; } = flag;
}

/// <summary>
/// Implemented by classes that represent invocable CLI commands.
/// </summary>
public abstract class CliCommand
{
    public abstract Task ExecuteAsync(string[] args, CancellationToken ct);
}

/// <summary>
/// Attribute applied to CLI commands to set their sub-command grouping,
/// mirroring errandigo's @Command({ command: "seed:roles" }) pattern.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CommandGroupAttribute(string group) : Attribute
{
    public string Group { get; } = group;
}
