using SharpMonads.Core.monads.option;
using SharpMonads.Core.monads.result;

namespace SharpMonads.Core.Tests.monads.option;

public class OptionShould
{
    private static Option<int> Increment(int x) => Option<int>.Some(x + 1);
    private static Option<int> Double(int x) => Option<int>.Some(x * 2);

    [Test]
    [Arguments(0)]
    [Arguments(5)]
    [Arguments(-3)]
    public async Task SatisfyTheLeftIdentityLaw(int value)
    {
        await Assert.That(Option<int>.Some(value).Bind(Increment)).IsEqualTo(Increment(value));
    }

    [Test]
    public async Task SatisfyTheRightIdentityLawForSome()
    {
        var option = Option<int>.Some(42);
        await Assert.That(option.Bind(Option<int>.Some)).IsEqualTo(option);
    }

    [Test]
    public async Task SatisfyTheRightIdentityLawForNone()
    {
        var option = Option<int>.None;
        await Assert.That(option.Bind(Option<int>.Some)).IsEqualTo(option);
    }

    [Test]
    [Arguments(10)]
    [Arguments(-1)]
    public async Task SatisfyTheAssociativityLaw(int value)
    {
        var option = Option<int>.Some(value);
        var chained = option.Bind(Increment).Bind(Double);
        var nested = option.Bind(x => Increment(x).Bind(Double));
        await Assert.That(chained).IsEqualTo(nested);
    }

    [Test]
    public async Task MapConsistentlyWithBind()
    {
        Func<int, int> add = x => x + 100;
        var option = Option<int>.Some(7);
        await Assert.That(option.Map(add)).IsEqualTo(option.Bind(x => Option<int>.Some(add(x))));
    }

    [Test]
    public async Task MatchTheSomeBranchWhenItHasAValue()
    {
        var label = Option<int>.Some(5).Match(x => $"some {x}", () => "none");
        await Assert.That(label).IsEqualTo("some 5");
    }

    [Test]
    public async Task MatchTheNoneBranchWhenItIsEmpty()
    {
        var label = Option<int>.None.Match(x => $"some {x}", () => "none");
        await Assert.That(label).IsEqualTo("none");
    }

    [Test]
    public async Task NotInvokeTheBinderWhenItIsNone()
    {
        var invoked = false;
        var result = Option<int>.None.Bind(x =>
        {
            invoked = true;
            return Option<int>.Some(x);
        });

        await Assert.That(invoked).IsFalse();
        await Assert.That(result).IsEqualTo(Option<int>.None);
    }

    [Test]
    public async Task NotInvokeTheMapperWhenItIsNone()
    {
        var invoked = false;
        var result = Option<int>.None.Map(x =>
        {
            invoked = true;
            return x + 1;
        });

        await Assert.That(invoked).IsFalse();
        await Assert.That(result).IsEqualTo(Option<int>.None);
    }

    [Test]
    public async Task KeepTheValueWhenThePredicateHolds()
    {
        var option = Option<int>.Some(4);
        await Assert.That(option.Filter(x => x % 2 == 0)).IsEqualTo(option);
    }

    [Test]
    public async Task ReturnNoneWhenThePredicateFails()
    {
        var option = Option<int>.Some(3);
        await Assert.That(option.Filter(x => x % 2 == 0)).IsEqualTo(Option<int>.None);
    }

    [Test]
    public async Task ReturnTheFallbackValueWhenItIsNone()
    {
        await Assert.That(Option<int>.None.ValueOr(99)).IsEqualTo(99);
        await Assert.That(Option<int>.Some(1).ValueOr(99)).IsEqualTo(1);
    }

    [Test]
    public async Task ProduceTheSameResultWithLinqQuerySyntax()
    {
        var option = Option<int>.Some(3);

        var query =
            from x in option
            from y in Increment(x)
            select x + y;

        var chained = option.Bind(x => Increment(x).Map(y => x + y));

        await Assert.That(query).IsEqualTo(chained);
        await Assert.That(query).IsEqualTo(Option<int>.Some(7));
    }

    [Test]
    public async Task FilterWithTheLinqWhereClause()
    {
        var query =
            from x in Option<int>.Some(5)
            where x > 10
            select x;

        await Assert.That(query).IsEqualTo(Option<int>.None);
    }

    [Test]
    public async Task ConvertASuccessfulResultIntoSome()
    {
        var option = Result<int, string>.Success(7).ToOption();
        await Assert.That(option).IsEqualTo(Option<int>.Some(7));
    }

    [Test]
    public async Task ConvertAFailedResultIntoNone()
    {
        var option = Result<int, string>.Failure("boom").ToOption();
        await Assert.That(option).IsEqualTo(Option<int>.None);
    }

    [Test]
    public async Task EnumerateItsValueOrAnEmptySequence()
    {
        await Assert.That(Option<int>.Some(1).ToEnumerable()).IsEquivalentTo(new[] { 1 });
        await Assert.That(Option<int>.None.ToEnumerable()).IsEmpty();
    }
}
