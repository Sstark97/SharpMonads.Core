using SharpMonads.Core.monads.result;
using SharpMonads.Core.monads.unit;

namespace SharpMonads.Core.Tests.monads.result;

public class ResultShould
{
    private static Result<int, string> Increment(int x) => Result<int, string>.Success(x + 1);
    private static Result<int, string> Double(int x) => Result<int, string>.Success(x * 2);

    [Test]
    [Arguments(0)]
    [Arguments(5)]
    [Arguments(-3)]
    public async Task SatisfyTheLeftIdentityLaw(int value)
    {
        await Assert.That(Result<int, string>.Success(value).Bind(Increment)).IsEqualTo(Increment(value));
    }

    [Test]
    public async Task SatisfyTheRightIdentityLawForSuccess()
    {
        var result = Result<int, string>.Success(42);
        await Assert.That(result.Bind(Result<int, string>.Success)).IsEqualTo(result);
    }

    [Test]
    public async Task SatisfyTheRightIdentityLawForFailure()
    {
        var result = Result<int, string>.Failure("boom");
        await Assert.That(result.Bind(Result<int, string>.Success)).IsEqualTo(result);
    }

    [Test]
    [Arguments(10)]
    [Arguments(-1)]
    public async Task SatisfyTheAssociativityLaw(int value)
    {
        var result = Result<int, string>.Success(value);
        var chained = result.Bind(Increment).Bind(Double);
        var nested = result.Bind(x => Increment(x).Bind(Double));
        await Assert.That(chained).IsEqualTo(nested);
    }

    [Test]
    public async Task MapConsistentlyWithBind()
    {
        Func<int, int> add = x => x + 100;
        var result = Result<int, string>.Success(7);
        await Assert.That(result.Map(add)).IsEqualTo(result.Bind(x => Result<int, string>.Success(add(x))));
    }

    [Test]
    public async Task MatchTheSuccessBranchWhenItSucceeds()
    {
        var label = Result<int, string>.Success(5).Match(x => $"ok {x}", e => $"err {e}");
        await Assert.That(label).IsEqualTo("ok 5");
    }

    [Test]
    public async Task MatchTheFailureBranchWhenItFails()
    {
        var label = Result<int, string>.Failure("boom").Match(x => $"ok {x}", e => $"err {e}");
        await Assert.That(label).IsEqualTo("err boom");
    }

    [Test]
    public async Task NotInvokeTheBinderAndPreserveTheErrorWhenItFails()
    {
        var invoked = false;
        var result = Result<int, string>.Failure("boom").Bind(x =>
        {
            invoked = true;
            return Result<int, string>.Success(x);
        });

        await Assert.That(invoked).IsFalse();
        await Assert.That(result).IsEqualTo(Result<int, string>.Failure("boom"));
    }

    [Test]
    public async Task NotInvokeTheMapperAndPreserveTheErrorWhenItFails()
    {
        var invoked = false;
        var result = Result<int, string>.Failure("boom").Map(x =>
        {
            invoked = true;
            return x + 1;
        });

        await Assert.That(invoked).IsFalse();
        await Assert.That(result).IsEqualTo(Result<int, string>.Failure("boom"));
    }

