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

## Current prototype

The console prototype currently models:

- a ruler, office-holders and potential advisers;
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
  Characters/   rulers, advisers and later other people
  Countries/    country and government state
  Orders/       player/AI intentions and their processing
  Player/       who the player currently represents
  Reports/      information returned to the player
  Scenarios/    starting world setups
  Systems/      world simulation systems
```

## Near-term direction

The next useful systems are deliberately small:

1. richer character relationships and loyalties;
2. government expenditure and recurring costs;
3. diplomatic relations and a first diplomatic order;
4. ruler death and succession;
5. only then, a first minimal war system.

The prototype should prove that decisions mediated through unreliable people are interesting before a map or large historical database is built.
