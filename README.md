# Minimalists

A 3D real-time strategy game stripped to its essentials: capture structures, commit forces by percentage, and outthink AI opponents in anything from a duel to a four-way free-for-all.

[Play on itch.io](https://olivernealdev.itch.io/minimalists) · [Full technical breakdown](https://oliverneal.dev/minimalists.html) · [Portfolio](https://oliverneal.dev)

| | |
|---|---|
| **Role** | Solo developer |
| **Engine** | Unity 6 · C# |
| **Released** | August 2025 |
| **Genre** | Node-conquest RTS |
| **Opponents** | 1v1 up to four-way AI |

---

## Overview

Minimalists is a node-conquest RTS. Each map is a handful of structures (houses, turrets, headquarters) and every structure holds a live unit count. You grow your force by holding structures, and you take new ones by sending units at them. That is the entire game, and the restraint is the point: with no fog of war, no build orders and no resource screens, **every match is decided by judgement about where and how hard to commit**.

The minimalist presentation is a design constraint, not a budget one: colour is ownership, shape is function, and the number floating over a building is its entire status readout.

---

## Key systems

### Committing forces by percentage

The core strategic verb is the send: click a structure you own, click a target, and a slice of your garrison marches. The size of that slice is set by a **commitment slider of 25%, 50%, 75% or 100%**, which turns every attack into a risk decision. Full commitment captures faster but leaves the source structure exposed; small probes are safe but feed the defender's regeneration.

Structure variety layers on top: houses generate units over time, turrets project defensive power over their surroundings, and losing a headquarters is losing the match. Structures can be upgraded, raising their capacity so map control compounds.

### Enemy AI that plays the same game you do

The AI has no cheats and no extra information. It wins by making the same commitment decisions the player faces: which neutral structures are cheap to take, which enemy structures are poorly garrisoned, when to reinforce a border house and when to mass for a push on a headquarters.

Crucially it commits through the same percentage interface the player uses, so it is bound by the same rules. It sizes the attack from what the target is actually holding plus a margin, then clamps the result, which is what stops it either trickling in one unit at a time or emptying a node it still needs to defend:

```csharp
// Assets/Scripts/AIManager.cs
if (bestSourceNode != null && bestTargetNode != null)
{
    // Send a calculated percentage of units to be efficient.
    float unitsToSend = bestTargetNode.UnitCount + OFFENSIVE_ATTACK_MARGIN;
    float percentage  = unitsToSend / bestSourceNode.UnitCount;
    bestSourceNode.SendUnits(bestTargetNode, Mathf.Clamp(percentage, 0.1f, 1.0f));
    return true; // Offensive action taken.
}
```

### Six AI personalities, not one

There is not just one brain. The project ships **six distinct AI personalities**: a priority-list attacker, a pure scorer, one that coordinates multi-structure pushes, an economy-first generalist, a momentum player that shifts behaviour depending on whether it is winning or losing, and a warlord that sorts the map into frontline, support and economic structures relative to where its enemies are massed.

The real test is the **four-way free-for-all**, where a single match can pit several of those brains against each other at once: factions expanding into the same contested middle, punishing each other when they spread too thin, and turning on whoever looks weakest, the player included.

### Showing the move, not just making it

When you send units, the move itself is the feedback. The particles that represent them are mapped onto the actual **navigation path**, repositioned and turned to follow each corner and the slope of the ground, so a send reads as a living column of force moving across the map rather than a straight line.

### Designed to be read at a glance

Because the whole board is always visible, readability is the UI. The game sticks to strict visual rules: **one colour per faction, one silhouette per structure type, one number per building.** A player can parse a four-faction board state in a second, and the menus and match flow follow the same restraint.

---

## Project structure

```
Assets/Scripts/
  AIManager.cs … AIManager7.cs   The six AI personalities, all using the player's send interface
  GameManager.cs                 Match state, factions, win and loss
  ConstructController.cs         Per-structure unit counts, capture, upgrades, regeneration
  UnitController.cs              Units in transit and their arrival resolution
  NavMeshPathfinder.cs           Path queries behind the send
  AStarPathfinder.cs             Custom pathfinding used alongside the NavMesh route
  ProceduralArrow.cs             Send visualisation mapped onto the real navigation path
  NodeUIController.cs            The one-number-per-building readout
  InputManager.cs                Selection, targeting and the commitment slider
Assets/SOSs/                     ScriptableObject data: factions, constructs, turrets,
                                 mortars, houses, helipads, AI difficulty settings
```

36 scripts, roughly 5,300 lines of C#.

---

## Running it

```bash
git clone https://github.com/OliverNealDev/Minimalists.git
```

Open the project in **Unity 6 (6000.0.45f1 or newer)** and load the main menu scene from `Assets/Scenes`.

Or skip the editor and [play it in the browser on itch.io](https://olivernealdev.itch.io/minimalists).

---

## License

MIT. See [LICENSE](LICENSE).

---

## Author

**Oliver Neal**, gameplay programmer specialising in Unity and C#.

[oliverneal.dev](https://oliverneal.dev) · [itch.io](https://olivernealdev.itch.io) · [LinkedIn](https://www.linkedin.com/in/oliverjackneal/) · [GitHub](https://github.com/OliverNealDev)
