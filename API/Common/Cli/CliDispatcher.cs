using System.Reflection;

namespace API.Common.Cli;

/// <summary>
/// Discovers and dispatches CLI commands registered in the assembly.
/// Usage: dotnet run -- cli <command> [args...]
/// </summary>
public class CliDispatcher(IServiceProvider serviceProvider)
{
    private readonly Dictionary<string, Type> _commands = new(StringComparer.OrdinalIgnoreCase);

    public CliDispatcher RegisterFromAssembly(Assembly assembly)
    {
        var commandTypes = assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(CliCommand)) && !t.IsAbstract && t.GetCustomAttribute<CommandAttribute>() is not null);

        foreach (var type in commandTypes)
        {
            var attr = type.GetCustomAttribute<CommandAttribute>()!;
            var groupAttr = type.GetCustomAttribute<CommandGroupAttribute>();
            var key = groupAttr is not null ? $"{groupAttr.Group}:{attr.Name}" : attr.Name;
            _commands[key] = type;
        }

        return this;
    }

    public async Task<int> RunAsync(string[] args, CancellationToken ct = default)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run -- cli <command> [args...]");
            Console.WriteLine();
            Console.WriteLine("Available commands:");
            foreach (var (name, type) in _commands.OrderBy(x => x.Key))
            {
                var attr = type.GetCustomAttribute<CommandAttribute>()!;
                Console.WriteLine($"  {name,-35} {attr.Description}");
            }
            return 0;
        }

        var commandName = args[0];
        if (!_commands.TryGetValue(commandName, out var commandType))
        {
            Console.Error.WriteLine($"Unknown command: {commandName}");
            return 1;
        }

        using var scope = serviceProvider.CreateScope();
        var command = (CliCommand)ActivatorUtilities.CreateInstance(scope.ServiceProvider, commandType);
        await command.ExecuteAsync(args[1..], ct);
        return 0;
    }
}
