# Save / Load System

Leader Game uses an explicit, versioned JSON save format.

The goal is not merely to reproduce the visible dashboard. A loaded campaign should resume the same simulation state and, where deterministic randomness is concerned, the same future timeline.

## Current format

Current save version: **1**

The default desktop slot is stored under the operating system's local application-data directory:

```text
LeaderGame/saves/campaign.json
```

The exact root is platform-specific. Hovering the desktop Save or Load button shows the full path.

Saves are written atomically through a temporary file before replacing the active slot.

## What is preserved

A save currently stores:

- simulation date;
- both deterministic RNG states;
- player country, current character and political lineage;
- republican term length, election countdown, campaign window and player-lineage election victories;
- selected scenario, campaign objective definitions, progress and completion dates;
- campaign victory state and reason;
- every character and their mutable attributes;
- character allegiances and power-base standing;
- country economy, military, government and political structure;
- rulers, political figures and succession orders;
- country topology / neighbours;
- directional personal relationships;
- diplomatic relations and trade agreements;
- wars, IDs, status, stances and war score;
- political plots and discovery state;
- power-base demands, stable IDs, named spokespeople, ruler acknowledgement/rejection state and resolution dates;
- organised opposition blocs;
- diplomatic proposals;
- pending player orders;
- simulation reports;
- player knowledge / last-known information;
- adviser report history and caveats;
- pending information requests;
- hidden historical information snapshots;
- pending, accepted, rejected and expired cabinet recommendations, including the adviser's stated rationale.

Object relationships are persisted through stable IDs and rebuilt during loading. The loader rejects broken references rather than attempting to continue with a partially corrupted state.

## Deterministic continuation

`SimulationRandom` exposes its exact internal state.

Both the main simulation RNG and the independent information RNG are saved and restored. Tests verify that the next sequence of random values is identical after a round trip.

This is important because reloading should not change later battles, illness, plots or misinformation simply because the campaign was saved.

## Pending orders

Pending orders are serialised explicitly by order kind instead of using general-purpose polymorphic JSON.

Supported order types currently include:

- tax changes;
- budget changes;
- appointments and dismissals;
- investigations;
- arrests and prisoner releases;
- diplomatic outreach;
- trade negotiations and termination;
- declarations of war;
- campaign stance changes;
- peace offers;
- diplomatic-proposal responses;
- adviser report requests.

Adding a new order type should also add an explicit save representation and round-trip test.

## Versioning

The root snapshot contains a numeric save-format version.

A loader currently rejects versions it does not understand rather than guessing.

When the format changes, prefer an explicit migration from older versions to the newest in-memory snapshot. Do not reinterpret an old field with a new meaning.

## Variable-resolution time

The current simulation clock is still `GameDate(year, month)`, so save version 1 records year and month.

Variable turn lengths are an intended future feature: peaceful stretches may advance months or years, while elections, coups and acute crises may operate in days or finer resolution.

When the simulation clock is replaced with a richer time representation, the save format should migrate the current `DateSnapshot` rather than repurposing its fields. Existing version-1 saves can be interpreted as a timestamp at a documented point within their recorded month.

This is one reason the save file is versioned now, before variable-time work begins.

## Desktop behaviour

The desktop currently exposes a manual campaign slot plus a separate recovery autosave:

- **Save** writes the current campaign to `campaign.json`.
- **Load** reconstructs the manual campaign slot.
- every completed turn writes `autosave.json` separately;
- **Auto** recovers that autosave without overwriting the manual slot;
- loading rebuilds the entire `PlayerViewState` from the restored world.

Multiple named slots and a file-picker UI remain future work.

## Tests

Persistence tests currently verify:

- broad world-state round trips;
- player knowledge survives loading;
- pending orders reconnect to the restored object graph;
- both RNG streams continue identically;
- atomic file saving and file loading work.
- selected scenario, campaign-objective progress and victory state survive round trips;
- political-demand spokespeople reconnect to restored character objects.

Future systems that add mutable simulation state should extend these tests.
