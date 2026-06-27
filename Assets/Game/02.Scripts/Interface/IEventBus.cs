namespace JumJump.Interface
{
    public interface IEventBus
    {
        void Subscribe<T>(EventHandler<T> handler) where T : struct;
        void Unsubscribe<T>(EventHandler<T> handler) where T : struct;
        void Publish<T>(in T ev) where T : struct;
    }

    public delegate void EventHandler<T>(in T ev) where T : struct;
}
