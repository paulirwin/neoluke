using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace NeoLuke.Services;

/// <summary>
/// Message types for cross-component communication
/// </summary>

/// <summary>
/// Request to navigate to the Documents tab and show a specific document
/// </summary>
public record NavigateToDocumentMessage(int DocId);

/// <summary>
/// Request to navigate to the More Like This tab with a specific document
/// </summary>
public record NavigateToMoreLikeThisMessage(int DocId);

/// <summary>
/// Request to navigate to the Search tab and execute a field:term search
/// </summary>
public record SearchByTermMessage(string FieldName, string Term);

/// <summary>
/// Notification that a document was deleted
/// </summary>
public record DocumentDeletedMessage(int DocId);

/// <summary>
/// Message bus for loosely-coupled communication between components
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to all subscribers
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to publish</param>
    void Publish<T>(T message);

    /// <summary>
    /// Subscribes to messages of a specific type
    /// </summary>
    /// <typeparam name="T">The message type to listen for</typeparam>
    /// <returns>An observable stream of messages</returns>
    IObservable<T> Listen<T>();
}

/// <summary>
/// Default implementation of IMessageBus using System.Reactive
/// </summary>
public class MessageBus : IMessageBus
{
    private readonly Subject<object> _messageStream = new();

    public void Publish<T>(T message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        _messageStream.OnNext(message);
    }

    public IObservable<T> Listen<T>()
    {
        return _messageStream
            .OfType<T>()
            .AsObservable();
    }

    public void Dispose()
    {
        _messageStream.Dispose();
    }
}
