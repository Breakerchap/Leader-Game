using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class InformationSystemTests
{
    [Fact]
    public void DemoScenario_StartsWithInheritedLastKnownInformation()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var nordmark = state.FindCountry("nordmark")!;

        var treasury = state.Knowledge.Get(
            InformationMetric.Treasury,
            country.Id);
        var army = state.Knowledge.Get(
            InformationMetric.ArmySize,
            country.Id);
        var foreignRelations = state.Knowledge.Get(
            InformationMetric.DiplomaticRelations,
            nordmark.Id,
            country.Id);

        Assert.NotNull(treasury);
        Assert.NotNull(army);
        Assert.NotNull(foreignRelations);
        Assert.True(treasury!.AgeInMonths(state.Date) >= 1);
        Assert.True(foreignRelations!.AgeInMonths(state.Date) >= 1);
        Assert.NotEmpty(state.AdvisorReports);
    }

    [Fact]
    public void RequestedReport_TakesAtLeastOneFutureTurnToArrive()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        state.InformationRandom = new SequenceRandom(1.0);

        var oldKnowledge = state.Knowledge.Get(
            InformationMetric.Treasury,
            country.Id)!;

        simulation.SubmitOrder(new RequestReportOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            Topic = InformationTopic.Economy,
            SubjectCountry = country
        });

        simulation.AdvanceMonth();

        Assert.Contains(
            state.InformationRequests,
            request =>
                request.Topic == InformationTopic.Economy &&
                request.Status == InformationRequestStatus.Pending);

        var afterRequest = state.Knowledge.Get(
            InformationMetric.Treasury,
            country.Id)!;
        Assert.Equal(oldKnowledge.ReceivedOn, afterRequest.ReceivedOn);

        simulation.AdvanceMonth();

        Assert.Contains(
            state.InformationRequests,
            request =>
                request.Topic == InformationTopic.Economy &&
                request.Status == InformationRequestStatus.Completed);

        var refreshed = state.Knowledge.Get(
            InformationMetric.Treasury,
            country.Id)!;

        Assert.True(refreshed.ReceivedOn.Year > oldKnowledge.ReceivedOn.Year ||
                    refreshed.ReceivedOn.Month > oldKnowledge.ReceivedOn.Month);
        Assert.True(refreshed.WasRequested);
    }

    [Fact]
    public void DeeplyUnwillingAdvisor_CanRefuseReportRequest()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        var relationship = state.Relationships.GetOrCreate(
            treasurer,
            country.Ruler);
        relationship.Opinion = -100;
        relationship.Trust = 0;
        relationship.Fear = 0;
        treasurer.Ambition = 100;
        treasurer.SetAllegiance(
            PoliticalKeys.Country(country.Id),
            0);

        state.InformationRandom = new SequenceRandom(0.0);

        var order = new RequestReportOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            Topic = InformationTopic.Economy,
            SubjectCountry = country
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Refused, order.Status);
        Assert.DoesNotContain(
            state.InformationRequests,
            request =>
                request.Status == InformationRequestStatus.Pending &&
                ReferenceEquals(request.Advisor, treasurer));
    }

    [Fact]
    public void StaleForeignReport_UsesHistoricalTruthRatherThanCurrentTruth()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;
        var nordmark = state.FindCountry("nordmark")!;
        var historicalArmy = nordmark.ArmySize;

        state.InformationRandom = new ConstantRandom(0.0);

        simulation.SubmitOrder(new RequestReportOrder
        {
            Issuer = country.Ruler,
            Recipient = marshal,
            IssuedOn = state.Date,
            Country = country,
            Topic = InformationTopic.Military,
            SubjectCountry = nordmark,
            RelatedCountry = country
        });

        // Request is accepted in January and the January truth is captured.
        simulation.AdvanceMonth();

        // February passes with the old army still in place.
        simulation.AdvanceMonth();

        // The army changes sharply just before the report is finally delivered
        // in March. A one-month information lag should therefore use February.
        nordmark.ArmySize = 20_000;
        simulation.AdvanceMonth();

        var report = state.AdvisorReports
            .Last(candidate =>
                candidate.WasRequested &&
                candidate.Topic == InformationTopic.Military &&
                ReferenceEquals(candidate.SubjectCountry, nordmark));

        var armyFact = report.Facts.Single(fact =>
            fact.Key.Metric == InformationMetric.ArmySize);

        Assert.Equal(new GameDate(1450, 2), report.DataAsOf);
        Assert.True(
            Math.Abs(armyFact.Estimate - historicalArmy) <
            Math.Abs(armyFact.Estimate - nordmark.ArmySize));
    }

    [Fact]
    public void InformationRandomness_DoesNotPerturbPhysicalRandomStream()
    {
        var first = DemoScenario.Create();
        var second = DemoScenario.Create();

        first.Random = new SimulationRandom(123456);
        second.Random = new SimulationRandom(123456);
        first.InformationRandom = new SequenceRandom(0.2, 0.4, 0.6, 0.8);
        second.InformationRandom = new SequenceRandom(1.0);

        var firstSimulation = new GameSimulation(first);
        var secondSimulation = new GameSimulation(second);

        var country = first.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        firstSimulation.SubmitOrder(new RequestReportOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = first.Date,
            Country = country,
            Topic = InformationTopic.Economy,
            SubjectCountry = country
        });

        firstSimulation.AdvanceMonth();
        secondSimulation.AdvanceMonth();

        Assert.Equal(
            first.Random.NextDouble(),
            second.Random.NextDouble());
    }

    private sealed class ConstantRandom : IRandomSource
    {
        private readonly double _value;

        public ConstantRandom(double value)
        {
            _value = value;
        }

        public double NextDouble() => _value;
    }

    private sealed class SequenceRandom : IRandomSource
    {
        private readonly Queue<double> _values;

        public SequenceRandom(params double[] values)
        {
            _values = new Queue<double>(values);
        }

        public double NextDouble()
        {
            return _values.Count > 0
                ? _values.Dequeue()
                : 1.0;
        }
    }
}
