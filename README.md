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
- player lineage continuity, succession and usurpation;
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

### State capacity costs money

Army readiness and administrative effectiveness require sustained funding. Cutting expenditure solves a fiscal problem now by creating a capability problem later. Debt preserves capacity temporarily, but eventually damages political stability.

### Jobs are not character classes

Ruler, Marshal and Treasurer are roles held by people. Characters can gain and lose offices without becoming a different object type.

### The country is not the player

The simulation continues to model a country even if the player's political lineage is removed from power. Losing political control is a player loss, not destruction of the state.

## Near-term direction

The next systems should continue to grow from the same model:

1. diplomacy mediated through the Chancellor and diplomats;
2. mortality, illness, abdication and other causes of succession;
3. imprisonment, exile, pardons and legal/political consequences for conspirators;
4. succession laws and institutions that derive candidates instead of using a fixed list;
5. larger factions and interest groups built from the relationship network;
6. trade and economic shocks that make fiscal policy react to the outside world;
7. only then, a first minimal war system.
