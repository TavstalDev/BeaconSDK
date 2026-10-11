# Contributing to BeaconSDK

Thank you for your interest in helping with this project. This guide explains how to contribute.

---

## How to contribute

1. **Fork** the repository on GitHub.
2. **Clone** your fork to your computer.
3. Create a **new branch** for your work:
   ```bash
   git checkout -b feature/my-new-feature
   ```
4. Make your changes.
5. **Test** your changes (see [Testing](#testing)).
6. **Commit** your changes with a clear message.
7. **Push** your branch and open a **pull request** (PR).

---

## Development setup

Requirements:

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- A code editor (Visual Studio, Rider, VS Code, etc.)

Build the solution:

```bash
dotnet build BeaconSDK.sln
```

Run the tests:

```bash
dotnet run --project ./BeaconSDK.Tests/BeaconSDK.Tests.csproj --configuration Debug
```

---

## Testing

The project has a test project: `BeaconSDK.Tests`. Please follow these rules:

- Add tests for every new feature or bug fix.
- Put tests in files that clearly describe what they test.
- Run all tests before you open a pull request.

---

## Code style

Please follow the existing style in the project:

- Use the same naming conventions as the rest of the code.
- Use file-scoped namespaces.
- Enable `nullable` and `implicit usings` (they are already on).
- Do **not** add comments unless they explain something non-obvious.
- Keep methods small and focused.

If you are unsure, look at similar files in the same folder for examples.

---

## Commit messages

Use short, clear commit messages. We follow the **Conventional Commits** style:

- `feat: add new endpoint for ...`
- `fix: correct the retry logic in ...`
- `docs: update the API reference ...`
- `refactor: clean up ...`
- `test: add tests for ...`

---

## Pull requests

Before you open a pull request, please check:

- [ ] The code builds without errors.
- [ ] All tests pass.
- [ ] No secrets or tokens are committed.
- [ ] The branch is up to date with `master`.

---

## AI usage

This policy applies to pull requests from external contributors.

- **AI-generated documentation** — accepted, as long as a human reviews it.
- **AI-generated code** — not accepted. This means code written in bulk by an AI, like a whole
  method or file pasted from a chatbot.
- **AI-suggested fixes** — these are in the gray zone. A suggestion is a small edit, like inline
  autocomplete or a short fix (up to roughly one method). A human must review it and test it before
  merging.
- **Gray-zone disclosure** — if your pull request contains gray-zone fixes, mention it in the PR
  description so reviewers know what to check closely.
- **Testing** — gray-zone fixes are only mergeable when the full test suite passes. Please add a
  new test when it makes sense.

---

## Questions?

If you need help, open an [issue](https://github.com/TavstalDev/BeaconSDK/issues) and ask.
