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

The console prototype currently models:

- characters whose political roles can change over time;
- directional personal relationships: opinion, trust and fear;
- separate allegiance to countries and political lineages;
- personal competence, ambition, legitimacy and political influence;
- calculated order willingness rather than a single magic "loyalty" stat;
- calculated political threat based on influence, ambition, hostility and allegiance;
- a ruler, office-holders and potential advisers;
- a player-controlled political lineage, currently demonstrated as a dynasty;
- explicit country succession separate from player continuity;
- succession within the player's lineage, or loss if an outsider takes power;
- monthly political drift: office changes influence and fear slowly fades;
- a country, government, treasury, GDP, tax rate, stability and unrest;
- queued player orders;
- adviser-mediated tax changes, including outright refusal by a sufficiently hostile Treasurer;
- appointing and replacing advisers, including personal grievances for dismissed officials;
- monthly tax revenue;
- reports explaining what happened;
- a court screen exposing the political state behind adviser behaviour.

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

A player action should usually create an intention or order. A character then carries it out. Their competence determines how well they can do it; their relationship with the ruler, fear, ambition and allegiances determine how willing they are.

### Fear is not loyalty

A character may hate the ruler and still obey because they are afraid. That can make the government effective in the short term while leaving a dangerous political structure behind.

### Jobs are not character classes

"Ruler", "Marshal" and "Treasurer" are roles held by people. Characters can gain and lose offices without becoming a different object type.

### The country is not the player

The simulation continues to model a country even if the player's political lineage is removed from power. Losing political control is a player loss, not destruction of the state.

## Near-term direction

The next systems should grow out of the political model rather than bypass it:

1. plots, conspiracies, coups and usurpation driven by accumulated political pressure;
2. relationships between non-ruler characters, allowing alliances and rival court blocs;
3. government expenditure and recurring costs;
4. diplomacy mediated through a Chancellor or diplomats;
5. mortality, illness, abdication and other causes of succession;
6. succession laws and institutions that derive candidates instead of using a fixed list;
7. only then, a first minimal war system.
