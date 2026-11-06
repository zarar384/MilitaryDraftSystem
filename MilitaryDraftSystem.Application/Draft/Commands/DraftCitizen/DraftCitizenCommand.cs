using MilitaryDraftSystem.Application.Common.Contracts;

namespace MilitaryDraftSystem.Application.Draft.Commands.DraftCitizen
{
    /// <summary>
    /// Requests that a recruitment officer manually drafts a specific citizen.
    /// </summary>
    public sealed record DraftCitizenCommand(Guid CitizenId, Guid RecruitmentOfficerId) : ICommand;
}
