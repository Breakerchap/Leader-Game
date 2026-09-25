# Leader Game

A simulation-first grand-strategy prototype about **political power rather than direct control of a country**.

The player controls a ruler and the political lineage behind them. Decisions are normally issued as orders to real characters inside the simulation, who may carry them out well, badly, reluctantly or not at all depending on their competence, loyalties, relationships, fear, ambition and circumstances.

```text
player decision
    -> order
    -> character willingness / interpretation / competence
    -> implementation
    -> world consequence
    -> report to the player
```

The country is deliberately separate from the player. A state can survive a ruler, dynasty, party or faction losing power.

> **Project status:** early playable simulation with both a console host and an Avalonia desktop client. All primary desktop surfaces are interactive: Briefing, Government, Court, Economy, Foreign Affairs, Military, Intelligence and Archive.

See **[docs/FINAL_GOAL.md](docs/FINAL_GOAL.md)** for the intended final game.

## Current prototype

The current build uses a shared .NET 9 simulation core with two hosts: the original console client and an Avalonia desktop client. Both run the same three-country demo scenario and monthly simulation.

Implemented systems include:

- character attributes including competence, ambition, legitimacy, influence, age and health;
- directional personal relationships with separate opinion, trust and fear;
- allegiance to countries and political lineages;
- explicit political power bases: aristocracy, military, merchants, clergy, bureaucracy, workers, peasantry, regional elites, parties and royal families;
- country-specific power structures and character standing within those power bases;
- autonomous domestic demands from dissatisfied power bases;
- multiple power bases able to maintain simultaneous grievances and demands;
- organised opposition blocs that form around shared rival leaders when major groups become alienated;
- bloc cohesion and membership feeding back into political threat, conspiracy co-operation and willingness to obey the ruler;
- escalating political pressure when important groups are ignored, including rivals gaining support;
- material conditions such as underfunding, high taxes, unrest and war exhaustion changing political backing over time;
- calculated willingness to obey orders;
- advisers and offices including Treasurer, Marshal and Chancellor;
- appointment and dismissal of advisers, with patronage and dismissal reshaping power-base support;
- political threat calculations and court coalitions;
- investigations and discoverable coup plots;
- arrests, political imprisonment and prisoner release;
- failed or arbitrary repression producing political consequences;
- deterministic coups and usurpation;
- seeded illness, recovery, ageing and natural mortality;
- succession after a ruler's death;
- player lineage continuity and loss of political control;
- population, GDP, treasury, debt and tax revenue;
- army, administration and court/patronage budgets;
- administrative efficiency and army readiness responding to funding;
- tax and budget decisions changing the ruler's backing among affected power bases;
- deficits, debt and monthly debt interest;
- fiscal stress feeding into unrest and stability;
- bilateral diplomatic relations, trust and tension;
- borders/neighbours and trade agreements;
- Chancellor-mediated diplomatic outreach;
- autonomous foreign trade proposals and responses;
- tribute ultimatums and foreign pressure;
- ruler-to-ruler personal relationships affecting diplomacy;
- war declarations and autonomous military behaviour;
- campaign stances;
- army losses, readiness, war score and war exhaustion;
- peace negotiation with multiple terms;
- player knowledge stored separately from hidden simulation truth;
- persistent last-known estimates with source, uncertainty and staleness;
- adviser-initiated reports based on urgency and information age;
- requestable Treasurer, Marshal and Chancellor reports with refusal and multi-month procurement delays;
- competence, institutional quality, fog and data age affecting report accuracy;
- deliberate misinformation and selective omission by hostile or opposition-aligned advisers;
- visible contradictions between reports without revealing which source is correct;
- hidden historical truth snapshots so stale reports are genuinely based on older world state;
- a separate deterministic information RNG so reporting uncertainty does not perturb physical simulation outcomes;
- deterministic/random-source injection for reproducible simulation tests.

## How the simulation advances

A month currently processes roughly in this order:

1. character life, illness and mortality;
2. succession;
3. queued player orders;
4. economy;
5. domestic politics;
6. diplomacy;
7. foreign military AI;
8. active wars;
9. foreign policy;
10. political plots;
11. capture hidden information history;
12. process requested and adviser-initiated reports;
13. advance the date.

This order matters. For example, a ruler can die and succession can be resolved before that month's queued orders are processed.

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Any desktop platform supported by Avalonia for the GUI client

The desktop client uses Avalonia UI packages restored through NuGet. The simulation core itself remains UI-framework independent.

## Run

Clone the repository:

```bash
git clone https://github.com/Breakerchap/Leader-Game.git
cd Leader-Game
```

Run the desktop client:

```bash
dotnet run --project LeaderGame.Desktop/LeaderGame.Desktop.csproj
```

The console client remains available:

```bash
dotnet run --project LeaderGame.csproj
```

The desktop client is the intended direction for normal play; the console remains useful as a fallback and debugging surface.

## Tests

Run the test project with:

```bash
dotnet test LeaderGame.Tests/LeaderGame.Tests.csproj
```

The tests cover the major simulation systems, including appointments, budgets, taxes, relationships, politics, plots, succession, life simulation, diplomacy, foreign policy, war and peace.

## Project structure

