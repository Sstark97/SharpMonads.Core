# SharpMonads.Core

A functional programming library providing solid, efficient implementations of essential monads for C#.

`SharpMonads.Core` ships zero-allocation value types (`readonly record struct`) for the most common functional building blocks: `Option<T>`, `Result<TValue, TError>`, `Either<TLeft, TRight>`, and `Unit`. Each comes with `Map`/`Bind`/`Match` operators, LINQ query-syntax support, and async combinators.

- **Target framework:** `net10.0`
- **License:** MIT

## Installation

```bash
dotnet add package SharpMonads.Core
```

```csharp
using SharpMonads.Core;
```

## `Option<T>`

Represents a value that may or may not be present — a type-safe alternative to `null`.

```csharp
// Construction
Option<int> some = Option<int>.Some(42);
Option<int> none = Option<int>.None;

// Match: collapse to a single value
string label = some.Match(
    onSome: value => $"Got {value}",
    onNone: () => "Nothing here");

// Map: transform the inner value if present
Option<int> doubled = some.Map(x => x * 2);            // Some(84)

// Bind: chain operations that themselves return an Option
Option<int> Parse(string s) =>
    int.TryParse(s, out var n) ? Option<int>.Some(n) : Option<int>.None;

Option<int> parsed = Option<string>.Some("10").Bind(Parse); // Some(10)

// Filter: keep the value only if it satisfies a predicate
Option<int> evenOnly = some.Filter(x => x % 2 == 0);   // Some(42)

// ValueOr: provide a fallback
int safe = none.ValueOr(0);                            // 0
```

### LINQ query syntax

`Select`, `SelectMany`, and `Where` let you compose options with C# query expressions:

```csharp
Option<int> result =
    from a in Option<int>.Some(3)
    from b in Option<int>.Some(4)
    where a < b
    select a + b;                                      // Some(7)

// Convert to/from other shapes
IEnumerable<int> items = result.ToEnumerable();        // [7]
Option<int> fromResult = Result<int, string>.Success(1).ToOption(); // Some(1)
```

## `Result<TValue, TError>`

Represents either a success carrying a `TValue` or a failure carrying a `TError`. Use it to model recoverable errors without exceptions.

```csharp
// Construction
Result<int, string> ok = Result<int, string>.Success(42);
Result<int, string> err = Result<int, string>.Failure("boom");

// Inspect
bool succeeded = ok.IsSuccess;   // true
bool failed = err.IsFailure;     // true

// Match: handle both branches
string message = ok.Match(
    onSuccess: value => $"OK: {value}",
    onFailure: error => $"Error: {error}");

// Map: transform the success value
Result<string, string> mapped = ok.Map(x => x.ToString());

// Bind: chain operations that may fail
Result<int, string> Divide(int a, int b) =>
    b == 0
        ? Result<int, string>.Failure("divide by zero")
        : Result<int, string>.Success(a / b);

Result<int, string> chained = ok.Bind(x => Divide(x, 2)); // Success(21)
```

### LINQ query syntax

```csharp
Result<int, string> total =
    from a in Result<int, string>.Success(10)
    from b in Result<int, string>.Success(5)
    select a + b;                                      // Success(15)
```

### Collecting sequences

`Collect` and `CollectMany` turn a sequence of fallible operations into a single `Result`, short-circuiting on the first failure:

```csharp
Result<IReadOnlyList<int>, string> numbers =
    new[] { "1", "2", "3" }.Collect(Parse);            // Success([1, 2, 3])

Result<int, string> Parse(string s) =>
    int.TryParse(s, out var n)
        ? Result<int, string>.Success(n)
        : Result<int, string>.Failure($"invalid: {s}");
```

### Recovering from failure

```csharp
Result<int, string> recovered = err.Recover(0);                 // Success(0)
Result<int, string> recoveredFrom = err.Recover(e => e.Length); // Success(4)
```

### Async combinators

`BindAsync`, `MapAsync`, and `TapAsync` work on both `Result<...>` and `Task<Result<...>>`, so you can build fluent asynchronous pipelines:

```csharp
async Task<Result<User, string>> LoadUserAsync(int id) { /* ... */ }
async Task<Result<Unit, string>> AuditAsync(User user) { /* ... */ }

Result<string, string> name = await Result<int, string>.Success(1)
    .BindAsync(LoadUserAsync)      // chain an async fallible step
    .TapAsync(AuditAsync)          // run a side effect, keep the value
    .MapAsync(user => user.Name);  // transform the success value
```

## `Either<TLeft, TRight>`

Represents a value of one of two possible types. By convention it is *right-biased*: `Right` holds the "happy path" value and `Left` the alternative, so `Map`/`Bind` operate on the `Right` side and short-circuit on `Left`.

```csharp
// Construction
Either<string, int> right = Either<string, int>.FromRight(42);
Either<string, int> left = Either<string, int>.FromLeft("boom");

// Inspect
bool isRight = right.IsRight;   // true
bool isLeft = left.IsLeft;      // true

// Match: handle both branches
string message = right.Match(
    onLeft: error => $"Left: {error}",
    onRight: value => $"Right: {value}");

// Map: transform the Right value (Left passes through untouched)
Either<string, string> mapped = right.Map(x => x.ToString());

// MapLeft: transform the Left value (Right passes through untouched)
Either<int, int> mappedLeft = left.MapLeft(e => e.Length);

// Bind: chain operations that themselves return an Either
Either<string, int> Halve(int n) =>
    n % 2 == 0
        ? Either<string, int>.FromRight(n / 2)
        : Either<string, int>.FromLeft("odd number");

Either<string, int> chained = right.Bind(Halve); // FromRight(21)
```

### LINQ query syntax

`Select` and `SelectMany` compose the `Right` side with C# query expressions:

```csharp
Either<string, int> total =
    from a in Either<string, int>.FromRight(10)
    from b in Either<string, int>.FromRight(5)
    select a + b;                                      // FromRight(15)

// RightOr: provide a fallback when Left
int safe = left.RightOr(0);                           // 0

// Swap: flip Left and Right
Either<int, string> swapped = right.Swap();           // FromLeft(42)
```

### Async combinators

`BindAsync` and `MapAsync` work on both `Either<...>` and `Task<Either<...>>`, so you can build fluent asynchronous pipelines:

```csharp
async Task<Either<string, User>> LoadUserAsync(int id) { /* ... */ }

Either<string, string> name = await Either<string, int>.FromRight(1)
    .BindAsync(LoadUserAsync)      // chain an async step on the Right side
    .MapAsync(user => user.Name);  // transform the Right value
```

## `Unit`

Represents the absence of a meaningful value (the functional equivalent of `void`). Useful as a `TValue` for operations performed purely for their side effects.

```csharp
Result<Unit, string> SaveChanges()
{
    // ... perform work ...
    return Result<Unit, string>.Success(Unit.Value);
}
```

## License

Distributed under the [MIT](https://opensource.org/licenses/MIT) license.
