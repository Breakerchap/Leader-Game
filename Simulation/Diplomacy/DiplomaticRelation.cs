using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Diplomacy;

/// <summary>
/// Shared state between two countries. This is deliberately separate from
/// personal relationships between rulers and ministers.
/// </summary>
public sealed class DiplomaticRelation
{
    private int _relations;
    private int _trust = 50;
    private int _tension;

    public required string CountryAId { get; init; }

    public required string CountryBId { get; init; }

    /// <summary>
    /// General state-to-state relationship from -100 (hostile) to +100 (very close).
    /// </summary>
    public int Relations
    {
        get => _relations;
        set => _relations = Math.Clamp(value, -100, 100);
    }

    /// <summary>
    /// Confidence that agreements and assurances will be honoured.
    /// </summary>
    public int Trust
    {
        get => _trust;
        set => _trust = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Immediate diplomatic and strategic friction.
    /// </summary>
    public int Tension
    {
        get => _tension;
        set => _tension = Math.Clamp(value, 0, 100);
    }

    public bool HasTradeAgreement { get; set; }

    public GameDate? TradeAgreementStartedOn { get; set; }

    public bool HasNonAggressionPact { get; set; }

    public GameDate? NonAggressionPactStartedOn { get; set; }

    private int _borderDisputeSeverity;

    /// <summary>
    /// Persistent territorial or border grievance. Treaties can contain it,
    /// but they do not erase the underlying dispute.
    /// </summary>
    public int BorderDisputeSeverity
    {
        get => _borderDisputeSeverity;
        set => _borderDisputeSeverity = Math.Clamp(value, 0, 100);
    }

    public bool Involves(Country country) =>
        CountryAId == country.Id || CountryBId == country.Id;

    public string OtherCountryId(Country country)
    {
        if (CountryAId == country.Id)
            return CountryBId;

        if (CountryBId == country.Id)
            return CountryAId;

        throw new ArgumentException(
            $"{country.Name} is not part of this diplomatic relation.",
            nameof(country));
    }

    public void ChangeRelations(int delta) => Relations += delta;

    public void ChangeTrust(int delta) => Trust += delta;

    public void ChangeTension(int delta) => Tension += delta;
}