    [Test]
    public async Task ThrowWhenAccessingTheValueOfAFailure()
    {
        var failure = Result<int, string>.Failure("boom");
        await Assert.That(() => failure.Value).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowWhenAccessingTheErrorOfASuccess()
    {
        var success = Result<int, string>.Success(1);
        await Assert.That(() => success.Error).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ProduceTheSameResultWithLinqQuerySyntax()
    {
        var result = Result<int, string>.Success(3);

        var query =
            from x in result
            from y in Increment(x)
            select x + y;

        var chained = result.Bind(x => Increment(x).Map(y => x + y));

        await Assert.That(query).IsEqualTo(chained);
        await Assert.That(query).IsEqualTo(Result<int, string>.Success(7));
    }

    [Test]
    public async Task ShortCircuitTheLinqQueryOnFailure()
    {
        var query =
            from x in Result<int, string>.Failure("boom")
            from y in Increment(x)
            select x + y;

        await Assert.That(query).IsEqualTo(Result<int, string>.Failure("boom"));
    }

    [Test]
    public async Task AggregateAllSuccessesWhenCollecting()
    {
        var result = new[] { 1, 2, 3 }.Collect(Increment);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEquivalentTo(new[] { 2, 3, 4 });
    }

    [Test]
    public async Task ShortCircuitCollectOnTheFirstFailure()
    {
        var result = new[] { 1, 2, 3 }.Collect(x =>
            x == 2 ? Result<int, string>.Failure("bad 2") : Increment(x));

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("bad 2");
    }

    [Test]
    public async Task FlattenSuccessfulListsWhenCollectingMany()
    {
        var result = new[] { 1, 2 }.CollectMany(x =>
            Result<IReadOnlyList<int>, string>.Success(new[] { x, x * 10 }));

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEquivalentTo(new[] { 1, 10, 2, 20 });
    }

    [Test]
    public async Task ShortCircuitCollectManyOnTheFirstFailure()
    {
        var result = new[] { 1, 2 }.CollectMany(x =>
            x == 2
                ? Result<IReadOnlyList<int>, string>.Failure("bad 2")
                : Result<IReadOnlyList<int>, string>.Success(new[] { x }));

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("bad 2");
    }

    [Test]
    public async Task ReplaceAFailureWithTheFallbackValueWhenRecovering()
    {
        var recovered = Result<int, string>.Failure("boom").Recover(0);
        await Assert.That(recovered).IsEqualTo(Result<int, string>.Success(0));
    }

    [Test]
    public async Task LeaveASuccessUntouchedWhenRecovering()
    {
        var result = Result<int, string>.Success(1);
        await Assert.That(result.Recover(0)).IsEqualTo(result);
    }

    [Test]
    public async Task UseTheErrorWhenRecoveringWithAFactory()
    {
        var recovered = Result<int, string>.Failure("boom").Recover(error => error.Length);
        await Assert.That(recovered).IsEqualTo(Result<int, string>.Success(4));
    }

    [Test]
    public async Task ChainAsynchronouslyWithBindAsyncOnSuccess()
    {
        var result = await Result<int, string>.Success(3)
            .BindAsync(x => Task.FromResult(Result<int, string>.Success(x + 1)));

        await Assert.That(result).IsEqualTo(Result<int, string>.Success(4));
    }

    [Test]
    public async Task ShortCircuitBindAsyncOnFailure()
    {
        var invoked = false;
        var result = await Result<int, string>.Failure("boom")
            .BindAsync(x =>
            {
                invoked = true;
                return Task.FromResult(Result<int, string>.Success(x));
            });

        await Assert.That(invoked).IsFalse();
        await Assert.That(result).IsEqualTo(Result<int, string>.Failure("boom"));
    }

    [Test]
    public async Task MapAsynchronouslyOnSuccess()
    {
        var result = await Result<int, string>.Success(3)
            .MapAsync(x => Task.FromResult(x * 2));

        await Assert.That(result).IsEqualTo(Result<int, string>.Success(6));
    }

    [Test]
    public async Task PropagateTheFailureFromTheSideEffectWithTapAsync()
    {
        var result = await Result<int, string>.Success(3)
            .TapAsync(_ => Task.FromResult(Result<Unit, string>.Failure("side")));

        await Assert.That(result).IsEqualTo(Result<int, string>.Failure("side"));
    }

    [Test]
    public async Task KeepTheValueWhenTheSideEffectSucceedsWithTapAsync()
    {
        var result = await Result<int, string>.Success(3)
            .TapAsync(_ => Task.FromResult(Result<Unit, string>.Success(Unit.Value)));

        await Assert.That(result).IsEqualTo(Result<int, string>.Success(3));
    }
}