```text
Leader-Game/
├── Program.cs
├── LeaderGame.csproj
├── LeaderGame.Core/
├── LeaderGame.Desktop/
├── Simulation/
│   ├── Characters/
│   ├── Countries/
│   ├── Diplomacy/
│   ├── Military/
│   ├── Information/
│   ├── Orders/
│   ├── Player/
│   ├── Politics/
│   ├── Randomness/
│   ├── Reports/
│   ├── Scenarios/
│   ├── Systems/
│   ├── GameDate.cs
│   ├── GameSimulation.cs
│   └── GameState.cs
├── LeaderGame.Tests/
├── docs/
│   ├── FINAL_GOAL.md
│   ├── INFORMATION_SYSTEM.md
│   └── DESKTOP_UI.md
└── .github/workflows/
```

### Important pieces

- **`Program.cs`** — legacy/current console host.
- **`LeaderGame.Core/`** — reusable simulation assembly plus the player-safe `GameSession` presentation boundary.
- **`LeaderGame.Desktop/`** — Avalonia desktop client with functional Briefing, Government, Court, Economy, Foreign Affairs, Military, Intelligence and Archive surfaces.
- **`Simulation/GameState.cs`** — the central hidden world state.
- **`Simulation/GameSimulation.cs`** — monthly simulation orchestration and order processing.
- **`Simulation/Information/`** — player knowledge, adviser reports, uncertainty, report requests and hidden historical snapshots.
- **`Simulation/Orders/`** — player intentions represented as structured orders.
- **`Simulation/Systems/`** — economy, politics, domestic power-base pressure, life, succession, diplomacy, plots, foreign policy, military AI and war processing.
- **`Simulation/Scenarios/DemoScenario.cs`** — the current hand-built prototype world.
- **`LeaderGame.Tests/`** — xUnit simulation tests.
- **`docs/FINAL_GOAL.md`** — long-term game design target.
- **`docs/INFORMATION_SYSTEM.md`** — design and implementation rules for unreliable adviser reports and player knowledge.
- **`docs/DESKTOP_UI.md`** — desktop architecture, information-boundary rules and GUI roadmap.

## Design rules

### The player is not omnipotent

Player actions should normally enter the world as orders, not direct edits to simulation values.

A ruler can ask a Treasurer to change spending, a Marshal to conduct an arrest or campaign, or a Chancellor to negotiate abroad. The relevant character still has to execute the instruction.

### Competence and loyalty are different

The best person for a job may also be politically dangerous.

A highly competent subordinate can execute an order efficiently while using the resulting office, army, information or prestige to build their own power.

Appointments are therefore political bargains as well as staffing decisions. Bringing a well-backed figure into government can improve the ruler's standing with their supporters, while the office itself gives that figure more access and prestige. Dismissing a popular office-holder can have the opposite effect.

### Fear is not loyalty

A character can dislike or distrust the ruler and still comply because they are afraid. That may make a government effective in the short term while making its political foundations more brittle.

### Politics is a network

Characters have relationships with one another, not just with the ruler. Coalitions and threats should therefore emerge from groups of people with shared interests or enemies.

### Power must come from somewhere

A character's influence is not only a consequence of holding office. The simulation tracks support from political power bases such as the army, aristocracy, merchants, bureaucracy, clergy, parties and royal family. Different countries give those bases different structural importance, and policy choices can strengthen or weaken the ruler's backing among them. Important dissatisfied groups can now form explicit demands; leaving those demands unresolved escalates pressure on the government and can shift support toward political rivals.

### State capacity costs resources

Administrative efficiency and military readiness require continued funding. Cutting expenditure can improve the budget while making future orders harder to implement.

### Information should be mediated

The player and the simulation now have separate information states.

The simulation uses exact hidden values. The player normally sees the latest adviser estimate, including who supplied it, how old its underlying information is, an uncertainty range and the adviser's apparent confidence.

Advisers may volunteer reports or be explicitly tasked. Requested work can be refused, delayed or slowed further while underway. Reports can be wrong because of incompetence, weak institutions, fog of war, ordinary mistakes, deliberate distortion or selective omission. A later report can visibly contradict an earlier one without revealing which source is correct.

See **[docs/INFORMATION_SYSTEM.md](docs/INFORMATION_SYSTEM.md)** for the current design and implementation rules.

### Randomness must be reproducible

Random events use an injectable seeded random source. Given the same state and random sequence, the simulation should produce the same result.

This makes failures debuggable and the simulation testable.

## Current limitations

The current version is deliberately small compared with the intended game.

It does **not** yet provide:

- a historical world or historical start dates;
- a geographic world map;
- hundreds of states or large populations of political actors;
- deep social groups, parties, religions or regional politics;
- dynamic constitutional institutions;
- a full production/trade/resource economy;
- detailed military logistics or territorial occupation;
- a knowledge/capability/adoption model for technology;
- dynamic time compression;
- save/load and campaign persistence;
- a geographic map and map-based territorial interface.

Those belong to the longer-term design rather than the current prototype.

## Long-term direction

The project is intended to grow by deepening the same simulation model rather than replacing it with unrelated systems.

The long-term goal is a centuries-spanning political simulation in which:

- rulers depend on imperfectly loyal people and institutions;
- information is incomplete and politically mediated;
- governments and regimes can change form;
- political lineages can survive deposition, opposition and exile;
- domestic politics, economics, diplomacy and war feed into one another;
- technology is modelled through knowledge, capability and adoption rather than a simple tech tree;
- alternate history emerges from simulated conditions instead of scripted event branches.

The full design target is documented in **[docs/FINAL_GOAL.md](docs/FINAL_GOAL.md)**.
