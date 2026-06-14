namespace SharpMonads.Core.Tests;

public class EitherShould
{
    private static Either<string, int> Increment(int x) => Either<string, int>.FromRight(x + 1);
    private static Either<string, int> Double(int x) => Either<string, int>.FromRight(x * 2);

    [Test]
    [Arguments(0)]
    [Arguments(5)]
    [Arguments(-3)]
    public async Task SatisfyTheLeftIdentityLaw(int value)
    {
        await Assert.That(Either<string, int>.FromRight(value).Bind(Increment)).IsEqualTo(Increment(value));
    }

    [Test]
    public async Task SatisfyTheRightIdentityLawForRight()
    {
        var either = Either<string, int>.FromRight(42);
        await Assert.That(either.Bind(Either<string, int>.FromRight)).IsEqualTo(either);
    }

    [Test]
    public async Task SatisfyTheRightIdentityLawForLeft()
    {
        var either = Either<string, int>.FromLeft("boom");
        await Assert.That(either.Bind(Either<string, int>.FromRight)).IsEqualTo(either);
    }

    [Test]
    [Arguments(10)]
    [Arguments(-1)]
    public async Task SatisfyTheAssociativityLaw(int value)
    {
        var either = Either<string, int>.FromRight(value);
        var chained = either.Bind(Increment).Bind(Double);
        var nested = either.Bind(x => Increment(x).Bind(Double));
        await Assert.That(chained).IsEqualTo(nested);
    }

    [Test]
    public async Task MapConsistentlyWithBind()
    {
        var either = Either<string, int>.FromRight(7);
        await Assert.That(either.Map(x => x + 100)).IsEqualTo(either.Bind(x => Either<string, int>.FromRight(x + 100)));
    }
    
    [Test]
    public async Task MatchTheRightBranchWhenItIsRight()
    {
        var label = Either<string, int>.FromRight(5).Match(e => $"left {e}", x => $"right {x}");
        await Assert.That(label).IsEqualTo("right 5");
    }

    [Test]
    public async Task MatchTheLeftBranchWhenItIsLeft()
    {
        var label = Either<string, int>.FromLeft("boom").Match(e => $"left {e}", x => $"right {x}");
        await Assert.That(label).IsEqualTo("left boom");
    }

    [Test]
    public async Task NotInvokeTheBinderAndPreserveTheLeftWhenItIsLeft()
    {
        var invoked = false;
        var either = Either<string, int>.FromLeft("boom").Bind(x =>
        {
            invoked = true;
            return Either<string, int>.FromRight(x);
        });

        await Assert.That(invoked).IsFalse();
        await Assert.That(either).IsEqualTo(Either<string, int>.FromLeft("boom"));
    }

    [Test]
    public async Task NotInvokeTheMapperAndPreserveTheLeftWhenItIsLeft()
    {
        var invoked = false;
        var either = Either<string, int>.FromLeft("boom").Map(x =>
        {
            invoked = true;
            return x + 1;
        });

        await Assert.That(invoked).IsFalse();
        await Assert.That(either).IsEqualTo(Either<string, int>.FromLeft("boom"));
    }

    [Test]
    public async Task TransformTheLeftValueWithMapLeft()
    {
        var either = Either<string, int>.FromLeft("boom").MapLeft(e => e.Length);
        await Assert.That(either).IsEqualTo(Either<int, int>.FromLeft(4));
    }

    [Test]
    public async Task NotInvokeMapLeftAndPreserveTheRightWhenItIsRight()
    {
        var invoked = false;
        var either = Either<string, int>.FromRight(7).MapLeft(e =>
        {
            invoked = true;
            return e.Length;
        });

        await Assert.That(invoked).IsFalse();
        await Assert.That(either).IsEqualTo(Either<int, int>.FromRight(7));
    }

    [Test]
    public async Task ThrowWhenAccessingTheRightOfALeft()
    {
        var left = Either<string, int>.FromLeft("boom");
        await Assert.That(() => left.Right).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowWhenAccessingTheLeftOfARight()
    {
        var right = Either<string, int>.FromRight(1);
        await Assert.That(() => right.Left).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ProduceTheSameResultWithLinqQuerySyntax()
    {
        var either = Either<string, int>.FromRight(3);

        var query =
            from x in either
            from y in Increment(x)
            select x + y;

        var chained = either.Bind(x => Increment(x).Map(y => x + y));

        await Assert.That(query).IsEqualTo(chained);
        await Assert.That(query).IsEqualTo(Either<string, int>.FromRight(7));
    }

    [Test]
    public async Task ShortCircuitTheLinqQueryOnLeft()
    {
        var query =
            from x in Either<string, int>.FromLeft("boom")
            from y in Increment(x)
            select x + y;

        await Assert.That(query).IsEqualTo(Either<string, int>.FromLeft("boom"));
    }

    [Test]
    public async Task ReturnTheFallbackValueWhenItIsLeft()
    {
        await Assert.That(Either<string, int>.FromLeft("boom").RightOr(99)).IsEqualTo(99);
        await Assert.That(Either<string, int>.FromRight(1).RightOr(99)).IsEqualTo(1);
    }

    [Test]
    public async Task FlipTheBranchesWhenSwapping()
    {
        await Assert.That(Either<string, int>.FromRight(7).Swap()).IsEqualTo(Either<int, string>.FromLeft(7));
        await Assert.That(Either<string, int>.FromLeft("boom").Swap()).IsEqualTo(Either<int, string>.FromRight("boom"));
    }

    [Test]
    public async Task ChainAsynchronouslyWithBindAsyncOnRight()
    {
        var either = await Either<string, int>.FromRight(3)
            .BindAsync(x => Task.FromResult(Either<string, int>.FromRight(x + 1)));

        await Assert.That(either).IsEqualTo(Either<string, int>.FromRight(4));
    }

    [Test]
    public async Task ShortCircuitBindAsyncOnLeft()
    {
        var invoked = false;
        var either = await Either<string, int>.FromLeft("boom")
            .BindAsync(x =>
            {
                invoked = true;
                return Task.FromResult(Either<string, int>.FromRight(x));
            });

        await Assert.That(invoked).IsFalse();
        await Assert.That(either).IsEqualTo(Either<string, int>.FromLeft("boom"));
    }

    [Test]
    public async Task MapAsynchronouslyOnRight()
    {
        var either = await Either<string, int>.FromRight(3)
            .MapAsync(x => Task.FromResult(x * 2));

        await Assert.That(either).IsEqualTo(Either<string, int>.FromRight(6));
    }

    [Test]
    public async Task ShortCircuitMapAsyncOnLeft()
    {
        var invoked = false;
        var either = await Either<string, int>.FromLeft("boom")
            .MapAsync(x =>
            {
                invoked = true;
                return Task.FromResult(x * 2);
            });

        await Assert.That(invoked).IsFalse();
        await Assert.That(either).IsEqualTo(Either<string, int>.FromLeft("boom"));
    }
}
