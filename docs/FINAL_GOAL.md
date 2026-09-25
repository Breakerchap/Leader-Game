# Leader Game — Final Goal

> This document describes the intended end-state of the game, not the feature set of the current prototype. See [../README.md](../README.md) for what is implemented now.

## Vision

**Leader Game** is intended to be a simulation-first grand-strategy game about holding, using and surviving political power.

The player does **not** directly control a country as though the state were a single machine. The player controls a continuing **political lineage or power network**: a dynasty, party, faction, revolutionary movement, military regime, ideological organisation or whatever form that continuity takes over time.

Countries, governments, regimes, institutions and individual rulers are separate things. They can outlive one another. A monarchy can become a republic, a party can survive the loss of an election, a dynasty can be exiled and return, a revolutionary movement can become the establishment, and a state can continue after the player's political project has collapsed.

The game should eventually cover a historical world over a very long period, roughly **1450–2100+**, beginning from recognisable real countries and conditions but allowing history to diverge freely from the moment play begins.

The central question is not:

> "What should the country do?"

It is:

> "What can I persuade, order, threaten, fund or organise other people into doing — and what happens when they interpret that differently from how I intended?"

## Core design pillars

### 1. The player is not the state

The state is a simulated entity with its own institutions, finances, population, armed forces, bureaucracy and interests.

The player controls a political continuity within that state. At different times that continuity may be the government, merely one powerful faction inside it, an opposition movement, an exile network or a claimant trying to return to power.

Losing office is therefore not automatically game over. Losing every realistic route back to meaningful political power is.

Likewise, a country being conquered, partitioned, merged, reformed or transformed does not necessarily end the player's political continuity.

### 2. Rule indirectly

The player should rarely alter the world directly.

Instead, the player:

- receives reports, petitions, briefings and advice;
- chooses advisers, ministers, commanders, governors and other subordinates;
- approves laws, budgets and priorities;
- gives broad orders;
- delegates implementation;
- negotiates with important people and institutions;
- personally handles major diplomacy, crises and political confrontations;
- reacts to what actually happened rather than what was intended.

An order is an intention, not a guaranteed state change.

A subordinate may obey enthusiastically, obey reluctantly, interpret the instruction differently, perform it badly, quietly obstruct it, leak it, exploit it for personal gain or refuse outright. Their response should depend on their ability, incentives, loyalties, relationships, fears, ambitions and circumstances.

### 3. People are political actors, not stat blocks

Important characters should have persistent relationships, ambitions, loyalties, reputations, positions, power bases and memories.

A minister can be competent but disloyal. A general can hate the ruler and still obey out of fear. A weak courtier can become dangerous because they are trusted by the army, related to a claimant, backed by a religious institution or central to a coalition.

Characters should care about one another as well as about the player. Politics therefore forms a network rather than a list of independent approval meters.

Over time, people should marry, age, fall ill, inherit, gain offices, lose influence, form alliances, defect, retire, die and be replaced.

### 4. Power must come from somewhere

Political power should emerge from concrete sources rather than from a single abstract score.

Possible power bases include:

- legal or constitutional authority;
- land and wealth;
- control of soldiers or police;
- bureaucracy and administrative networks;
- religion;
- parties and organised movements;
- patronage;
- business and finance;
- media and public communication;
- expertise and technical institutions;
- family and personal networks;
- popular legitimacy;
- foreign backing.

A ruler can be legally supreme and practically weak. A person with no formal office can be enormously powerful.

The simulation should make that difference matter.

### 5. Information is incomplete

The player should not be omniscient.

Information arrives through people and institutions, and therefore may be:

- delayed;
- incomplete;
- mistaken;
- deliberately misleading;
- biased by the source;
- too broad to expose the real cause;
- more or less reliable depending on state capacity.

The player should often act on estimates rather than exact hidden values.

Reports must still be useful. Uncertainty should create political judgement, not arbitrary guessing. Where possible, the game should explain what the player knows, who supplied that information and why it may be uncertain.

### 6. Consequences must be explainable

The simulation can be complicated without becoming opaque.

When something important happens, the player should be able to trace the broad causal chain:

```
conditions
    -> people and institutions react
    -> order is interpreted and implemented
    -> systems change
    -> political/economic/social consequences
    -> reports reach the player
```

