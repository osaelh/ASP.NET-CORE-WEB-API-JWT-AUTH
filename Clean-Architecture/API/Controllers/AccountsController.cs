 using Entities.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DataContext _context;
        private readonly IConfigurationSection _jwtSettings;

        public AccountsController(UserManager<IdentityUser> userManager, IConfiguration configuration, DataContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _jwtSettings = _configuration.GetSection("JwtSettings");
        }

        [HttpPost("Registration")]
        public async Task<IActionResult> RegisterUser([FromBody]  UserForRegistrationDto userForRegistration)
        {
            if (userForRegistration is null || ModelState.IsValid == false)
            {
                return BadRequest();
            }

            var user = new IdentityUser { UserName = userForRegistration.Email, Email = userForRegistration.Email };
            var result = await _userManager.CreateAsync(user, userForRegistration.Password);
            if (result.Succeeded == false)
            {
                var errors = result.Errors.Select(e => e.Description);

                return BadRequest(new RegistrationResponseDto { Errors = errors });
            }
            await _userManager.AddToRoleAsync(user, "Viewer");

            return StatusCode(201);

        }



        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserForAuthenticationDto userForAuthentication)
        {
            var user = await _userManager.FindByNameAsync(userForAuthentication.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, userForAuthentication.Password))
                return Unauthorized(new AuthResponseDto { ErrorMessage = "Invalid Authentication" });

            var signingCredentials = GetSigningCredentials();
            var claims = await GetClaims(user);
            var tokenOptions = GenerateTokenOptions(signingCredentials, claims);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            return Ok(new AuthResponseDto { IsAuthSuccessful = true, Token = token });


        }

        [HttpPost("Edit")]
        public async Task<IActionResult> EditUser([FromBody] UserForEditDto userForEditDto)
        {
            var user = await _userManager.FindByNameAsync(userForEditDto.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, userForEditDto.Password))
                return Unauthorized(new AuthResponseDto { ErrorMessage = "Invalid Authentication" });
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (userForEditDto.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, userForEditDto.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    
                    return BadRequest();
                }
            }
            if (userForEditDto.UserName != user.UserName)
            {
                user.UserName = userForEditDto.UserName;
            }
            if (userForEditDto.NewEmail != user.Email)
            {
                user.Email = userForEditDto.NewEmail;
            }

            await _userManager.UpdateAsync(user);

            await _context.SaveChangesAsync();
            return StatusCode(200);

        }

        [HttpPost("Delete")]
        public async Task<IActionResult> DeleteUser([FromBody] UserForAuthenticationDto userForRegistration)
        {
            var user = await _userManager.FindByNameAsync(userForRegistration.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, userForRegistration.Password))
                return Unauthorized(new AuthResponseDto { ErrorMessage = "Invalid Authentication" });
            var result = await  _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest();
            }

            return StatusCode(200);
        }

        

        private SigningCredentials GetSigningCredentials()
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings["securityKey"]);
            var secret = new SymmetricSecurityKey(key);

            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        private async Task<List<Claim>> GetClaims(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email)
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            return claims;
        }

        private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            var tokenOptions = new JwtSecurityToken(
                issuer: _jwtSettings["validIssuer"],
                audience: _jwtSettings["validAudience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_jwtSettings["expiryInMinutes"])),
                signingCredentials: signingCredentials);

            return tokenOptions;
        }
  

    }
}
