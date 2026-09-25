# Leader Game

An experimental grand-strategy simulation where the player controls a ruler or political lineage rather than directly controlling a state.

The important distinction is:

```
player decision
    -> order
    -> character interpretation / implementation
    -> simulation consequence
    -> report to the player
```

The player should not be able to directly set the world state just because they clicked a button.

A second important distinction is that **the player is not the country**. The state can continue after the player's dynasty, party or faction has lost power.

## Current prototype

The console prototype currently models:

- characters whose political roles can change over time;
- a ruler, office-holders and potential advisers;
- a player-controlled political lineage, currently demonstrated as a dynasty;
- explicit country succession separate from player continuity;
- succession within the player's lineage, or loss if an outsider takes power;
- a country, government, treasury, GDP, tax rate, stability and unrest;
- monthly simulation ticks;
- queued player orders;
- adviser-mediated tax changes;
- competence and loyalty affecting how faithfully a tax order is implemented;
- appointing and replacing advisers, including loyalty consequences for dismissed officials;
- monthly tax revenue;
- reports explaining what happened.

Run it with:

```bash
dotnet run
```

Run the tests with:

```bash
dotnet test LeaderGame.Tests/LeaderGame.Tests.csproj
```

## Project structure

```
Simulation/
  Characters/   people and their current political roles
  Countries/    country, government and succession state
  Orders/       player/AI intentions and their processing
  Player/       current character and political-lineage continuity
  Reports/      information returned to the player
  Scenarios/    starting world setups
  Systems/      world simulation systems
```

## Near-term direction

The next useful systems are deliberately small:

1. richer relationships and multiple kinds of loyalty;
2. government expenditure and recurring costs;
3. diplomatic relations and a first diplomatic order;
4. causes of ruler death, abdication and overthrow;
5. succession rules derived from laws and institutions rather than a fixed list;
6. only then, a first minimal war system.

The prototype should prove that decisions mediated through unreliable people are interesting before a map or large historical database is built.
