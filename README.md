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
- calculated order willingness rather than a single magic loyalty stat;
- calculated political threat based on influence, ambition, hostility and allegiance;
- relationships between courtiers, allowing alliances and rival blocs to emerge;
- coup plots that begin, grow, decay, recruit supporters, become discoverable and resolve against regime strength;
- supporters who dynamically join or leave conspiracies as relationships change;
- player counterplay through Chancellor-led investigations and direct dismissal of office-holders;
- investigations that may expose or disrupt a plot, but can be inconclusive and create grievances;
- a ruler, office-holders and potential advisers;
- a player-controlled political lineage, currently demonstrated as a dynasty;
- explicit succession separate from player continuity;
- succession within the player's lineage, or loss if an outsider takes power;
- loss through usurpation even when the usurper belongs to the same player lineage;
- monthly political drift: office changes influence and fear slowly fades;
- a country, government, treasury, GDP, tax rate, stability and unrest;
- adviser-mediated tax changes, including outright refusal by a sufficiently hostile Treasurer;
- monthly tax revenue and political reports;
- a court screen exposing known threats and the political state behind adviser behaviour.

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

A player action should usually create an intention or order. A character then carries it out. Their competence determines how well they can do it; their relationships, fear, ambition and allegiances determine how willing they are.

### Fear is not loyalty

A character may hate the ruler and still obey because they are afraid. That can make the government effective in the short term while leaving a dangerous political structure behind.

### Politics is a network

Characters do not relate only to the ruler. Friendship, trust and shared hostility between courtiers can turn an isolated rival into the centre of a coalition. Player actions can reshape that network unintentionally.

### Jobs are not character classes

Ruler, Marshal and Treasurer are roles held by people. Characters can gain and lose offices without becoming a different object type.

### The country is not the player

The simulation continues to model a country even if the player's political lineage is removed from power. Losing political control is a player loss, not destruction of the state.

## Near-term direction

The next systems should continue to grow from the same model:

1. government expenditure and recurring costs;
2. diplomacy mediated through a Chancellor or diplomats;
3. mortality, illness, abdication and other causes of succession;
4. imprisonment, exile, pardons and legal/political consequences for dealing with conspirators;
5. succession laws and institutions that derive candidates instead of using a fixed list;
6. larger factions and interest groups built from the relationship network;
7. only then, a first minimal war system.
