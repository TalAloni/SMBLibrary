---
trigger: model_decision
---

# Unit Testing Best Practices — Python / pytest — Not Applicable

This repository does not contain Python code. Ignore Python unit testing rules; if a Python tooling
subproject is added later, you can reintroduce pytest-focused guidance there.

---

## 1) Goals & Philosophy

- Behavior over implementation. Tests state what the system should do, not how it is built.
- Small, fast, isolated. Avoid real I/O and network calls in unit tests.
- Executable documentation. A test name should express scenario plus expected outcome.
- One reason to fail. Keep each test focused on a single behavior.

---

## 2) Naming & Discovery

- Place tests under a top-level tests/ directory.
- Files named test\__.py or_\_test.py are auto-discovered.
- Use plain test functions or classes prefixed with Test (no **init**).

Examples

````text
src/myapp/auth.py
tests/test_auth.py
tests/payment/test_billing.py
```text
Function names

- Good: test_login_with_valid_credentials_returns_true()
- Avoid: test_login() (too vague)

Prefer domain language in names and variables.

---

## 3) Pytest Configuration (optional but recommended)

Use pyproject.toml to centralize pytest options:

```toml
[tool.pytest.ini_options]
testpaths = ["tests"]
addopts = "-q -ra -vv --maxfail=1"
filterwarnings = [
  "error::DeprecationWarning",
]
```text
Common add-ons:

- pytest-cov for coverage
- pytest-xdist for parallel runs (-n auto)
- pytest-rerunfailures for transient flakes (short-term only)
- pytest-mock for a friendly mocker fixture over unittest.mock

---

## 4) Writing Tests: AAA & Given-When-Then

Prefer Arrange-Act-Assert with clear separation:

```python
def test_multiply_two_positive_numbers_returns_product():
    # Arrange
    calc = Calculator()

    # Act
    result = calc.multiply(2, 3)

    # Assert
    assert result == 6
```text
For complex scenarios, a Given-When-Then naming or comment style improves readability.

---

## 5) Fixtures

Use @pytest.fixture to share setup cleanly. Put common fixtures in tests/conftest.py so they are available everywhere.

```python
# tests/conftest.py
import pytest
from myapp.users import UserService, EmailSender

@pytest.fixture
def user_service(mocker):
    email = mocker.Mock(spec=EmailSender)
    return UserService(email), email
```text
Fixture scopes: function (default), module, session. Prefer function scope unless setup is expensive and immutable. For teardown, yield from fixtures:

```python
@pytest.fixture
def temp_dir(tmp_path):
    d = tmp_path / "work"
    d.mkdir()
    yield d
    # teardown happens after test
```text
---

## 6) Parameterized Tests

Use @pytest.mark.parametrize to cover input matrices without loops/ifs in test bodies:

```python
import pytest

@pytest.mark.parametrize(
    "text, expected",
    [
        ("", 0),
        ("5", 5),
        ("2,3", 5),
    ],
    ids=["empty", "single", "two_numbers"]
)
def test_add_returns_sum(text, expected):
    assert add(text) == expected
```text
---

## 7) Test Doubles (mocks/stubs/fakes)

Prefer simple fakes/stubs for data-returning collaborators. Use unittest.mock (via the mocker fixture from pytest-mock) when you need to verify interactions.

```python
def test_create_user_sends_welcome_email(mocker):
    send_email = mocker.patch("myapp.mail.send_email")
    create_user("alice@example.com")
    send_email.assert_called_once_with("alice@example.com")
```text
Guidelines

- Patch at the usage site (module.function) rather than where it is defined elsewhere.
- Avoid deep stubbing and complex expectations; keep mocks focused on observable behavior.
- Prefer dependency injection and small pure functions to reduce mocking needs.

---

## 8) Assertions & Exceptions

- Use plain assert; pytest shows rich diffs (lists, dicts, strings).
- For floats, use pytest.approx:

  ```python
  assert area == pytest.approx(3.14159, rel=1e-6)
````

- Exceptions:

  ```python
  import pytest
  with pytest.raises(ValueError, match="insufficient"):
      withdraw(balance=10, amount=100)
  ```

Create helper functions for repeated, domain-specific checks (e.g., assert_valid_user(user)).

---

## 9) Determinism & Isolation

- Time: Freeze time with freezegun.freeze_time or inject a clock dependency.
- Randomness: Seed RNGs (random.seed(0)) or inject deterministic generators.
- Environment: Use tmp_path for file system isolation, monkeypatch for env vars and globals.
- Order independence: Tests should pass regardless of execution order; avoid shared mutable state.

Parallelize with pytest-xdist if tests are order-independent and free of global state.

---

## 10) Working with Legacy or Side-Effect-Heavy Code

- Start with characterization tests to capture current behavior.
- Introduce seams: pass collaborators as parameters; wrap global access (time, env, I/O) behind small functions that can be patched.
- Refactor in small steps once behavior is under test.

---

## 11) Flakiness & Performance

- Avoid sleeps; poll only when necessary (integration level). Prefer deterministic checks.
- Control nondeterminism: fixed seeds, frozen time, isolated temp dirs.
- Use -x/--maxfail=1 to stop on first failure locally; run verbose -vv when diagnosing.
- For intermittent failures during transition, pytest-rerunfailures can reduce noise (but fix the root cause).

---

## 12) Tooling & CI

- Run locally with: pytest -q -ra -vv
- Coverage:

  ```bash
  pytest --cov=src --cov-report=term-missing
  ```

- JUnit XML for CI systems that parse test results:

  ```bash
  pytest --junitxml=reports/junit.xml
  ```

---

## 13) Quick Checklist

- [ ] Names read as behavior (scenario + expected outcome).
- [ ] One Act per test; AAA separated.
- [ ] No loops/ifs in tests; use parametrization.
- [ ] Doubles are minimal; mocks only for interaction verification.
- [ ] Deterministic time/random/env; isolated file system.
- [ ] Order-independent; safe to parallelize.
- [ ] Failures are self-explanatory.

---

## References

- pytest: <https://docs.pytest.org/>
- pytest-mock: <https://github.com/pytest-dev/pytest-mock>
- pytest-xdist: <https://github.com/pytest-dev/pytest-xdist>
- freezegun: <https://github.com/spulec/freezegun>
- pytest-cov / coverage.py: <https://github.com/pytest-dev/pytest-cov>
