# Leader Game

An experimental grand-strategy simulation where the player controls a ruler or political lineage rather than directly controlling a state.

The core chain is:

```
player decision
    -> order
    -> character willingness / interpretation / competence
    -> implementation
    -> world consequence
    -> report to the player
```

The player should not be able to directly set the world state just because they clicked a button.

A second important distinction is that **the player is not the country**. The state can continue after the player's dynasty, party or faction has lost power.

## Current prototype

The prototype now links court politics and state capacity rather than treating them as separate minigames:

- directional personal relationships: opinion, trust and fear;
- allegiance to countries and political lineages;
- competence, ambition, legitimacy and political influence;
- calculated willingness to obey orders;
- relationships between courtiers, coalition-building and rival blocs;
- deterministic coup plots that grow or decay from political conditions;
- investigations, dismissals and the grievances they create;
- Marshal-mediated arrests whose success depends on competence, willingness and army readiness;
- political imprisonment that removes characters from office, plots and succession while confined;
- arbitrary arrests that cost legitimacy, stability and relationships;
- failed arrests that can harden opposition into an active coup plot;
- release and political rehabilitation of prisoners;
- deterministic seeded ageing, illness, recovery and natural mortality;
- natural ruler death flowing directly into the succession system in the same monthly tick;
- player lineage continuity, succession and usurpation;
- three fully simulated countries in the prototype world, each with its own ruler, court, economy and succession;
- bilateral state relations tracking relations, trust, tension, borders and treaties;
- persistent border disputes that generate strategic tension over time;
- Chancellor-mediated diplomatic outreach, trade negotiation and non-aggression pacts;
- trade agreements that generate bilateral monthly income and slowly build trust;
- non-aggression pacts that suppress escalation without erasing underlying territorial disputes;
- ruler-to-ruler personal relationships that influence diplomacy and change with succession;
- autonomous foreign governments that can initiate trade or non-aggression proposals without deciding for the player;
- pending proposals that can be accepted, rejected or allowed to expire through the Foreign Affairs screen;
- explicit treaty-breaking costs: declaring war through a non-aggression pact damages trust, legitimacy and domestic stability;
- a first abstract war model with declarations, Marshal-mediated campaign stances, monthly losses, readiness and war exhaustion;
- monthly taxes and recurring government expenditure;
- army, administration and court/patronage budgets;
- Treasurer-mediated budget changes rather than direct sliders;
- administrative efficiency and army readiness that respond to funding;
- cash reserves, deficits, debt and monthly debt interest;
- fiscal stress feeding back into unrest and government stability;
- reports and court information that expose what the player could reasonably know.

Run it with:

```bash
dotnet run
```

Run the tests with:

```bash
dotnet test LeaderGame.Tests/LeaderGame.Tests.csproj
```

## Design principles

### People, not buttons

A player action usually creates an intention or order. A character then carries it out. Their competence determines how well they can do it; their relationships, fear, ambition and allegiances determine how willing they are.

### Fear is not loyalty

A character may hate the ruler and still obey because they are afraid. That can make the government effective in the short term while leaving a dangerous political structure behind.

### Politics is a network

Characters do not relate only to the ruler. Friendship, trust and shared hostility between courtiers can turn an isolated rival into the centre of a coalition. Player actions can reshape that network unintentionally.

### Randomness must be reproducible

Illness and mortality use a seeded simulation random source rather than ad-hoc random calls. The same state and RNG state reproduce the same future, and tests can inject exact random sequences.

### Treaties contain conflict; they do not erase it

A border dispute is a structural grievance, while a non-aggression pact is a political commitment. A pact can lower tension and build trust without deleting the dispute. Breaking that commitment later should be remembered and costly.

### State capacity costs money

Army readiness and administrative effectiveness require sustained funding. Cutting expenditure solves a fiscal problem now by creating a capability problem later. Debt preserves capacity temporarily, but eventually damages political stability.

### Jobs are not character classes

Ruler, Marshal and Treasurer are roles held by people. Characters can gain and lose offices without becoming a different object type.

### The country is not the player

The simulation continues to model a country even if the player's political lineage is removed from power. Losing political control is a player loss, not destruction of the state.

## Near-term direction

The next systems should continue to grow from the same model:

1. war goals, peace negotiations and political/economic consequences of settlements;
2. defensive alliances and third-country responses to wars;
3. abdication and other non-medical causes of succession;
4. exile, pardons and more formal legal institutions around political repression;
5. succession laws and institutions that derive candidates instead of using a fixed list;
6. larger factions and interest groups built from the relationship network;
7. trade and economic shocks that make fiscal policy react to the outside world.
