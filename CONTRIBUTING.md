# Contributing Guidelines

## Commit Message Convention

We use the **Conventional Commits** style:

```git
<type>(<scope>): <short summary>
```

### Common Types

| Type | Purpose |
|------|----------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation or comments |
| `style` | Formatting or style-only changes |
| `refactor` | Code refactoring |
| `perf` | Performance improvements |
| `chore` | Maintenance, configuration updates |
| `test` | Adding or modifying tests |
| `meta` | Project management materials: meeting summaries, planning notes, brainstorming docs |
| `add` | New files of every kind (must be relevant) |
| `remove` | Deleting files of every kind (must be relevant) |

### Examples

```git
feat(input): add sprint action
fix(camera): correct follow target reference
docs(gitignore): clarify intention of local packages folder
chore: ignore .vscode directory
```

## Branching Convention

### Main Branches

- `develop`: default unstable branch for ongoing development

Branches should be named using the following pattern:

```git
<type>/<short-description>
```

Optionally include issue IDs:

```git
feat/42-input-sprint-action
```

The target branch is usually `develop`.
For urgent fixes, use `hotfix` branches targeting `main`.

| Type | Purpose |
|------|----------|
| `feat` | New feature |
| `fix` | Bug fix |
| `hotfix` | Urgent fixes. Target branch: `main` |
| `docs` | Documentation or comments |
| `style` | Formatting or style-only changes |
| `refactor` | Code refactoring |
| `chore` | Maintenance or configuration updates |
| `test` | Adding or modifying tests |

Examples:

```git
feat/input-sprint-action
fix/camera-follow-target
docs/readme-update
```

## PR's Title Convention

| Type | Purpose |
|------|----------|
| `feat` | New feature |
| `fix` | Bug fix |
| `hotfix` | Urgent fixes. Target branch: `main` |
| `docs` | Documentation or comments |
| `style` | Formatting or style-only changes |
| `refactor` | Code refactoring |
| `chore` | Maintenance or configuration updates |
| `test` | Adding or modifying tests |

Examples:

```git
feat: add input sprint action
fix: correct camera follow target
docs: update readme
```

## Code Style

- KISS!
- Meaningful names
- Consistent formatting

### Variable Naming

#### Names

- Use **nouns** for variables and properties (e.g., `playerScore`) because they represent data or state.
- Use **verbs** for functions and methods (e.g., `CalculateScore()`) because they perform actions or calculations.
- Prefix boolean variables and methods with a **verb** that expresses condition or ability (e.g., `isGameOver`, `hasKey`, `IsPlayerHit()`) to clearly signify true/false values.
- C# events start with **On** + **subject** + **Action** (e.g., `OnPlayerDeath`) to indicate they notify something happening.
- Methods that are triggered when "something happens", but **are not directly tied to an event subscription**, also start with **On** + **subject** + **Action** (e.g., `OnPlayerDeath()`) to represent internal logic invoked at those moments.
- Variables holding ScriptableObject (SO) events end with **Event** (e.g., `playerDeathEvent`) to clearly identify them as event objects.
- Methods that **raise (trigger)** SO events start with **Raise** (e.g., `RaisePlayerDeathEvent()`) to show their role in firing the event.
- Methods that **handle an event** end with **Handler** (e.g., `PlayerDeathEventHandler()`) indicating they respond as subscribers to the event.
- Scripts that inherit from ScriptableObjects begin with **SO** (e.g., `SOEnemyData`) to help recognize their type quickly.
- Interfaces start with a capital **I** (e.g., `IInteractable`) following .NET conventions to clearly distinguish them.

#### Casing Schemes

- use `PascalCase` for public variables, properties, enums, functions, Scriptable Objects (e.g. `PlayerScore`, `CalculateScore()`)
- use `camelCase` for [SerializeField] (e.g. `playerScore`)
- use `_camelCase` for private and protected variables (e.g. `_playerScore`)
- use `UPPER_SNAKE_CASE` for constants (e.g. `MAX_HEALTH`)

### Ordering

In general, follow this order inside your scripts:

  1. Constants
  2. Serialized fields
  3. Private fields
  4. Unity lifecycle methods (`Awake`, `OnEnable`, `OnDisable`, `Start`, `Update`, `OnDestroy`)
  5. Private methods
  6. Public methods
  7. Event handlers
  
If you have a lot of code in one script, consider using `#region` blocks to organize related portions of code together.

## Pull Requests

Before opening a pull request:

1. Make sure your branch is up to date with the base branch.
2. Test the project in Unity to verify your changes.
3. Use a clear PR title following the conventions above.
4. Add a short description of what the PR does.
5. Link related issues if any (e.g. `Closes #42`).
6. Request reviews from relevant team members.
7. Keep taking care of any feedback until the PR is approved and merged