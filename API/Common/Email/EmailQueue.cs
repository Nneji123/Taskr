using System.Threading.Channels;

namespace API.Common.Email;

/// <summary>A unit of work to be sent by the background email worker.</summary>
public record EmailQueueEntry(string To, string Subject, string TemplatePath, Dictionary<string, string> Variables);

/// <summary>Enqueues email jobs for background processing.</summary>
public interface IEmailQueue
{
    void Enqueue(EmailQueueEntry entry);
    ValueTask EnqueueAsync(EmailQueueEntry entry, CancellationToken ct = default);
    ChannelReader<EmailQueueEntry> Reader { get; }
}

public class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailQueueEntry> _channel = Channel.CreateBounded<EmailQueueEntry>(new BoundedChannelOptions(200)
    {
        FullMode = BoundedChannelFullMode.DropOldest
    });

    public void Enqueue(EmailQueueEntry entry)
    {
        _channel.Writer.TryWrite(entry);
    }

    public async ValueTask EnqueueAsync(EmailQueueEntry entry, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(entry, ct);
    }

    public ChannelReader<EmailQueueEntry> Reader => _channel.Reader;
}
