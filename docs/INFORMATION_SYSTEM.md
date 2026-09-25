# Information and Unreliable Reports

This system is one of the central design pillars of **Leader Game**.

The simulation knows the truth. The player does not.

A ruler should normally make decisions from reports, estimates, rumours, official records and the judgement of people who may be competent, incompetent, frightened, ambitious, hostile, overloaded, corrupt or deliberately deceptive.

The goal is not to add random noise to numbers. The goal is to make **information itself a political and administrative system**.

## Truth and player knowledge are separate

`GameState` contains the real world state: treasury balances, army sizes, readiness, diplomatic relations, unrest, stability, war progress and so on.

The player-facing information layer stores something different:

- the latest estimate the player has received;
- who supplied it;
- when the report was delivered;
- what date the underlying information describes;
- an estimated uncertainty range;
- the adviser's stated or apparent confidence;
- the history of reports that produced those estimates.

A value can therefore be accurate but stale, current but imprecise, precise-looking but wrong, selectively incomplete, deliberately misleading, or contradicted by another report.

The UI should not silently bypass this layer just because the exact simulation value is easy to access.

## Adviser responsibilities

### Treasurer

The Treasurer currently reports on population and broad economic scale, GDP, treasury, debt, recent tax revenue, trade income, expenses, fiscal balance and administrative efficiency. Domestic accounts are easier to observe than most foreign or political information, but weak administration, incompetence and political hostility still matter.

### Marshal

The Marshal reports on own army strength, readiness and war exhaustion, plus foreign military strength, foreign readiness and the reported campaign position. Foreign military intelligence has substantially more fog than domestic military records.

### Chancellor

The Chancellor reports on diplomatic relations, diplomatic trust and tension, domestic unrest, government stability and overall political backing. The Chancellor is also the current prototype's main source for broad political and foreign-government assessments.

These office responsibilities are a prototype. Later governments can split reporting among intelligence services, interior ministries, auditors, colonial offices, general staffs, diplomats, party organisations and regional authorities.

## Persistent last-known information

The player has a persistent `PlayerKnowledge` store. Each fact is keyed by metric, subject country and, where relevant, another related country.

Receiving a new report updates the current working estimate but does not delete the old report from history. The player can therefore know that their latest army estimate is several months old even if the true army has changed dramatically since then.

## Historical truth snapshots

The simulation records hidden monthly information snapshots. These are **not player-visible data**. They exist so an adviser who is reporting two-month-old information can actually be estimating the world as it was two months ago.

Without this, a supposedly stale report would just be today's truth with an old date attached. With snapshots, an enemy can reinforce this month while the ruler still acts on last month's smaller reported army.

The prototype currently retains a rolling recent history rather than an unlimited archive.

## Requested reports

The player can formally task an adviser with a report. A request is a structured order, not an instant query into hidden state.

An adviser may accept, refuse, take longer because the subject is difficult, take longer because they are incompetent or reluctant, suffer further delays while working, and eventually return information that is incomplete or wrong.

Foreign military and foreign-affairs inquiries generally take longer than domestic bookkeeping. A nominal one-month inquiry always requires at least one future turn; it cannot be ordered and completed inside the same monthly tick.

## Proactive reports

Advisers also decide when to speak without being asked. Their decision can depend on how stale the ruler's information is, competence, willingness, urgency, fiscal deterioration, war, diplomatic tension, incoming proposals, unrest, political demands and organised opposition.

An opposition-aligned adviser is less likely to volunteer useful information. Silence can therefore itself be politically meaningful, although it is not proof of deception.

## Accuracy

Accuracy is not a single random percentage. It currently depends on adviser competence, willingness, institutional quality, the inherent observability of the subject, information age and deterministic estimation error.

Administrative efficiency currently helps fiscal reporting, army readiness helps military reporting, and government stability acts as a rough proxy for political and diplomatic information capacity. These are interim proxies for more explicit institutions later.

Foreign military information has greater base fog than domestic records. Diplomatic intentions and political backing are also harder to measure than a ledger entry.

## Ordinary mistakes

Reports can be wrong through arithmetic mistakes, bad sampling, outdated local returns, rumours, incomplete scouting, clerical errors, missing records and mistaken interpretation. This uncertainty is generated from a dedicated deterministic information RNG.

## Deliberate distortion

Advisers can knowingly distort reports. The chance rises with low willingness, organised-opposition alignment and high ambition.

An opposition-aligned adviser tends to distort in politically useful directions: exaggerating debt, expenses, unrest, war exhaustion, diplomatic tension or enemy strength; or understating treasury strength, revenue, administrative effectiveness, political backing, diplomatic trust and favourable campaign progress.

The player is **not told when deliberate distortion has happened**.

## Selective omission

Misleading does not require inventing a false number. A reluctant or hostile adviser may omit individual findings entirely. Specifically requested reports reduce the chance of omission, but do not eliminate it.

## Confidence is not truth

Each estimate includes apparent adviser confidence. High confidence is not an objective probability of correctness.

A competent liar can produce a polished, confident false report. An honest but incompetent official may produce an uncertain report that happens to be right.

## Contradictory reports

When a new report differs materially from the previous estimate beyond their stated uncertainty, the contradiction is surfaced to the player. The game does not reveal which report is correct.

This turns conflicting information into a decision problem rather than an automatic correction.

## Independent deterministic randomness

Information uncertainty uses a separate deterministic RNG stream from the physical simulation. Adding or changing report-generation behaviour must not alter unrelated outcomes such as deaths, battles, illness, coup timing or foreign AI behaviour.

## What the player can know exactly

Some facts are directly known because they are decisions or public acts: the tax rate the government enacted, the formal budget allocation, whether a treaty exists, whether war has been declared, an ordered campaign stance, and public appointments or dismissals.

Other state should normally be mediated: treasury, debt, GDP, population, revenue, expenses, administrative efficiency, army strength, readiness, war exhaustion, campaign progress, enemy strength, unrest, stability, political backing and diplomatic relationship estimates.

Human psychological and political traits are shown qualitatively in the current UI rather than as exact meters.

## Report history and staleness

The intelligence screen preserves report history. Reports show the adviser, office, delivery date, data date, requested or unsolicited origin, estimates, uncertainty, apparent confidence and visible contradictions or missing-result caveats.

The latest estimate for each metric is used elsewhere in the UI. Old information remains available until replaced, and sufficiently old information is explicitly marked stale.

## Future extensions

The current implementation is a foundation. Important future extensions include:

- separate intelligence agencies and information budgets;
- spies, informants and counter-intelligence;
- source networks by country and region;
- courier and travel time tied to geography;
- communication disruption during war;
- intercepted correspondence;
- forged documents and planted intelligence;
- rumours with explicit source chains;
- newspapers and public-opinion reporting;
- regional governors filing false returns;
- corruption and embezzlement hidden from central accounts;
- commanders falsifying casualty reports;
- multiple independent sources estimating the same fact;
- player requests for corroboration or second opinions;
- secrecy classifications and compartmentalisation;
- advisers withholding information to protect themselves or allies;
- advisers timing reports to affect political decisions;
- institutions whose information capacity changes historically;
- uncertain borders, occupations, armies and map control.

The long-term objective is that **knowing what is happening becomes a strategic problem of its own**, not a presentation layer over otherwise omniscient grand strategy.