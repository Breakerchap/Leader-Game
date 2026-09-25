using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class PlotSystem
{
    private const double PlotStartThreat = 70;
    private const double PlotGrowthThreat = 55;
    private const double JoinAffinity = 65;
    private const double LeaveAffinity = 55;

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();

        foreach (var country in state.Countries)
        {
            if (!country.Ruler.IsAlive)
                continue;

            foreach (var disabledPlot in state.Plots.Where(plot =>
                         !plot.IsResolved &&
                         ReferenceEquals(plot.Country, country) &&
                         !plot.Instigator.IsPoliticallyActive))
            {
                disabledPlot.IsResolved = true;
                disabledPlot.Succeeded = false;
            }

            var candidates = country.PoliticalFigures
                .Where(character =>
                    character.IsPoliticallyActive &&
                    !ReferenceEquals(character, country.Ruler))
                .ToList();

            foreach (var character in candidates)
            {
                var plot = state.Plots.FirstOrDefault(existing =>
                    !existing.IsResolved &&
                    ReferenceEquals(existing.Country, country) &&
                    ReferenceEquals(existing.Instigator, character));

                var threat = PoliticalCalculations.GetThreatScore(
                    state,
                    country,
                    character);

                if (plot is null && threat >= PlotStartThreat)
                {
                    plot = new PoliticalPlot
                    {
                        Country = country,
                        Instigator = character,
                        Progress = Math.Clamp((threat - 65) * 1.5, 5, 30)
                    };

                    state.Plots.Add(plot);
                }

                if (plot is null)
                    continue;

                UpdateSupporters(state, plot);
                UpdatePlotProgress(country, character, plot, threat);
                ReportDiscovery(state, plot, reports);

                if (plot.Progress >= 100)
                    reports.Add(ResolveCoup(state, plot));
            }
        }

        return reports;
    }

    private static void UpdateSupporters(GameState state, PoliticalPlot plot)
    {
        foreach (var character in plot.Country.PoliticalFigures)
        {
            if (!character.IsPoliticallyActive ||
                ReferenceEquals(character, plot.Country.Ruler) ||
                ReferenceEquals(character, plot.Instigator))
            {
                plot.SupporterIds.Remove(character.Id);
                continue;
            }

            var affinity = PoliticalCalculations.GetConspiracyAffinity(
                state,
                plot.Country,
                character,
                plot.Instigator);

            if (affinity >= JoinAffinity)
                plot.SupporterIds.Add(character.Id);
            else if (affinity < LeaveAffinity)
                plot.SupporterIds.Remove(character.Id);
        }
    }

    private static void UpdatePlotProgress(
        Country country,
        Character instigator,
        PoliticalPlot plot,
        double threat)
    {
        if (threat < PlotGrowthThreat)
        {
            plot.Progress -= Math.Clamp(
                (PlotGrowthThreat - threat) / 6.0,
                1,
                5);
            return;
        }

        var instability =
            (100 - country.Government.Stability) * 0.004 +
            country.PublicUnrest * 0.003;

        var accessMultiplier = instigator.Position switch
        {
            Position.Marshal => 1.25,
            Position.Chancellor => 1.15,
            Position.Treasurer => 1.05,
            _ => 0.85
        };

        var supporterInfluence = GetSupporterInfluence(country, plot);
        var coalitionMultiplier =
            1 + Math.Min(0.60, supporterInfluence / 200.0);

        var progress =
            ((threat - PlotGrowthThreat) / 5.0) *
            (0.80 + instability) *
            accessMultiplier *
            coalitionMultiplier;

        plot.Progress += Math.Clamp(progress, 0.25, 12);
    }

    private static void ReportDiscovery(
        GameState state,
        PoliticalPlot plot,
        List<SimulationReport> reports)
    {
        if (!ReferenceEquals(plot.Country, state.Player.Country))
            return;

        if (plot.Progress >= 70 && plot.DiscoveryStage < 2)
        {
            plot.DiscoveryStage = 2;

            var knownSupporters = GetSupporterNames(plot);
            var supportText = knownSupporters.Count == 0
                ? "No reliable evidence identifies other conspirators."
                : $"Evidence also implicates {string.Join(", ", knownSupporters)}.";

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"Evidence of a plot led by {plot.Instigator.FullName}",
                $"Your agents now have credible evidence that {plot.Instigator.FullName} " +
                $"is organising against the rule of {plot.Country.Ruler.FullName}. " +
                supportText));
        }
        else if (plot.Progress >= 35 && plot.DiscoveryStage < 1)
        {
            plot.DiscoveryStage = 1;
            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"Rumours surround {plot.Instigator.FullName}",
                $"Court gossip increasingly links {plot.Instigator.FullName} with " +
                "private meetings and opposition to the ruler. Nothing is proven yet."));
        }
    }

    private static SimulationReport ResolveCoup(GameState state, PoliticalPlot plot)
    {
        var country = plot.Country;
        var instigator = plot.Instigator;
        var oldRuler = country.Ruler;

        var attack = CalculateCoupStrength(state, country, plot);
        var defence = CalculateRegimeStrength(state, country);

        plot.IsResolved = true;

        if (attack > defence)
        {
            plot.Succeeded = true;
            instigator.Position = null;
            country.Ruler = instigator;

            if (ReferenceEquals(country, state.Player.Country))
            {
                state.Player.HasLost = true;
                state.Player.LossReason =
                    $"{instigator.FullName} overthrew {oldRuler.FullName}. " +
                    "Usurpation ends the player's political continuity even if the " +
                    "new ruler belongs to the same dynasty, party or faction.";
            }

            var supporters = GetSupporterNames(plot);
            var coalitionText = supporters.Count == 0
                ? string.Empty
                : $" The coup was backed by {string.Join(", ", supporters)}.";

            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"{instigator.FullName} seizes power",
                $"A coup against {oldRuler.FullName} succeeds. Political strength " +
                $"{attack:F0} overcame regime strength {defence:F0}. " +
                $"{instigator.FullName} now rules {country.Name}.{coalitionText}");
        }

        plot.Succeeded = false;

        var oldInfluence = instigator.Influence;
        instigator.Position = null;
        instigator.Influence = Math.Max(0, instigator.Influence - 35);

        var toRuler = state.Relationships.GetOrCreate(instigator, oldRuler);
        toRuler.Opinion = Math.Max(-100, toRuler.Opinion - 30);
        toRuler.Trust = 0;
        toRuler.Fear = Math.Min(100, toRuler.Fear + 30);

        var rulerToInstigator = state.Relationships.GetOrCreate(oldRuler, instigator);
        rulerToInstigator.Opinion = Math.Max(-100, rulerToInstigator.Opinion - 60);
        rulerToInstigator.Trust = 0;

        foreach (var supporter in GetSupporters(country, plot))
        {
            supporter.Influence = Math.Max(0, supporter.Influence - 10);

            var supporterToRuler =
                state.Relationships.GetOrCreate(supporter, oldRuler);
            supporterToRuler.ChangeOpinion(-15);
            supporterToRuler.Trust = 0;
            supporterToRuler.ChangeFear(15);
        }

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{instigator.FullName}'s coup collapses",
            $"{instigator.FullName} failed to overthrow {oldRuler.FullName}: " +
            $"political strength {attack:F0} against regime strength {defence:F0}. " +
            $"They lose their office and their influence falls from {oldInfluence} " +
            $"to {instigator.Influence}.");
    }

    private static double CalculateCoupStrength(
        GameState state,
        Country country,
        PoliticalPlot plot)
    {
        var instigator = plot.Instigator;
        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            country,
            instigator,
            country.Ruler);

        var officeBonus = instigator.Position switch
        {
            Position.Marshal => 8,
            Position.Chancellor => 4,
            Position.Treasurer => 2,
            _ => 0
        };

        var supporterBonus =
            Math.Min(25, GetSupporterInfluence(country, plot) * 0.20);

        return
            instigator.Influence * 0.35 +
            instigator.Ambition * 0.25 +
            (100 - willingness) * 0.20 +
            country.PublicUnrest * 0.10 +
            (100 - country.Government.Stability) * 0.10 +
            officeBonus +
            supporterBonus;
    }

    private static double CalculateRegimeStrength(
        GameState state,
        Country country)
    {
        var officeHolders = country.ActiveAdvisors.ToList();

        var averageWillingness = officeHolders.Count == 0
            ? 50
            : officeHolders.Average(character =>
                PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    character,
                    country.Ruler));

        return
            country.Ruler.Influence * 0.25 +
            country.Ruler.Legitimacy * 0.25 +
            country.Government.Stability * 0.35 +
            averageWillingness * 0.15;
    }

    private static double GetSupporterInfluence(
        Country country,
        PoliticalPlot plot)
    {
        return GetSupporters(country, plot).Sum(character => character.Influence);
    }

    private static List<Character> GetSupporters(
        Country country,
        PoliticalPlot plot)
    {
        return country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                plot.SupporterIds.Contains(character.Id))
            .ToList();
    }

    private static List<string> GetSupporterNames(PoliticalPlot plot)
    {
        return GetSupporters(plot.Country, plot)
            .Select(character => character.FullName)
            .ToList();
    }
}
