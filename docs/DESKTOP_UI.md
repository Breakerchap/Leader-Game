# Desktop UI

The desktop client is built with **Avalonia UI 12** on .NET 9.

The current GUI is intentionally a thin host around the simulation rather than a second implementation of game logic.

## Architecture

```text
Simulation source
      |
      v
LeaderGame.Core
  - simulation
  - player knowledge
  - GameSession presentation facade
      |
      +--------------------+
      |                    |
      v                    v
LeaderGame.csproj     LeaderGame.Desktop
console host          Avalonia desktop host
```

The important boundary is `GameSession`.

Desktop code should not bind directly to `GameState`, `Country.ArmySize`, treasury balances, diplomatic relationship scores, political calculations, or other hidden simulation values.

Instead the desktop asks `GameSession` for a `PlayerViewState`. That state is deliberately assembled from:

- persistent player knowledge and adviser estimates;
- report history;
- public events and formal government acts;
- qualitative political impressions;
- the player's own orders and policies.

This is a design rule, not just an MVVM convenience. Accidentally binding a label directly to hidden simulation truth would undermine the central unreliable-information mechanic.

## Current desktop surfaces

### Briefing

The initial home screen includes:

- current country, date, ruler and lineage;
- key reported metrics such as treasury, debt, army strength, readiness, stability and political backing;
- source, uncertainty and age for those estimates;
- stale-information warnings;
- recent adviser reports and simulation events;
- queued-order and pending-inquiry counts;
- live adviser cards showing qualitative ability, trust, obedience and political risk;
- advancing the simulation by one month.

The intention is that advancing time normally returns the player to a briefing rather than to a map.

### Intelligence

The current Intelligence surface includes:

- requesting an economic report from the Treasurer;
- requesting an own-army report from the Marshal;
- requesting a domestic political assessment from the Chancellor;
- selecting another country and requesting either diplomatic or military intelligence;
- pending inquiries;
- report history;
- reported estimates, margins and apparent confidence;
- contradictions and caveats surfaced by the information system.

The request still enters the simulation as a real order. Clicking the button does not instantly reveal information.

### Navigation placeholders

Government, Court, Economy, Foreign Affairs, Military and Archive are already represented in navigation so future screens grow inside one coherent shell.

They are deliberately placeholders until each system has a player-safe presentation model.

## Run the desktop client

```bash
dotnet run --project LeaderGame.Desktop/LeaderGame.Desktop.csproj
```

The console client remains available:

```bash
dotnet run --project LeaderGame.csproj
```

## Visual direction

The desktop client currently uses a restrained dark institutional style rather than medieval parchment.

That is deliberate: the final game spans centuries. The long-term direction is to keep information architecture stable while allowing visual presentation to evolve with the era and regime.

Examples might include royal correspondence and seals in an early-modern court, ministry files and telegrams in the nineteenth century, typed intelligence folders in the twentieth century, and digital government systems in a modern state.

## Next desktop priorities

The highest-priority screens after the shell are:

1. Government — policies, offices, budgets and orders.
2. Court — people, relationships, patronage and political blocs.
3. Foreign Affairs — treaties, incoming messages and Chancellor assessments.
4. Military — commands and fogged campaign information.
5. Economy — time-series charts built from **reported historical estimates**, not hidden truth.
6. Archive — the historical record of what the ruler was told and believed at the time.

A geographic map should come later and behave as another uncertain information surface rather than the central object of play.
