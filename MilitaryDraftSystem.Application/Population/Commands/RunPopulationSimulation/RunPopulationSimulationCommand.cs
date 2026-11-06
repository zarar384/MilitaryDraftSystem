using MilitaryDraftSystem.Application.Common.Contracts;

namespace MilitaryDraftSystem.Application.Population.Commands.RunPopulationSimulation
{
    /// <summary>
    /// Executes one tick of the world simulation: aging citizens, birthdays and deaths.
    /// </summary>
    public sealed record RunPopulationSimulationCommand : ICommand;
}
