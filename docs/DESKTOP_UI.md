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

### Campaign chooser

A fresh desktop launch begins at a campaign chooser rather than immediately starting one country.

The chooser currently offers:

- **The Falken Crown** — Friedrich von Falken / Falkenreich / House von Falken;
- **The Valerian Republic** — Marco Vieri / Valeria / Vieri Coalition;
- continuing the manual save;
- recovering the latest turn autosave.

The same simulated world exists behind both starts, but the player country, lineage, initial intelligence and victory objectives differ.

### Briefing

The home screen includes:

- current country, date, ruler and lineage;
- reported treasury, debt, army strength, readiness, stability and political backing;
- source, uncertainty and age for those estimates;
- stale-information warnings;
- semantic category colours;
- recent adviser reports and simulation events;
- queued-order and pending-inquiry counts;
- a dedicated orders desk showing queued instructions and recent resolutions;
- cabinet recommendations that can be accepted or rejected;
- campaign objectives with consecutive-month progress and completion state;
- explicit campaign victory / political defeat banners;
- live adviser cards showing qualitative ability, trust, obedience and political risk;
- advancing the simulation by one month and returning directly to the new briefing.

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
- active political-demand cards with named spokespeople, age and escalation;
- **Concede** and **Reject** responses to political demands;
- the wider set of political figures;
- qualitative ambition, influence, trust and constituencies;
- known conspiracy evidence only where investigations have actually uncovered it;
- appointment candidate selection;
- office selection;
- appointment and dismissal orders;
- investigation orders through the Chancellor;
- arrest orders through the Marshal;
- direct prisoner-release orders by the ruler.

The desktop filters appointment candidates according to the simulation's actual eligibility rules.

Appointments and dismissals still enter the real order system and retain their political consequences.

Conceding a political demand does not directly edit policy. It queues the appropriate tax, budget or peace order and the demand remains active until the government actually delivers. Rejecting a demand is a direct political act and creates immediate backlash.

Arrests deliberately remain risky. Court cards expose discovered evidence, not the hidden existence or strength of undiscovered plots. Failed arrests and investigation outcomes are reported qualitatively rather than revealing internal simulation scores.

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

### Military

The Military surface works in both peace and war.

It includes:

- current reported army strength, readiness and war exhaustion;
- the Marshal and qualitative obedience;
- active campaign selection;
- reported campaign position;
- own-force and enemy-force estimates;
- campaign stance orders;
- fresh enemy-intelligence requests;
- peace proposals with white peace, demanded reparations or offered reparations.

Battlefield position and enemy strength are always mediated by player knowledge. A campaign can therefore change before the player's screen catches up.

### Economy

The Economy surface is explicitly a **reported-history** screen rather than an omniscient ledger.

It includes:

- current reported GDP, treasury, debt, revenue, expenses, monthly balance and administrative efficiency;
- the Treasurer and qualitative obedience;
- a fresh-report request;
- successive historical Treasury snapshots;
- delivered date, data age, source and origin;
- reported treasury, debt, revenue, expenses and balance for each historical report;
- contradiction/caveat indicators.

Historical rows are not corrected after later information arrives. If the Treasurer first reports one figure and later revises it, both claims remain visible.

### Archive

The Archive is a searchable record of what reached the ruler.

It combines:

- adviser reports;
- order outcomes;
- diplomatic events;
- military events;
- political events;
- other player-visible government records.

It supports free-text search and category filtering. It deliberately stores the player-visible record rather than hidden simulation truth.

## Current coverage

All primary desktop navigation surfaces are now functional:

- Briefing;
- Government;
- Court;
- Economy;
- Foreign Affairs;
- Military;
- Intelligence;
- Archive.

The next desktop work is depth and polish rather than replacing placeholders.

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

With the primary screens now covered, the next priorities are:

1. **Multiple named save slots** and better save-slot management; scenario selection, manual save/load and turn autosave already work.
2. **Deeper court interaction** such as investigations, arrests, prisoners and direct political responses.
3. **Richer autonomous agendas** so advisers, factions and foreign governments create more decision pressure.
4. **Economy visualisation** using reported historical series rather than hidden data.
5. **Military depth** around commanders, fronts, logistics and uncertain territorial control.
6. **Keyboard/accessibility polish**, tooltips, empty states and better narrow-window behaviour.
7. **Map work later**, treated as another uncertain information surface rather than the centre of the game.

At this point the desktop client can cover the normal high-level play loop without relying on the console for its main management surfaces.
