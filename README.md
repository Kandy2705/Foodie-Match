# Foodie Match - Plate Serving Puzzle

Welcome! This is the repository for **Foodie Match**, a small casual plate-serving puzzle game developed with **Unity 6000.0.58f2**.

Foodie Match is a prototype puzzle where players serve customers by matching food items from a shared tray while managing limited storage plates. The game focuses on a simple, strategic serving loop.

---
## About the Game
Customers order plates requiring one, two, or three matching food items. Players:
- Pick food from the tray
- Place correct items into active customer plates
- Use empty storage plates for extras
- Serve completed plates
- New orders appear after serving
- Clear the tray to finish the level

The game is easy to learn but requires careful planning for each move.

---
## Core Gameplay Loop
```text
Customer requests plates
→ Player selects food from tray
→ Place food into the correct order plate
→ Use empty plates as temporary storage
→ Complete the requested plate
→ Serve the plate to the customer
→ A new plate request appears
→ Clear all food from the tray
```

---

## Tech Stack

This project is currently developed with:

| Tool | Usage |
|---|---|
| Unity 6000.0.58f2 | Main game engine |
| C# | Gameplay, UI, and game systems |
| Git | Version control |
| GitHub | Repository hosting and collaboration |
| Husky | Local Git hooks |
| Commitlint | Commit message validation |
| Gitmoji | Emoji-based commit style |

---

## Commit Convention

This project uses **Husky**, **Commitlint**, and **Gitmoji** to keep commit messages consistent.

Commit format:

```text
type: Commit message
type: emoji Commit message
type(scope): Commit message
type(scope): emoji Commit message
```

Examples:

```text
feat: Add food selection logic
feat: ✨ Add food selection logic
fix(plate): Fix plate completion detection
docs(readme): 📝 Update project overview
chore(project): 🔧 Setup commit validation
```

Allowed types:

```text
feat, fix, docs, style, refactor, test, chore, build, ci, perf, revert
```

Commits with invalid format will be rejected by the Git hook.

