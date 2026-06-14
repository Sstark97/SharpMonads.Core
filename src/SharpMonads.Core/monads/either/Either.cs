namespace SharpMonads.Core;

public readonly record struct Either<TLeft, TRight>
{
    private readonly TLeft? left;
    private readonly TRight? right;

    private Either(TLeft left)
    {
        this.left = left;
        right = default;
        IsRight = false;
    }

    private Either(TRight right)
    {
        left = default;
        this.right = right;
        IsRight = true;
    }

    public bool IsRight { get; }

    public bool IsLeft => !IsRight;

    public TRight Right => IsRight
        ? right!
        : throw new InvalidOperationException("Cannot access Right of a Left value");

    public TLeft Left => !IsRight
        ? left!
        : throw new InvalidOperationException("Cannot access Left of a Right value");

    public static Either<TLeft, TRight> FromLeft(TLeft left) => new(left);
    public static Either<TLeft, TRight> FromRight(TRight right) => new(right);

    public Either<TLeft, TOut> Map<TOut>(Func<TRight, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsRight
            ? Either<TLeft, TOut>.FromRight(mapper(right!))
            : Either<TLeft, TOut>.FromLeft(left!);
    }

    public Either<TOut, TRight> MapLeft<TOut>(Func<TLeft, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsLeft
            ? Either<TOut, TRight>.FromLeft(mapper(left!))
            : Either<TOut, TRight>.FromRight(right!);
    }

    public Either<TLeft, TOut> Bind<TOut>(Func<TRight, Either<TLeft, TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsRight
            ? binder(right!)
            : Either<TLeft, TOut>.FromLeft(left!);
    }

    public TResult Match<TResult>(
        Func<TLeft, TResult> onLeft,
        Func<TRight, TResult> onRight)
    {
        ArgumentNullException.ThrowIfNull(onLeft);
        ArgumentNullException.ThrowIfNull(onRight);
        return IsRight ? onRight(right!) : onLeft(left!);
    }
}
