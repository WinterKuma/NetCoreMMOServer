namespace NetCoreMMOServer.Utility
{
    public delegate ValueTask<bool> AsyncPredicate<in T>(T eventArgs);
    public delegate ValueTask AsyncAction<in T>(T eventArgs);
    public delegate ValueTask AsyncAction();

    public static class AsyncEventExtensions
    {
        public static ValueTask SafeInvokeAsync<T>(this AsyncAction<T>? handler, Func<T> argsFactory)
        {
            if (handler is null)
            {
                return ValueTask.CompletedTask;
            }

            return handler.Invoke(argsFactory());
        }

        public static ValueTask SafeInvokeAsync<T>(this AsyncAction<T>? handler, T args)
        {
            if (handler is null)
            {
                return ValueTask.CompletedTask;
            }

            return handler.Invoke(args);
        }

        public static ValueTask SafeInvokeAsync(this AsyncAction? handler)
        {
            if (handler is null)
            {
                return ValueTask.CompletedTask;
            }

            return handler.Invoke();
        }
    }
}