The game should avoid unexplained "because RNG said so" outcomes.

Randomness can model uncertainty, accidents and human variation, but important results should emerge primarily from simulated conditions and should be reproducible from the same state and random seed.

## The core gameplay loop

The mature game should revolve around a repeating loop:

1. **Reports and briefing**  
   The player learns what advisers, officials, diplomats, commanders and other sources believe is happening.

2. **Agenda and judgement**  
   The player decides what matters now, what can wait, and which information to trust.

3. **Political preparation**  
   The player appoints people, bargains for support, spends political capital, shifts funding, builds coalitions or intimidates opposition.

4. **Orders and decisions**  
   The player sets goals and gives instructions rather than directly manipulating most outcomes.

5. **Interpretation and implementation**  
   Characters and institutions decide how the orders are carried out.

6. **Reaction**  
   Elites, institutions, foreign governments and the population respond according to their interests and circumstances.

7. **Consequences**  
   The economy, legitimacy, state capacity, relationships, wars, social conditions and political balance change.

8. **Time advances**  
   The next round of reports arrives, often with effects the player did not predict.

The most important gameplay should come from the gap between **intention** and **implementation**.

## Time and scale

The game should support centuries of play without requiring the player to process every month at the same level of detail.

Time should compress and expand with events:

- quiet periods may pass quickly;
- ordinary government may advance in months;
- wars, elections or negotiations may require shorter intervals;
- coups, revolutions, assassinations or constitutional crises may require day-scale decisions.

The player should be able to let routine government operate through institutions and subordinates, then become more involved when the political situation demands it.

## Government and institutions

Government forms should not be fixed character classes for countries.

A state may evolve between monarchies, republics, dictatorships, military regimes, party states, federations and other constitutional arrangements. The important part is not the label alone but the institutions and power relationships underneath it.

Institutions should determine questions such as:

- who can make laws;
- who controls taxation and spending;
- how rulers are selected and removed;
- how offices are filled;
- who commands armed forces;
- how courts and policing work;
- what regional authorities can do;
- how much freedom media, parties, unions, religious institutions and civil society possess;
- how effectively decisions can actually be implemented.

Institutional change should emerge through reform, bargaining, crisis, conquest, revolution and gradual political development rather than only through a menu that swaps one government type for another.

## Society, legitimacy and domestic politics

The population should not be a single approval number.

People should exist in meaningful social, economic, regional, religious, cultural and political groupings whose interests can overlap.

Domestic politics should include:

- elite factions and coalitions;
- parties and movements;
- regional interests;
- class and economic conflict;
- religious institutions;
- public opinion;
- nationalism and identity;
- unrest, protest and strikes;
- repression and policing;
- elections where relevant;
- coups and palace politics;
- revolutions and civil wars.

The goal is not to simulate every individual. It is to simulate enough structure that political outcomes have understandable causes.

## Economy and state capacity

Money should matter because governments need resources to make policy real.

The mature economy should model, at an appropriate strategic level:

- population and labour;
- production and trade;
- taxation;
- government expenditure;
- debt and finance;
- infrastructure;
- shortages and economic shocks;
- regional differences;
- administrative capacity;
- corruption and leakage;
- military and security costs.

The player should be able to announce an ambitious programme and then discover that the bureaucracy cannot implement it, the treasury cannot sustain it, local elites sabotage it or the population resists the cost.

Economic policy should therefore feed directly back into political power.

## Diplomacy

Diplomacy should be conducted through states **and** people.

Routine diplomacy can be delegated to ministers, diplomats and institutions. Personal ruler-to-ruler diplomacy should matter most for major negotiations, crises, alliances, conferences and symbolic events.

Foreign governments should have their own interests, internal political pressures and incomplete information. They should not exist only to react to the player.

Diplomatic relationships should be affected by:

- strategic interests;
- borders and territorial disputes;
- trade;
- alliances and treaties;
- ideology and regime relationships;
- ruler chemistry;
- historical grievances;
- military leverage;
- domestic political constraints;
- trust and credibility.

Agreements should create commitments that continue to matter after the person who signed them leaves office.

## War and coercion

War should be part of politics, not a separate tactical minigame.

The player should set political objectives, appoint commanders, allocate resources, choose strategic priorities and decide how much risk or cost is acceptable.

