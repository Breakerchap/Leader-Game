# Campaigns, Starting Positions and Victory

Leader Game now supports multiple starting political positions inside the same simulated world.

A scenario is not a separate ruleset or scripted event chain. It selects:

- the country the player begins in;
- the ruler / political character initially controlled;
- the player's political lineage;
- the opening strategic problem;
- the campaign victory objectives;
- the initial player-information briefings.

The underlying countries, characters, relationships and simulation systems remain shared.

## Current starting campaigns

### The Falken Crown

**Country:** Falkenreich  
**Leader:** Friedrich von Falken  
**Lineage:** House von Falken — Dynasty

A wealthy but politically brittle feudal monarchy. The army, aristocracy and regional elites all possess enough independent weight to threaten the crown if badly handled.

Victory requires all three objectives:

1. **Secure the Crown** — maintain true government stability of at least 70 and overall ruler backing of at least 60 for six consecutive months.
2. **Sound Finances** — hold at least 1,000,000 in the treasury with a non-negative monthly balance for three consecutive months.
3. **Peace of the Realm** — remain outside active war with public unrest at or below 30 for six consecutive months.

### The Valerian Republic

**Country:** Valeria  
**Leader:** Marco Vieri  
**Lineage:** Vieri Coalition — Party

A richer merchant republic with a strong bureaucracy and commercial class but less dynastic insulation from political failure.

Victory requires all three objectives:

1. **Merchant Network** — maintain trade agreements with both neighbouring states.
2. **Commercial Reserves** — hold at least 2,000,000 in the treasury with a non-negative monthly balance for three consecutive months.
3. **Normalise Falkenreich** — maintain formal relations with Falkenreich at +10 or better for three consecutive months.

## Objective behaviour

Objectives are evaluated from the underlying simulation state.

For objectives requiring sustained conditions, progress is consecutive. Falling below the requirement resets the current streak.

Once an objective is completed it remains completed.

When every objective in the active campaign is complete:

- `PlayerState.HasWon` becomes true;
- a campaign-victory report is produced;
- normal order submission and time advancement stop;
- the desktop displays the victory state.

The player-facing objective panel deliberately does not expose the hidden exact value of uncertain metrics. It reports the objective wording and qualifying-month progress. This preserves the information game: for example, the simulation may know that stability is above 70 while the ruler only has an imperfect Chancellor estimate.

## Scenario selection

The desktop opens on a campaign chooser rather than silently beginning a particular country.

The chooser currently supports:

- starting either new campaign;
- continuing the manual save;
- recovering the latest autosave.

The console host also asks for a starting campaign before creating the simulation.

## Persistence

Scenario identity, objective definitions, current qualifying-month progress, completion dates, and victory state are all saved.

Older saves without campaign data are assigned the scenario that best matches their player country when loaded.

# Autonomous World Dynamics

The world is intended to move without waiting for the player.

## Economic and demographic drift

Each month, every country's GDP and population change gradually.

GDP responds to:

- administrative efficiency;
- government stability;
- public unrest;
- trade agreements;
- debt burden;
- active war and war exhaustion.

Population responds more slowly to:

- stability;
- unrest;
- war;
- war exhaustion.

The rates are deliberately small. These systems create long-run trajectories rather than instant economic events.

The player does not receive these true values directly. Changes become visible through later adviser reports.

## Structural political change

Every quarter, the structural strength of political power bases can change.

Examples:

- successful trade and moderate taxation can strengthen merchants;
- sustained administrative investment can strengthen bureaucracy;
- war or heavy military funding can strengthen the military;
- court/patronage spending can strengthen aristocratic, clerical or royal institutions;
- stable republics can strengthen organised party politics.

This means the political system itself changes over time. A constituency that was secondary at campaign start can become important later.

## Foreign-government adaptation

Non-player governments now change policy in response to their own conditions.

They may:

- raise military funding during war or poor readiness;
- cut military spending during expensive peace;
- increase administrative spending when state capacity is weak;
- raise taxes under debt or persistent deficits;
- lower taxes when unrest becomes dangerous;
- increase patronage spending to stabilise a weak regime;
- trim expensive court spending when stable but insolvent.

Foreign governments are therefore no longer frozen copies of their scenario-start budgets.

## Autonomous diplomacy between AI states

AI-controlled countries can form trade agreements with each other when relations, trust and tension permit.

They can also abandon trade relationships when hostility becomes severe.

Hostile AI relationships can worsen over time, and sufficiently hostile neighbouring AI states can declare war on one another when an ambitious ruler believes they possess enough military leverage.

Aggression involving the player remains routed through the more legible diplomatic/ultimatum systems rather than silently spawning arbitrary wars.

# Personalised Political Pressure

Power-base demands now have named spokespeople where a credible political figure exists.

A spokesperson is chosen from politically active figures using:

- their standing with the demanding constituency;
- influence;
- ambition;
- willingness to obey the ruler;
- whether their office naturally represents the constituency.

Examples include a Marshal fronting military demands or a Treasurer carrying merchant/bureaucratic pressure.

If the demand is satisfied:

- the ruler gains group support;
- the spokesperson gains political credit and some influence;
- their opinion and trust of the ruler improve.

If the demand is ignored and escalates:

- the spokesperson becomes a stronger alternative focal point;
- their constituency standing and influence rise;
- their relationship with the ruler deteriorates.

If the spokesperson becomes politically inactive, the group can find a replacement.

The ruler has three meaningful responses:

- **concede** — acknowledge the demand and queue the real policy order; the petition remains active until that order actually produces the requested condition;
- **reject** — close the immediate petition but take an immediate hit to constituency backing, stability, unrest and the spokesperson relationship;
- **ignore** — allow the existing escalation clock to continue.

A rejected group cannot immediately submit the same petition again; there is a six-month cooldown. The grievance itself may still matter to opposition formation because rejection usually worsens underlying backing.

This is intended to turn abstract political pressure into relationships between people rather than a collection of disconnected meters.

## Organised opposition actions

An opposition bloc is no longer only a modifier to threat and coup calculations.

Every third month that an active bloc survives, it takes one explainable political action:

- **institutional obstruction** when the bloc has office-holders inside government, reducing state capacity and confidence;
- **coordinated public pressure** when cohesion is high, strengthening the bloc leader while weakening the ruler with aligned constituencies;
- **recruitment and consolidation** when the bloc is still building, drawing other political figures closer to its leader.

These actions apply to AI countries as well as the player country. The player receives reports only for politically visible activity in their own government.

The effects are intentionally incremental. Blocs should create a deteriorating political environment that can still be repaired rather than jumping directly from opposition to coup.
