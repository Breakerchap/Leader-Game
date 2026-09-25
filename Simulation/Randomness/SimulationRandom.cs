namespace LeaderGame.Simulation.Randomness;

/// <summary>
/// Small deterministic PRNG for simulation decisions. A saved game only needs
/// to preserve this state to continue the same random sequence.
/// </summary>
public sealed class SimulationRandom : IRandomSource
{
    private ulong _state;

    public SimulationRandom(ulong seed)
    {
        _state = seed == 0
            ? 0x9E3779B97F4A7C15UL
            : seed;
    }

    public ulong State
    {
        get => _state;
        set => _state = value == 0
            ? 0x9E3779B97F4A7C15UL
            : value;
    }

    public double NextDouble()
    {
        var x = _state;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        _state = x;

        var value = x * 2685821657736338717UL;
        return (value >> 11) * (1.0 / (1UL << 53));
    }
}
