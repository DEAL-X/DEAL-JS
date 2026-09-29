namespace MiMFa.Engine.Walker
{
    public interface IWalker<T> where T : class
    {
        string Source { get; }
        int Position { get; }
        int Length { get; }
        bool IsRunning { get; }
        bool IsEnded { get; }
        T Current { get; }

        T Walk();
        T Peek(int offset);

        IWalker<T> Move(int count);
        IWalker<T> Reset(int position);

        IWalker<T> Remove(int start, int count);
        IWalker<T> Remove(T item);

        IWalker<T> Replace(int start, int count, params T[] replacement);
        IWalker<T> Replace(params T[] replacement);
    }
}
