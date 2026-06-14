namespace SharpMonads.Core;

public static class EitherLinqExtensions
{
    public static Either<TLeft, TOut> Select<TLeft, TRight, TOut>(
        this Either<TLeft, TRight> either,
        Func<TRight, TOut> selector)
        => either.Map(selector);

    public static Either<TLeft, TOut> SelectMany<TLeft, TRight, TIntermediate, TOut>(
        this Either<TLeft, TRight> either,
        Func<TRight, Either<TLeft, TIntermediate>> binder,
        Func<TRight, TIntermediate, TOut> projector)
        => either.Bind(right =>
            binder(right).Map(intermediate =>
                projector(right, intermediate)));

    public static TRight RightOr<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        TRight fallback) =>
        either.IsRight ? either.Right : fallback;

    public static Either<TRight, TLeft> Swap<TLeft, TRight>(
        this Either<TLeft, TRight> either) =>
        either.Match(
            Either<TRight, TLeft>.FromRight,
            Either<TRight, TLeft>.FromLeft);
}
