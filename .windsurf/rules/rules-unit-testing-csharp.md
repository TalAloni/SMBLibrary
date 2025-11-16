---
trigger: model_decision
---

# Unit Testing Best Practices — C# / MSTest

This guide provides conventions and patterns for writing clear, fast, and maintainable unit tests in C# using MSTest (used by SMBLibrary). It emphasizes behavior‑focused tests and a TDD‑friendly workflow while staying pragmatic for solo development.

---

## 1) Goals & Philosophy

- Behavior over implementation. Tests describe what the system should do; avoid coupling to private details.
- Small, fast, isolated. No real I/O or network in unit tests. Prefer pure logic and in-memory fakes.
- Executable documentation. A failing test name should tell you exactly what broke.
- One reason to fail. Each test checks one behavior. Split or parametrize rather than adding branching logic in a test.
- TDD rhythm (optional but encouraged). Red -> Green -> Refactor in small steps.

---

## 2) Naming Conventions

### Projects & files

- Test project per runtime project (e.g., MyApp.Core -> MyApp.Core.Tests).
- File name mirrors the class/feature under test: StringCalculatorTests.cs.

### Classes

- One test class per unit or behavior cluster, named `UnitName`Tests (e.g., InvoiceServiceTests).

### Methods

Use MethodUnderTest_Scenario_ExpectedResult or behavior statements that read like facts.

- Good

  ```csharp
  [Fact]
  public void Add_SingleNumber_ReturnsSameNumber() { ... }
  ```

- Also fine (BDD-flavored)

  ```csharp
  [Fact]
  public void Given_PastDeliveryDate_When_Validating_Then_ResultIsInvalid() { ... }
  ```

- Avoid

  ```csharp
  [Fact] public void Test1() { ... }   // Vague
  [Fact] public void Calculates() { ... }  // No scenario or outcome
  ```

Keep names concise but specific. Prefer domain vocabulary to technical jargon.

---

## 3) Layout & Organization

- Separate projects for unit, integration, and end-to-end tests.
- Mirror source structure: if production code has Services/, Domain/, etc., reflect that in Tests/ namespaces.
- Do not share state across tests. Tests must be order-independent.

---

## 4) Writing Tests: AAA & Given-When-Then

Prefer clear Arrange-Act-Assert (AAA) with blank lines or comments to delineate sections.

```csharp
[TestClass]
public class CalculatorTests
{
    [TestMethod]
    public void Multiply_TwoPositiveNumbers_ReturnsProduct()
    {
        // Arrange
        var calc = new Calculator();

        // Act
        var result = calc.Multiply(2, 3);

        // Assert
        Assert.AreEqual(6, result);
    }
}
```

For more complex domain scenarios, a Given-When-Then flavor can improve readability, especially if "Given" is shared via a fixture.

---

## 5) Parameterized Tests

Use `[DataTestMethod]` and `[DataRow]` to cover input matrices without loops/branching in test bodies.

```csharp
[TestClass]
public class StringCalculatorTests
{
    [DataTestMethod]
    [DataRow("", 0)]
    [DataRow("5", 5)]
    [DataRow("2,3", 5)]
    public void Add_ValidInput_ReturnsSum(string input, int expected)
    {
        var calc = new StringCalculator();
        var result = calc.Add(input);
        Assert.AreEqual(expected, result);
    }
}
```

---

## 6) Fixtures & Shared Context

Use MSTest lifecycle hooks:

- `[TestInitialize]` / `[TestCleanup]` for per‑test setup/teardown.
- `[ClassInitialize]` / `[ClassCleanup]` (static) for expensive, read‑only context shared by all tests in a class.
- `TestContext` for per‑test context if needed.

Dispose shared resources in `Cleanup`/`ClassCleanup`. Keep fixtures lean; shared mutable state is a flakiness trap.

---

## 7) Test Doubles (Fakes, Stubs, Mocks)

- Default to fakes/stubs for dependencies that return data (e.g., in-memory repo).
- Use mocks only when you need to assert interactions (e.g., "email sender called exactly once").

Popular libraries: Moq, NSubstitute, FakeItEasy.

```csharp
[Fact]
public void CreateUser_SendsWelcomeEmail()
{
    var email = new Mock<IEmailSender>();
    var svc = new UserService(email.Object);

    svc.Create("alice@example.com");

    email.Verify(x => x.SendWelcome("alice@example.com"), Times.Once);
}
```

Guidelines

- Mock what you own (your interfaces), not third-party internals.
- Avoid deep stubbing / long expectation lists (brittle).
- Prefer dependency injection; avoid static singletons in production code.

---

## 8) Assertions & Exceptions

- Use Assert.Equal/NotEqual/True/False/Null/`Throws<T>()` as needed.
- For richer messages, consider FluentAssertions:

  ```csharp
  result.Should().Be(6);
  await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*insufficient funds*");
  ```

- For floating-point, compare with tolerance:

  ```csharp
  Assert.Equal(expected, actual, precision: 3);
  ```

---

## 9) Determinism & Isolation

- Time: Abstract clock access (e.g., IClock.UtcNow) to inject fixed times in tests.
- Randomness: Inject RNG; seed it in tests for repeatability.
- Environment: Isolate file system with temp paths; prefer in-memory substitutes (e.g., EF Core InMemory provider) for unit tests. Avoid hitting real DBs.
- Parallelism: Do not rely on test ordering. If parallel execution causes issues (shared resources), isolate via separate classes/projects or configure runsettings as needed.

---

## 10) Working with Legacy or Side-Effect-Heavy Code

- Characterization tests first: lock current behavior as a safety net.
- Introduce seams: wrap static calls (time, env, I/O) behind interfaces; pass dependencies via constructors.
- Refactor in slices: once behavior is captured, move logic into testable units.

---

## 11) Flakiness & Performance

- Eliminate sleeps; assert via polling helpers with timeouts only in integration tests.
- Reset static/global state between tests; avoid mutable singletons.
- Control nondeterminism (clock, RNG, concurrency).

Floating point & async tips

- Use tolerances (precision) for floats.
- For async, await everything; do not block (.Result / .Wait()).

---

## 12) Tooling & CI

- Run locally with: `dotnet test -v minimal`
- Coverage with coverlet:

  ```bash
  dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
  ```

- Fail the build on test failures; publish test results/coverage in CI.

---

## 13) Quick Checklist

- [ ] Names read as behavior (unit + scenario + expected).
- [ ] One Act per test; AAA separated.
- [ ] No loops/ifs in tests; use [Theory] instead.
- [ ] Doubles are minimal; mocks only when verifying interactions.
- [ ] Deterministic: time, RNG, env isolated.
- [ ] Tests run fast and in any order.
- [ ] Failures are informative without opening the test body.

---

## References

- MSTest docs: <https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-mstest>
- NSubstitute: <https://nsubstitute.github.io/>
- Moq: <https://github.com/moq/moq4>
- FakeItEasy: <https://fakeiteasy.github.io/>
- Coverlet: <https://github.com/coverlet-coverage/coverlet>
