namespace SharpMonads.Core.monads.either;

public static class EitherAsyncExtensions
{
    public static async Task<Either<TLeft, TOut>> BindAsync<TLeft, TRight, TOut>(
        this Either<TLeft, TRight> either,
        Func<TRight, Task<Either<TLeft, TOut>>> binder)
    {
        if (either.IsLeft)
            return Either<TLeft, TOut>.FromLeft(either.Left);

        return await binder(either.Right);
    }

    public static async Task<Either<TLeft, TOut>> BindAsync<TLeft, TRight, TOut>(
        this Task<Either<TLeft, TRight>> eitherTask,
        Func<TRight, Task<Either<TLeft, TOut>>> binder)
    {
        var either = await eitherTask;
        return await either.BindAsync(binder);
    }

    public static async Task<Either<TLeft, TOut>> MapAsync<TLeft, TRight, TOut>(
        this Either<TLeft, TRight> either,
        Func<TRight, Task<TOut>> mapper)
    {
        if (either.IsLeft)
            return Either<TLeft, TOut>.FromLeft(either.Left);

        var mapped = await mapper(either.Right);
        return Either<TLeft, TOut>.FromRight(mapped);
    }

    public static async Task<Either<TLeft, TOut>> MapAsync<TLeft, TRight, TOut>(
        this Task<Either<TLeft, TRight>> eitherTask,
        Func<TRight, TOut> mapper)
    {
        var either = await eitherTask;
        return either.Map(mapper);
    }
}
