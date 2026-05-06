using SRG.Domain.Entities;

namespace SRG.Application.Auth;

public interface IJwtTokenService
{
    string CreateToken(User user);
    string CreateForemanToken(SubcontractorWorker worker);
}
