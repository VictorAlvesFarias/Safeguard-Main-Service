using App.Application.Configuration;
using Application.Dtos.Default;
using Application.Dtos.User.Create;
using Application.Dtos.User.Login;
using Application.Dtos.User.Password;
using Application.Dtos.User.Put;
using Domain.Entitites;
using Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Application.Services.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly ApplicationContext _context;
        private readonly SignInManager<ApplicationUser> _singInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtOptions _jwtOptions;
        private readonly string _userId;

        public IdentityService(
            ApplicationContext context, 
            SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager, 
            IOptions<JwtOptions> jwtOptions)
        {
            _context = context;
            _singInManager = signInManager;
            _userManager = userManager;
            _jwtOptions = jwtOptions.Value;
            _userId = _context.GetUserId();
        }

        public async Task<DefaultResponse> PutUser(PutUserRequest userData)
        {
            var user = await _userManager.FindByIdAsync(_userId);

            var response = new DefaultResponse();

            if (user != null)
            {
                user.Name = userData.Name ==  null ? user.Name : userData.Name;
                user.Email = userData.Email == null ? user.Email : userData.Email;
                user.UserName = userData.Username == null ? user.UserName : userData.Username;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    response.Success = true;
                }

                else 
                {
                    response.AddErrors(result.Errors.ToList().ConvertAll<string>(item=> item.Description));

                    response.Success = false;
                }


                return response;
            }

            else
            {
                response.AddError("Faça login novamente e tente mais tarde.");

                response.Success = false;

                return response;
            }
        }
        public async Task<DefaultResponse> AddUser(CreateUserRequest userData)
        {
            var validateEmail = await ValidateEmailAsync(userData.Email);
            
            if (!validateEmail.Success)
            {
                return validateEmail;
            }

            var user = new ApplicationUser()
            {
                UserName = userData.Username,
                Email = userData.Email,
                CreateDate = DateTime.UtcNow,
                EmailConfirmed = false,
                Name = userData.Name,
            };
            var createdUser = await _userManager.CreateAsync(user, userData.Password);
            var defaultResponse = new DefaultResponse(createdUser.Succeeded);
            
            if (!defaultResponse.Success)
            {
                defaultResponse.AddErrors(createdUser.Errors.ToList().ConvertAll<string>(item => item.Description));
            }

            return defaultResponse;
        }
        public async Task<DefaultResponse> DeleteUser(LoginUserRequest loginData)
        {
            var user = await GetUserByEmailOrUsername(loginData.AccessKey);

            var login = await _singInManager.PasswordSignInAsync(user, loginData.Password, false, false);

            var response = new DefaultResponse(login.Succeeded);

            if (login.Succeeded&&user.Id == _userId)
            {
                _userManager.DeleteAsync(user);

                return response;
            }

            else
            {
                response.AddError("Senha ou Usuario incorretos.");

                return response;
            }
        }
        public async Task<DefaultResponse> ValidateEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            var response = new DefaultResponse(user == null);

            if (response.Success)
            {
                return response;
            }

            else
            {
                response.AddError("E-mail ja utilizado.");

                return response;
            }
        }
        public async Task<BaseResponse<LoginUserResponse>> LoginAsync(LoginUserRequest loginData)
        {
            var user = string.IsNullOrWhiteSpace(loginData.AccessKey) ? null : await GetUserByEmailOrUsername(loginData.AccessKey);

            if (user == null || string.IsNullOrEmpty(loginData.Password))
            {
                var invalidLogin = new BaseResponse<LoginUserResponse>(false);

                invalidLogin.AddError("Senha ou Usuario incorretos.");

                return invalidLogin;
            }

            var login = await _singInManager.PasswordSignInAsync(user, loginData.Password,false, false);

            var response = new BaseResponse<LoginUserResponse>(login.Succeeded);

            if (login.Succeeded)
            {
                response.Data = new()
                {
                    Email = user.Email,
                    ExpectedExpirationTokenDateTime = DateTime.UtcNow.AddSeconds(3600) ,  
                    Name = user.Name,   
                    Username = user.UserName,
                    ExpirationTokenTime = _jwtOptions.AccessTokenExpiration,
                    Token = await CreateToken(user)
                    
                };

                return response;
            }

            else
            {
                response.AddError("Senha ou Usuario incorretos.");

                return response;
            }
        }
        public async Task<IList<Claim>> GetClaimsAndRoles(ApplicationUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));

            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, DateTime.Now.ToString()));

            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToString()));

            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            };

            return claims;
        }
        public async Task<string> CreateToken(ApplicationUser user)
        {
            var claims = await GetClaimsAndRoles(user);

            var expiresDate = DateTime.Now.AddSeconds(_jwtOptions.AccessTokenExpiration);

            var jwt = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expiresDate,
                notBefore: DateTime.Now,
                signingCredentials: _jwtOptions.SigningCredentials
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            return token;
        }
        public async Task<ApplicationUser> GetUserByEmailOrUsername(string accessKey) 
        {
            if (IsValidEmail(accessKey))
            {
                var response = await _userManager.FindByEmailAsync(accessKey);
        
                return response;
            }
            else
            {
                var response = await _userManager.FindByNameAsync(accessKey);

                return response;
            }
        }
        public bool IsValidEmail(string email)
        {
            string pattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
            Regex regex = new Regex(pattern);

            return regex.IsMatch(email);
        }
        public async Task<DefaultResponse> ValidateUsernameAsync(string email)
        {
            var user = await _userManager.FindByNameAsync(email);

            var response = new DefaultResponse(user == null);

            if (response.Success)
            {
                return response;
            }

            else
            {
                response.AddError("Nome de usuário já utilizado.");

                return response;
            }
        }
        public async Task<DefaultResponse> ChangePasswordAsync(ChangePasswordRequest changePasswordData)
        {
            var user = await _userManager.FindByIdAsync(_userId);

            var changedPassword = await _userManager.ChangePasswordAsync(user, changePasswordData.Passowrd,changePasswordData.NewPassword);

            var response = new DefaultResponse(changedPassword.Succeeded);

            if(response.Success)
            {
                return response;
            }

            else
            {
                response.AddErrors(changedPassword.Errors.ToList().ConvertAll<string>(item => item.Description));
            }

            throw new NotImplementedException();
        }
    }
}