using Application.Dtos.Authenticator.Base;
using Application.Dtos.Default;
using Domain.Entitites;

namespace Application.Services.AuthenticatorService
{
    public interface IAuthenticatorService
    {
        BaseResponse<Authenticator> RegisterAuthenticator(AuthenticatorRequest authenticatorRequest);
        BaseResponse<Authenticator> UpdateAuthenticator(AuthenticatorRequest authenticatorRequest, int id);
        DefaultResponse DeleteAuthenticator(int id);
        BaseResponse<List<Authenticator>> GetAuthenticators();
        BaseResponse<Authenticator> GetAuthenticatorById(int id);
    }
}
