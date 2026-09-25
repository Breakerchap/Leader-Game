# Desktop UI

The desktop client is built with **Avalonia UI 12** on .NET 9.

The GUI is intentionally a thin player-facing host around the simulation rather than a second implementation of game logic.

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

Instead the desktop asks `GameSession` for a `PlayerViewState`. That state is assembled from:

- persistent player knowledge and adviser estimates;
- report history;
- public events and formal government acts;
- qualitative political impressions;
- the player's own orders and policies.

This is a game-design rule, not just an MVVM convenience. Accidentally binding a label directly to hidden simulation truth would undermine the unreliable-information system.

Presentation-boundary tests explicitly check that hidden values do not leak into player-facing views.

## Visual language

The current desktop style is deliberately institutional rather than medieval parchment.

Colour is semantic rather than decorative:

- green — economy and fiscal policy;
- red — military information and danger;
- violet — court and domestic politics;
- blue/cyan — diplomacy and intelligence;
- amber — uncertainty, staleness, contradictions and attention;
- grey — ordinary administration and neutral system information.

The UI also uses badges, side rails, symbols and warning panels so information is not communicated by colour alone.

Navigation shows a coloured selection rail for the active screen.

## Current desktop surfaces

### Briefing

The home screen includes:

- current country, date, ruler and lineage;
- reported treasury, debt, army strength, readiness, stability and political backing;
- source, uncertainty and age for those estimates;
- stale-information warnings;
- semantic category colours;
- recent adviser reports and simulation events;
- queued-order and pending-inquiry counts;
- live adviser cards showing qualitative ability, trust, obedience and political risk;
- advancing the simulation by one month.

The intention is that advancing time normally returns the player to a government briefing rather than to a map.

### Intelligence

The Intelligence surface includes:

- requesting an economic report from the Treasurer;
- requesting an own-army report from the Marshal;
- requesting a domestic political assessment from the Chancellor;
- selecting another country for diplomatic or military intelligence;
- pending inquiries;
- report history;
- estimates, uncertainty margins and apparent confidence;
- contradictions and caveats;
- report cards visually keyed to the office that produced them.

The request enters the real simulation as an order. Clicking a button does not instantly reveal hidden information.

### Government

The Government surface is now interactive.

It includes:

- the current Treasurer and qualitative obedience assessment;
- the formal enacted tax rate;
- a tax target slider and tax directive order;
- formal army, administration and court/patronage funding;
- funding target sliders;
- a budget directive order;
- a clear reminder that orders are queued and may be moderated or refused.

Formal policy is shown exactly because it is a government act the ruler can know. Its real effects remain mediated by reports.

### Court

The Court surface includes:

- Chancellor, Treasurer and Marshal office cards;
- qualitative ability, trust, obedience and political risk;
- known organised opposition;
- known political demands;
- the wider set of political figures;
- qualitative ambition, influence, trust and constituencies;
- appointment candidate selection;
- office selection;
- appointment orders;
- dismissal orders.

The desktop filters appointment candidates according to the simulation's actual eligibility rules.

Appointments and dismissals still enter the real order system and retain their political consequences.

### Foreign Affairs

The Foreign Affairs surface includes:

- the current Chancellor and qualitative obedience assessment;
- selecting a country desk;
- reported relations, trust and tension;
- reported foreign GDP and military strength;
- public trade status;
- public war status;
- requesting a fresh Chancellor assessment;
- requesting foreign military intelligence;
- diplomatic outreach orders;
- proposing or ending trade agreements;
- declarations of war where structurally valid;
- incoming diplomatic proposals with accept/reject actions.

Foreign relations and state capabilities are shown from player knowledge. Treaties and formal war status are exact because they are public state acts.

### Remaining placeholders

The remaining major placeholder surfaces are:

- Economy;
- Military;
- Archive.

Those should be migrated behind the same `GameSession` boundary rather than reading simulation internals directly.

## Run the desktop client

```bash
dotnet run --project LeaderGame.Desktop/LeaderGame.Desktop.csproj
```

The console client remains available:

```bash
dotnet run --project LeaderGame.csproj
```

## Visual direction

The long-term direction is to keep the information architecture stable while allowing the presentation to evolve with the era and regime.

Examples might include royal correspondence and seals in an early-modern court, ministry files and telegrams in the nineteenth century, typed intelligence folders in the twentieth century, and digital government systems in a modern state.

The interface should remain readable and game-like rather than becoming a literal office-document simulator.

## Next desktop priorities

The highest-priority remaining surfaces are:

1. **Military** — campaign stance, peace offers, commanders and fogged field intelligence.
2. **Economy** — historical charts built from **reported estimates**, including contradictions between successive reports.
3. **Archive** — a searchable history of what the ruler was told, ordered and believed at the time.

After those are functional, the desktop client should be able to replace nearly all normal console play.

A geographic map should come later and behave as another uncertain information surface rather than the central object of play.
