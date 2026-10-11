using Application.Dtos.Authenticator.Base;
using Application.Dtos.Default;
using Domain.Entitites;
using Infrastructure.Repositories.BaseRepository;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Application.Services.AuthenticatorService
{
    public class AuthenticatorService : IAuthenticatorService
    {
        private static readonly string[] Algorithms = { "SHA1", "SHA256", "SHA512" };

        private readonly IBaseRepository<Authenticator> _authenticatorRepository;
        private readonly IBaseRepository<Email> _emailRepository;

        public AuthenticatorService
        (
            IBaseRepository<Authenticator> authenticatorRepository,
            IBaseRepository<Email> emailRepository
        )
        {
            _authenticatorRepository = authenticatorRepository;
            _emailRepository = emailRepository;
        }

        public BaseResponse<Authenticator> RegisterAuthenticator(AuthenticatorRequest authenticatorRequest)
        {
            var response = new BaseResponse<Authenticator>();
            var error = Validate(authenticatorRequest);

            if (error is not null)
            {
                response.AddError(error);
                return response;
            }

            var authenticator = new Authenticator();

            authenticator.Create(
                authenticatorRequest.Issuer,
                authenticatorRequest.AccountName,
                authenticatorRequest.Secret,
                authenticatorRequest.Algorithm,
                authenticatorRequest.Digits,
                authenticatorRequest.Period,
                authenticatorRequest.EmailId
            );

            response.Data = _authenticatorRepository.AddAsync(authenticator).Result;
            response.Success = true;

            return response;
        }

        public BaseResponse<Authenticator> UpdateAuthenticator(AuthenticatorRequest authenticatorRequest, int id)
        {
            var response = new BaseResponse<Authenticator>();
            var authenticator = _authenticatorRepository.Get().FirstOrDefault(e => e.Id == id);

            if (authenticator is null)
            {
                response.AddError("Código não encontrado");
                return response;
            }

            var error = Validate(authenticatorRequest);

            if (error is not null)
            {
                response.AddError(error);
                return response;
            }

            authenticator.Update(
                authenticatorRequest.Issuer,
                authenticatorRequest.AccountName,
                authenticatorRequest.Secret,
                authenticatorRequest.Algorithm,
                authenticatorRequest.Digits,
                authenticatorRequest.Period,
                authenticatorRequest.EmailId
            );

            response.Success = _authenticatorRepository.Update(authenticator);
            response.Data = authenticator;

            return response;
        }

        public DefaultResponse DeleteAuthenticator(int id)
        {
            var authenticator = _authenticatorRepository.Get().FirstOrDefault(e => e.Id == id);

            if (authenticator is null)
            {
                var notFound = new DefaultResponse();
                notFound.AddError("Código não encontrado");
                return notFound;
            }

            var success = _authenticatorRepository.Remove(authenticator);
            var response = new DefaultResponse(success);

            return response;
        }

        public BaseResponse<List<Authenticator>> GetAuthenticators()
        {
            var authenticators = _authenticatorRepository.Get()
                .Include(e => e.Email)
                .ThenInclude(e => e!.Platform)
                .ThenInclude(e => e.Image)
                .ThenInclude(e => e.StoredFile)
                .Include(e => e.Email)
                .ThenInclude(e => e!.EmailAddress)
                .ThenInclude(e => e.Provider)
                .OrderBy(e => e.Issuer)
                .ThenBy(e => e.AccountName)
                .ToList();

            var response = new BaseResponse<List<Authenticator>>()
            {
                Data = authenticators,
                Success = true
            };

            return response;
        }

        public BaseResponse<Authenticator> GetAuthenticatorById(int id)
        {
            var response = new BaseResponse<Authenticator>();
            var authenticator = _authenticatorRepository.Get().FirstOrDefault(e => e.Id == id);

            if (authenticator is null)
            {
                response.AddError("Código não encontrado");
                return response;
            }

            response.Data = authenticator;
            response.Success = true;

            return response;
        }

        private string? Validate(AuthenticatorRequest? request)
        {
            if (request is null)
            {
                return "Envie um código por vez";
            }

            request.Secret = Regex.Replace(request.Secret ?? "", @"[\s=-]", "").ToUpperInvariant();
            request.Algorithm = (request.Algorithm ?? "SHA1").Trim().ToUpperInvariant();
            request.Issuer = (request.Issuer ?? "").Trim();
            request.AccountName = (request.AccountName ?? "").Trim();

            if (request.Secret.Length < 8 || !Regex.IsMatch(request.Secret, "^[A-Z2-7]+$"))
            {
                return "Chave secreta inválida: use base32 (letras A-Z e números 2-7)";
            }
            if (!Algorithms.Contains(request.Algorithm))
            {
                return "Algoritmo deve ser SHA1, SHA256 ou SHA512";
            }
            if (request.Digits is < 6 or > 8)
            {
                return "O código deve ter 6, 7 ou 8 dígitos";
            }
            if (request.Period is < 10 or > 300)
            {
                return "O período deve ser de 10 a 300 segundos";
            }
            if (request.Issuer.Length == 0 && request.AccountName.Length == 0)
            {
                return "Informe o serviço ou a conta";
            }
            if (request.Issuer.Length > 120 || request.AccountName.Length > 200)
            {
                return "Nome muito longo";
            }
            if (request.EmailId is not null && !_emailRepository.Get().Any(e => e.Id == request.EmailId))
            {
                return "Conta vinculada não encontrada";
            }

            return null;
        }
    }
}
