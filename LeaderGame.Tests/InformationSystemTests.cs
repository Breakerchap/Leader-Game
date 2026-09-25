using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Orders;
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
            Politics.PoliticalKeys.Country(country.Id),
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
