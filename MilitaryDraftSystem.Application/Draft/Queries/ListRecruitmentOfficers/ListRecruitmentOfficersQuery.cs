using MilitaryDraftSystem.Application.Common.Contracts;

namespace MilitaryDraftSystem.Application.Draft.Queries.ListRecruitmentOfficers
{
    /// <summary>
    /// Lists every recruitment officer in the game, active or not, so callers
    /// can distinguish the current player's officer from other players' (and,
    /// today, from officers whose career has already ended).
    /// </summary>
    public sealed record ListRecruitmentOfficersQuery : IQuery<IReadOnlyList<RecruitmentOfficerSummaryDto>>;
}
