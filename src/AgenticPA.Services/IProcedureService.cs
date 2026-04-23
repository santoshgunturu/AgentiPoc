using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public interface IProcedureService
{
    Task<IReadOnlyList<Procedure>> SearchAsync(string query);
    Task<bool> CheckAuthRequiredAsync(string cpt);
}
