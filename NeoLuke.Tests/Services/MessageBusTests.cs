using System.Reactive.Linq;
using NeoLuke.Services;

namespace NeoLuke.Tests.Services;

public class MessageBusTests : IDisposable
{
    private readonly MessageBus _messageBus = new();

    // Test messages
    private record TestMessage(string Content);
    private record AnotherTestMessage(int Value);

    [Fact]
    public void Publish_WithValidMessage_NotifiesSubscribers()
    {
        // Arrange
        TestMessage? receivedMessage = null;
        _messageBus.Listen<TestMessage>().Subscribe(msg => receivedMessage = msg);

        var message = new TestMessage("Hello");

        // Act
        _messageBus.Publish(message);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal("Hello", receivedMessage.Content);
    }

    [Fact]
    public void Publish_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _messageBus.Publish<TestMessage>(null!));
    }

    [Fact]
    public void Listen_MultipleSubscribers_AllReceiveMessage()
    {
        // Arrange
        var received1 = false;
        var received2 = false;
        var received3 = false;

        _messageBus.Listen<TestMessage>().Subscribe(_ => received1 = true);
        _messageBus.Listen<TestMessage>().Subscribe(_ => received2 = true);
        _messageBus.Listen<TestMessage>().Subscribe(_ => received3 = true);

        // Act
        _messageBus.Publish(new TestMessage("Broadcast"));

        // Assert
        Assert.True(received1);
        Assert.True(received2);
        Assert.True(received3);
    }

    [Fact]
    public void Listen_DifferentMessageTypes_OnlyReceivesMatchingType()
    {
        // Arrange
        TestMessage? testMessageReceived = null;
        AnotherTestMessage? anotherMessageReceived = null;

        _messageBus.Listen<TestMessage>().Subscribe(msg => testMessageReceived = msg);
        _messageBus.Listen<AnotherTestMessage>().Subscribe(msg => anotherMessageReceived = msg);

        // Act
        _messageBus.Publish(new TestMessage("Test"));
        _messageBus.Publish(new AnotherTestMessage(42));

        // Assert
        Assert.NotNull(testMessageReceived);
        Assert.Equal("Test", testMessageReceived.Content);

        Assert.NotNull(anotherMessageReceived);
        Assert.Equal(42, anotherMessageReceived.Value);
    }

    [Fact]
    public void Listen_BeforePublish_ReceivesSubsequentMessages()
    {
        // Arrange
        var messages = new List<TestMessage>();
        _messageBus.Listen<TestMessage>().Subscribe(messages.Add);

        // Act
        _messageBus.Publish(new TestMessage("First"));
        _messageBus.Publish(new TestMessage("Second"));
        _messageBus.Publish(new TestMessage("Third"));

        // Assert
        Assert.Equal(3, messages.Count);
        Assert.Equal("First", messages[0].Content);
        Assert.Equal("Second", messages[1].Content);
        Assert.Equal("Third", messages[2].Content);
    }

    [Fact]
    public void Listen_AfterPublish_DoesNotReceivePreviousMessages()
    {
        // Arrange
        _messageBus.Publish(new TestMessage("Before subscription"));

        var messages = new List<TestMessage>();
        _messageBus.Listen<TestMessage>().Subscribe(messages.Add);

        // Act
        _messageBus.Publish(new TestMessage("After subscription"));

        // Assert
        Assert.Single(messages);
        Assert.Equal("After subscription", messages[0].Content);
    }

    [Fact]
    public void Subscription_Disposed_StopsReceivingMessages()
    {
        // Arrange
        var messageCount = 0;
        var subscription = _messageBus.Listen<TestMessage>().Subscribe(_ => messageCount++);

        _messageBus.Publish(new TestMessage("First"));

        // Act
        subscription.Dispose();
        _messageBus.Publish(new TestMessage("Second"));

        // Assert
        Assert.Equal(1, messageCount);
    }

    [Fact]
    public void NavigateToDocumentMessage_CanBePublishedAndReceived()
    {
        // Arrange
        NavigateToDocumentMessage? received = null;
        _messageBus.Listen<NavigateToDocumentMessage>().Subscribe(msg => received = msg);

        // Act
        _messageBus.Publish(new NavigateToDocumentMessage(42));

        // Assert
        Assert.NotNull(received);
        Assert.Equal(42, received.DocId);
    }

    [Fact]
    public void NavigateToMoreLikeThisMessage_CanBePublishedAndReceived()
    {
        // Arrange
        NavigateToMoreLikeThisMessage? received = null;
        _messageBus.Listen<NavigateToMoreLikeThisMessage>().Subscribe(msg => received = msg);

        // Act
        _messageBus.Publish(new NavigateToMoreLikeThisMessage(123));

        // Assert
        Assert.NotNull(received);
        Assert.Equal(123, received.DocId);
    }

    [Fact]
    public void SearchByTermMessage_CanBePublishedAndReceived()
    {
        // Arrange
        SearchByTermMessage? received = null;
        _messageBus.Listen<SearchByTermMessage>().Subscribe(msg => received = msg);

        // Act
        _messageBus.Publish(new SearchByTermMessage("content", "test"));

        // Assert
        Assert.NotNull(received);
        Assert.Equal("content", received.FieldName);
        Assert.Equal("test", received.Term);
    }

    [Fact]
    public void DocumentDeletedMessage_CanBePublishedAndReceived()
    {
        // Arrange
        DocumentDeletedMessage? received = null;
        _messageBus.Listen<DocumentDeletedMessage>().Subscribe(msg => received = msg);

        // Act
        _messageBus.Publish(new DocumentDeletedMessage(99));

        // Assert
        Assert.NotNull(received);
        Assert.Equal(99, received.DocId);
    }

    [Fact]
    public void MultipleMessageTypes_PublishedConcurrently_AllDeliveredCorrectly()
    {
        // Arrange
        var navMessages = new List<NavigateToDocumentMessage>();
        var searchMessages = new List<SearchByTermMessage>();
        var deleteMessages = new List<DocumentDeletedMessage>();

        _messageBus.Listen<NavigateToDocumentMessage>().Subscribe(navMessages.Add);
        _messageBus.Listen<SearchByTermMessage>().Subscribe(searchMessages.Add);
        _messageBus.Listen<DocumentDeletedMessage>().Subscribe(deleteMessages.Add);

        // Act
        _messageBus.Publish(new NavigateToDocumentMessage(1));
        _messageBus.Publish(new SearchByTermMessage("field1", "term1"));
        _messageBus.Publish(new DocumentDeletedMessage(10));
        _messageBus.Publish(new NavigateToDocumentMessage(2));
        _messageBus.Publish(new SearchByTermMessage("field2", "term2"));

        // Assert
        Assert.Equal(2, navMessages.Count);
        Assert.Equal(2, searchMessages.Count);
        Assert.Single(deleteMessages);

        Assert.Equal(1, navMessages[0].DocId);
        Assert.Equal(2, navMessages[1].DocId);
        Assert.Equal("field1", searchMessages[0].FieldName);
        Assert.Equal("field2", searchMessages[1].FieldName);
        Assert.Equal(10, deleteMessages[0].DocId);
    }

    [Fact]
    public void Listen_WithObservableOperators_WorksCorrectly()
    {
        // Arrange
        var messages = new List<int>();
        _messageBus.Listen<NavigateToDocumentMessage>()
            .Select(msg => msg.DocId)
            .Where(docId => docId > 10)
            .Subscribe(messages.Add);

        // Act
        _messageBus.Publish(new NavigateToDocumentMessage(5));
        _messageBus.Publish(new NavigateToDocumentMessage(15));
        _messageBus.Publish(new NavigateToDocumentMessage(8));
        _messageBus.Publish(new NavigateToDocumentMessage(20));

        // Assert
        Assert.Equal(2, messages.Count);
        Assert.Equal(15, messages[0]);
        Assert.Equal(20, messages[1]);
    }

    public void Dispose()
    {
        _messageBus.Dispose();
    }
}
