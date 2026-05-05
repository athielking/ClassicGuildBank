using ClassicGuildBankApi.Filters;
using ClassicGuildBankApi.Tools;
using ClassicGuildBankApi.ViewModels;
using ClassicGuildBankApi.ViewModels.Auth;
using ClassicGuildBankData.Models;
using ClassicGuildBankData.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ClassicGuildBankApi.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        #region Data Members

        private ILogger<AuthController> _logger;

        private SignInManager<ClassicGuildBankUser> _signInManager;

        private UserManager<ClassicGuildBankUser> _userManager;

        private IConfiguration _configuration;

        private SendGridService _sendGridService;

        private GuildBankRepository _guildBankRepository;

        #endregion

        #region Constructor

        public AuthController
        (
            SignInManager<ClassicGuildBankUser> signInManager,
            UserManager<ClassicGuildBankUser> userManager,
            GuildBankRepository repository,
            ILogger<AuthController> logger,
            IConfiguration configuration,
            SendGridService sendGridService
        )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
            _sendGridService = sendGridService;
            _guildBankRepository = repository;
        }

        #endregion

        #region Public Methods

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { errorMessage = GetModelStateErrors() });

                var user = await _userManager.FindByNameAsync(model.UserName);
                var emailResult = await _userManager.IsEmailConfirmedAsync(user);

                if (!emailResult)
                    return BadRequest(new { errorMessage = "Email Not Confirmed", errorAction = "Resend Confirmation" });

                var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    if (!String.IsNullOrEmpty(model.GuildToken))
                    {
                        var member = _guildBankRepository.AddGuildMember(model.GuildToken, user);
                        if (member == null)
                            return Ok(new { token = GenerateJwtToken(user), warningMessage = "Failed to add guild member. No Guild Found with provided token" });
                    }

                    return Ok(new { token = GenerateJwtToken(user) });
                }
                return BadRequest(JsonConvert.SerializeObject(result));                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown while logging in: {ex}");
                return BadRequest(new { errorMessage = ex.Message } );
            }

            return BadRequest(new { errorMessage = "Failed to login" } );
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown while logging out: {ex}");
            }

            return BadRequest(new { errorMessage = "Failed to logout" } );
        }

        [HttpPost("register")]
        [ValidateModel]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { errorMessage = GetModelStateErrors() } );

                var user = await _userManager.FindByNameAsync(model.Username);

                if (user != null)
                    return BadRequest(new { errorMessage = "Username is already in use" });

                var result = _userManager.CreateAsync(new ClassicGuildBankUser()
                {
                    UserName = model.Username,                      
                    Email = model.Email,
                }, model.Password).Result;

                if (!result.Succeeded)
                    return BadRequest(new { errorMessage = result.Errors.First().Description });

                var addedUser = _userManager.FindByNameAsync(model.Username).Result;

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(addedUser);
                var safeToken = HttpUtility.UrlEncode(token);

                _sendGridService.SendEmailConfirmationEmail(addedUser, safeToken);

                if(!String.IsNullOrEmpty(model.GuildToken))
                {
                    var member = _guildBankRepository.AddGuildMember(model.GuildToken, addedUser);
                    if (member == null)
                        return Ok(new { warningMessage = "Failed to add guild member. No Guild Found with provided token" });
                }

                return Ok();
            }
            catch
            {
                return BadRequest(new { errorMessage = "Failed to Register User" });
            }
        }

        [HttpGet("confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> Confirm([FromQuery]string username, [FromQuery]string code)
        {
            var user = await _userManager.FindByNameAsync(username);
            var vm = new ConfirmViewModel();
            vm.Success = true;
            vm.ClientUrl = _configuration.GetValue<string>("ClientUrl");

            if (user == null)
            {
                vm.Success = false;
                vm.Message = $"Unable to locate user {username} to confirm";

                return BadRequest(new { errorMessage = $"Unable to locate user {username} to confirm" });
            } 

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded)
            {
                vm.Success = false;
                vm.Message = $"Failed to confirm email";

                return BadRequest(new { errorMessage = "Failed to confirm email" });
            }

            return Accepted();
        }

        [AllowAnonymous]
        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody]ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errorMessage = GetModelStateErrors() });

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return BadRequest(new { errorMessage = "Unable to locate user" });

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);

            if (!result.Succeeded)
                return BadRequest(new { errorMessage = result.Errors.First().Description });

            var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
            if (signInResult.Succeeded)
                return Ok(GenerateJwtToken(user));

            return BadRequest(new { errorMessage = "Failed to sign in" });
        }

        [AllowAnonymous]
        [HttpPost("sendResetPasswordEmail")]
        public async Task<IActionResult> SendResetPasswordEmail([FromBody]SendResetPasswordEmailModel model)
        {

            if(!ModelState.IsValid)
                return BadRequest(new { errorMessage = GetModelStateErrors() });

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { errorMessage = "An account with that email address does not exist" });

            if (!user.UserName.Equals(model.Username, StringComparison.InvariantCultureIgnoreCase))
                return BadRequest(new { errorMessage = "An account with that username and email combination does not exist" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            _sendGridService.SendPasswordResetEmail(user, HttpUtility.UrlEncode(token));

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("sendConfirmationEmail")]
        public async Task<IActionResult> sendConfirmationEmail([FromBody]LoginViewModel model)
        {

            //if (!ModelState.IsValid)
            //    return BadRequest(GetModelStateErrors());

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
                return BadRequest(new { errorMessage = "Failed to locate user" });

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var safeToken = HttpUtility.UrlEncode(token);

            _sendGridService.SendEmailConfirmationEmail(user, safeToken);

            return Ok();
        }

        #endregion

        #region Private Methods

        private object GenerateJwtToken(ClassicGuildBankUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? String.Empty),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtExpireDays"]));

            var token = new JwtSecurityToken(
                _configuration["JwtIssuer"],
                _configuration["JwtIssuer"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                user = user.UserName
            };
            
        }

        private string GetModelStateErrors()
        {
            return String.Join(Environment.NewLine, ModelState.Values.SelectMany(value => value.Errors).Select(error => error.ErrorMessage));
        }

        #endregion
    }
}
