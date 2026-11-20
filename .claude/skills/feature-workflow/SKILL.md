---
name: feature-workflow
description: End-to-end workflow for implementing a new feature in a software project with Git – syncing with origin/dev, creating a feature branch, getting the plan approved, implementing without committing, running the auto-detected test suite, updating docs, and finally proposing a logical commit order for the user, who commits and opens the pull request themselves. Language/stack-agnostic (e.g. C#/.NET, TypeScript/Node, Python, Java, Go, Rust) – the agent detects build and test tooling from the project files. Always use this skill when the user asks to implement a new feature, capability, or extension in one of their projects – even if they don't explicitly say "skill", "workflow", or "branch", e.g. just "build me feature X" or "implement XY". Does not apply to pure bugfixes without a new feature, pure refactors, or one-line changes, unless the user explicitly refers to this process.
---

# Feature Workflow

This skill captures the fixed process the user relies on for implementing new features in their projects, regardless of language or tech stack. The value of the process comes from every step staying traceable: a clean starting point, an approved plan, verified tests, up-to-date docs, and a clear proposal for how the user should split the changes into commits. Stick to the order — each step builds on the previous one, and skipping one makes the next unreliable (e.g. tests on unplanned code).

**Important: the agent never commits, never pushes and never opens a pull request in this workflow.** All changes stay uncommitted in the working tree; the user commits them themselves. Creating the feature branch (step 2) is the only write operation on the git history.

Work with the Bash tool (git, the relevant build/test tool) inside the project directory, alongside the regular file tools for reading and editing code. Project-specific commands and conventions are documented in the root `CLAUDE.md` — read it first.

## Step 0: Detect project tooling

Before building or testing anything, look at the project structure and identify the language/stack along with the matching commands, instead of guessing or assuming a single tool by default. Typical signals:

- `.csproj` / `.sln` → .NET (`dotnet build`, `dotnet test`)
- `package.json` → Node/TypeScript (check the scripts section, e.g. `npm run build`, `npm test`; test framework is usually in devDependencies, e.g. Jest, Vitest)
- `pom.xml` / `build.gradle` → Java/Kotlin (`mvn test` or `./gradlew test`)
- `requirements.txt` / `pyproject.toml` → Python (`pytest`, possibly `poetry run pytest`)
- `go.mod` → Go (`go build ./...`, `go test ./...`)
- `Cargo.toml` → Rust (`cargo build`, `cargo test`)

If a CI configuration already exists (e.g. `.github/workflows/`), read the actual build/test commands from there instead of reinventing them. If a repo has multiple stacks (e.g. backend + frontend), keep track of which tooling belongs to which part of the feature.

## Step 1: Sync the repo

```bash
git fetch origin
git checkout dev
git pull origin dev
```

Check beforehand (`git status`) whether there are uncommitted local changes that could block the checkout, and flag them to the user instead of silently discarding them (no `git reset --hard` or `git clean` without asking first).

## Step 2: Create the feature branch

Derive a short, descriptive branch name from the feature name, in the format `feature/featurename` (lowercase, hyphens instead of spaces, e.g. `feature/csv-export`). If the user already specified a name, use it instead of inventing your own.

```bash
git checkout -b feature/<featurename>
```

## Step 3: Make sure you understand the feature

Before planning or implementing anything, summarize in your own words what the feature should do, and identify gaps: unclear acceptance criteria, affected components/layers, edge cases, UI vs. pure logic, impact on existing APIs/data models. Take a look at the project structure (similar to step 0, plus the folder layout and any existing similar features) so your questions are concrete rather than generic.

Ask as many follow-up questions as needed, but bundle them into a single message instead of asking one at a time. Only move on to step 4 once the feature is clear enough to write a concrete plan. For a trivial, unambiguously described feature, follow-up questions can be skipped — don't force them artificially.

## Step 4: Write a plan and get it approved

Write a concise, concrete implementation plan — not a wall of prose, but something the user can skim and approve in a minute or two. Useful elements typically include:

- Affected projects/folders/modules/classes (new and changed)
- Rough implementation flow, broken into actionable sub-steps if useful
- Impact on existing interfaces, data models, or behavior
- Any open assumptions you made

**Wait for explicit approval from the user before starting step 5.** This is the most important checkpoint in the whole workflow — a wrong plan that's already been implemented costs far more time than a short question up front. If the user wants changes to the plan, revise it and have them confirm again.

## Step 5: Implement (without committing)

Implement the feature according to the approved plan. **Do not run `git commit`** (and do not `git add` / stage anything either) — the user will commit the changes themselves at the end. Keep the changes cleanly separable by topic instead (e.g. one concern per file or per group of files, no unrelated drive-by edits), so that step 8 can propose a sensible split into small, logically complete commits.

While working, keep a mental or scratchpad list of which files belong to which logical unit of work — you will need it for step 8.

Make sure the project builds (using the build command identified in step 0) after finishing larger parts of the implementation.

## Step 6: Write tests, run them, fix bugs

Write tests for the new behavior (happy path and relevant edge cases) using the test framework already established in the project (see step 0) — don't invent a new test structure or framework when one is already in place. Follow the layout of existing test files/folders in the repo.

Run the tests with the matching command (e.g. `dotnet test`, `npm test`, `pytest`, `go test ./...`, `cargo test`, `mvn test`).

If tests fail: fix the underlying bug in the feature code (don't bend the test to make it pass, unless the test itself was wrong). Repeat until the test suite passes cleanly. Do not commit.

## Step 7: Update documentation

Update the documentation relevant to the project — e.g. README, doc comments on new public functions/classes (in the idiom of the respective language, e.g. XML doc comments for C#, JSDoc for TypeScript, docstrings for Python), and the CHANGELOG if one exists. Check which documentation conventions already exist in the project (including the language used) instead of introducing new ones.

## Step 8: Propose a logical commit order

Do **not** commit, push, or open a pull request. Instead, finish by giving the user an ordered list of commits they can create themselves from the uncommitted changes. Check `git status` / `git diff` first so the list covers every changed and new file exactly once (nothing forgotten, nothing listed twice).

For each commit give:

1. **Commit message** – Conventional Commits, **in English** (even though the conversation with the user is in German), e.g. `feat(csv-export): add CsvExportService with basic mapping`.
2. **Files** – the exact paths to stage (`git add <paths>`). If a single file has to be split across commits, say so and name the hunks (`git add -p`).

Ordering rules:

- Dependencies first: a commit should only rely on code from earlier commits, so the project builds after each one (using the build command from step 0).
- Smallest logically complete increments (e.g. model → service → endpoint/UI), not one big commit.
- Tests directly after (or together with) the code they cover; documentation last.
- Refactorings, config/CI changes and unrelated housekeeping in their own commits.

Format the result as a numbered list, for example:

```markdown
1. `feat(csv-export): add CsvExportService with basic mapping`
   `git add src/Services/CsvExportService.cs`
2. `feat(csv-export): wire export button in UI`
   `git add src/Pages/Export.razor`
3. `test(csv-export): cover empty dataset edge case`
   `git add tests/CsvExportServiceTests.cs`
4. `docs(csv-export): document CsvExportService usage`
   `git add README.md`
```

Close by reminding the user that the branch is `feature/<featurename>` and that the push and the pull request against `dev` (title and description in English) are left to them.
