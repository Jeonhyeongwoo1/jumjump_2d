using JumJump.Event;
using NUnit.Framework;

namespace JumJump.Tests
{
    public sealed class EventBusTests
    {
        [Test]
        public void Publish_InvokesSubscribedStructHandler()
        {
            var eventBus = new EventBus();
            var receivedScore = -1;

            eventBus.Subscribe<ScoreChangedEvent>((in ScoreChangedEvent ev) =>
            {
                receivedScore = ev.Score;
            });

            eventBus.Publish(new ScoreChangedEvent(7, 10));

            Assert.AreEqual(7, receivedScore);
        }

        [Test]
        public void Unsubscribe_RemovesHandler()
        {
            var eventBus = new EventBus();
            var callCount = 0;
            JumJump.Interface.EventHandler<TapRequestedEvent> handler = (in TapRequestedEvent ev) =>
            {
                callCount++;
            };

            eventBus.Subscribe(handler);
            eventBus.Unsubscribe(handler);
            eventBus.Publish(new TapRequestedEvent());

            Assert.AreEqual(0, callCount);
        }
    }
}