Commanders and military institutions should execute the war according to their competence, doctrine, loyalty, logistics and the resources they actually have.

The mature model should care about:

- manpower;
- equipment and technology;
- logistics;
- readiness;
- command quality;
- morale;
- geography;
- war aims;
- occupation;
- domestic support;
- economic cost;
- war exhaustion;
- foreign intervention;
- negotiation and peace terms.

The player should not normally move individual units around a map tile by tile.

Control of the armed forces should also be politically dangerous. A brilliant general who becomes indispensable may become a threat to the government that empowered them.

## Knowledge and technology

The game should avoid a conventional civilisation-style technology tree.

Instead, distinguish between:

- **knowledge** — whether an idea or technique is known;
- **capability** — whether the society can actually produce or perform it;
- **adoption** — how widely it is used;
- **institutions** — whether organisations exist to sustain it;
- **resources and infrastructure** — whether it can be deployed at scale.

Technology should spread unevenly through trade, education, espionage, migration, institutions and investment.

A government may understand a technology without being able to manufacture it, or possess the equipment without having the doctrine and institutions to use it well.

## Historical divergence

The starting world should be grounded in history, but the game should not attempt to force historical events after the simulation begins.

Historical pressures can exist because the conditions that caused them exist. The outcome should still be allowed to change.

The aim is for alternate history to be produced by the simulation rather than selected from scripted branches.

## Objectives, success and failure

The game should be a sandbox with meaningful ambitions rather than one universal victory screen.

A political lineage may pursue goals such as:

- preserving a dynasty;
- building a durable democracy;
- creating or maintaining an empire;
- unifying a nation;
- spreading an ideology;
- surviving a revolutionary period;
- modernising the state;
- achieving independence;
- constructing a long-lasting political settlement;
- building a personal or institutional legacy.

Ambitions may change as the political project changes.

Completing an ambition should be a major success but should not have to end the campaign.

### Defeat

The player loses when their political continuity has **no credible route back to meaningful power**.

Being voted out, deposed, exiled or temporarily reduced to opposition should not necessarily end the game. Those situations can become some of the most interesting parts of a campaign.

## Interface

The final presentation should support the simulation rather than turn it into a map-painting game.

The main interface should emphasise:

- briefings and reports;
- people and relationships;
- offices and institutions;
- decisions awaiting attention;
- budgets and state capacity;
- diplomatic situations;
- wars and crises;
- maps and charts where geography or trends matter.

A map is useful and important, but it should be an information surface rather than the object the player directly "plays".

The player should spend much of the game reading situations, judging people and making decisions.

## AI and language models

The simulation must remain authoritative and deterministic enough to test.

If a language model is ever used, it should be an optional presentation and interpretation layer: for example, turning a player's natural-language instruction into a structured order or expressing a simulated character's response in natural language.

A language model should **not** secretly decide the underlying outcome of a war, coup, budget, election or negotiation. Those results must come from the game state and simulation rules.

This keeps outcomes consistent, testable and explainable.

## What the game should not become

Leader Game should avoid drifting into:

- a conventional map-painting strategy game;
- direct omnipotent control of every government statistic;
- a tactical battle simulator;
- a fixed event-tree alternate-history game;
- a sequence of disconnected minigames;
- a system where every character is merely an approval bar;
- a game where "technology" means clicking the next box in a universal tree;
- a black-box AI story generator whose outcomes cannot be reproduced or explained.

## Definition of the final experience

A successful mature campaign should produce stories like this:

A ruler inherits a wealthy but politically fragile state. The treasury looks healthy, but the army answers more to an ambitious Marshal than to the crown. A tax reform is technically sensible, yet the Treasurer softens it to protect the merchants who support him. The ruler dismisses him, creating a coalition between commercial elites and a rival claimant. A neighbouring government notices the instability and applies pressure at the border. The ruler promotes a talented general to prepare for war, accidentally giving another potential rival an independent power base. Years later the ruler dies. The heir takes the throne, but several institutions recognise someone else. The player loses the capital, continues through loyal supporters in exile, bargains with a former enemy for help, and eventually returns under a constitution very different from the one the campaign began with.

None of that should require a pre-written event chain.

It should happen because **people, institutions, resources, information and power interacted consistently**.

That is the final goal.
