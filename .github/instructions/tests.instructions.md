---
applyTo: "**/*.Test/**,**/*Test.cs,**/*Tests.cs"
---

# Test Conventions

## Framework

- MSTest (`[TestClass]`, `[TestMethod]`)
- Moq for mocking (inject via `Mock<IService>()`)

## Patterns

- Follow Arrange/Act/Assert pattern
- Use `[TestInitialize]` for per-test setup, `[TestCleanup]` for teardown
- Name test classes: `{ClassName}Test.cs`
- Name test methods descriptively: `Constructor_InitializesProperties()`, `Method_Condition_ExpectedBehavior()`
- Integration tests suffixed `IntegrationTest` — these touch file system or DI container
- WPF tests use `[WpfTestMethod]` instead of `[TestMethod]` (enforces STA thread)

## Mocking

- Mock all dependencies injected via constructor
- Use `mock.Setup()` for behavior, `mock.Verify()` for assertions
- Use `It.IsAny<T>()` for flexible argument matching

## Assertions

- Use `Assert.AreEqual()`, `Assert.IsNotNull()`, `Assert.ThrowsException<T>()`
- One logical assertion per test when possible
