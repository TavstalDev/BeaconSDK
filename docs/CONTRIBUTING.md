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
- [ ] AI usage is disclosed in the PR description (see [AI usage](#ai-usage)).

---

## AI usage

This policy applies to pull requests from external contributors.

AI-assisted contributions are welcome. AI is treated as a tool, like an IDE feature: you are
responsible for everything you submit, AI or not.

Rules:

- **Review** - you must read, understand, and be able to explain every line you submit. Adjust and
  improve AI output instead of pasting it as-is.
- **Test** - the full test suite must pass. Add new tests when it makes sense.
- **Disclose** - mention your AI usage in the PR description (see template below).

Accepted:

- Implementing a feature or fix with AI help, then reviewing, adjusting, and testing it yourself.
- Inline autocomplete and small AI suggestions (up to roughly one method).
- AI-generated documentation, comments, commit messages, and PR descriptions, after a human review.
- AI help with tests, as long as you run them and verify they actually fail without the fix.

Not accepted:

- Pasting whole files, classes, or features from a chatbot without reviewing them.
- Submitting code you cannot explain or debug.
- Opening PRs generated end-to-end by an AI agent with little or no human review.
- Committing AI-hallucinated content: fake APIs, invented dependencies, or placeholder secrets.

Disclosure template (add to your PR description):

```markdown
### AI usage
- Tool: e.g. GitHub Copilot, ChatGPT, Claude
- Used for: e.g. drafted the retry logic in `X`, autocompleted small edits
- Verification: reviewed manually, ran the full test suite locally
```

Reviewer guidance:

- Reviewers may ask you to explain any part of your PR. If you cannot, it will not be merged.
- PRs with unreviewed bulk AI code or missing disclosure may be closed.
- When in doubt, over-disclose. Disclosure never counts against you.

---

## Questions?

If you need help, open an [issue](https://github.com/TavstalDev/BeaconSDK/issues) and ask.
