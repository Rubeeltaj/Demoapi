using DomainLayer.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ServiceLayer;
using System.IdentityModel.Tokens.Jwt;

using System.Security.Claims;

using System.Text;

namespace QAF.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UserController : ControllerBase
	{
		#region Property
		private readonly IUserService<User> _userService;
		private readonly ILogger<UserController> _logger;
		private readonly IConfiguration _configuration;
		#endregion

		#region Constructor
		public UserController(IUserService<User> userService,
							   ILogger<UserController> logger,
							   IConfiguration configuration
							   )
		{
			_userService = userService;
			_logger = logger;
			_configuration = configuration;
		}
		#endregion

		[HttpPost(nameof(UserRegistration))]
		public async Task<IActionResult> UserRegistration(User user)
		{
			try
			{
				var userData = await _userService.UserRegistration(user);
				if (userData is not null)
				{
					return Ok(new
					{
						status = 1,
						message = "Sucess!"
					});
				}
				else
				{
					return BadRequest(new
					{
						status = 0,
						message = "failure"
					});
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.InnerException?.Message ?? ex?.Message, nameof(Controllers));
				return BadRequest(new
				{
					status = 0,
					message = "Failed!",
					responseValue = ex?.InnerException?.Message ?? ex?.Message
				});
			}
		}

		[HttpPost(nameof(CheckLogin))]
		public async Task<IActionResult> CheckLogin(string? UserName, string? Password)
		{
			try
			{
				var userData = await _userService.CheckLogin(UserName, Password);
				if (userData is not null && userData.Status != true)
				{
					string token = GenerateToken(userData);
					return Ok(new
					{
						status = 1,
						message = "Sucess!",
						token = token
					});
				}
				else
				{
					return BadRequest(new
					{
						status = 0,
						message = "failure"
					});
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.InnerException?.Message ?? ex?.Message, nameof(Controllers));
				return BadRequest(new
				{
					status = 0,
					message = "Failed!",
					responseValue = ex?.InnerException?.Message ?? ex?.Message
				});
			}
		}

		[Authorize]
		[HttpGet(nameof(GetUserData))]
		public async Task<IActionResult> GetUserData()
		{
			try
			{
				var userData = await _userService.GetUserData();
				if (userData is not null )
				{					
					return Ok(new
					{
						status = 1,
						message = "Sucess!",
						responseValue = userData
					});
				}
				else
				{
					return BadRequest(new
					{
						status = 0,
						message = "failure"
					});
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.InnerException?.Message ?? ex?.Message, nameof(Controllers));
				return BadRequest(new
				{
					status = 0,
					message = "Failed!",
					responseValue = ex?.InnerException?.Message ?? ex?.Message
				});
			}
		}
		private string GenerateToken(User? user)
		{
			var claims = new[] {
						new Claim(JwtRegisteredClaimNames.Sub, _configuration["Jwt:Subject"]),
						new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
						new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
						new Claim("Id", user.Id.ToString()),
						new Claim("Name", user?.Name),
						new Claim("Email", user?.EmailId)
					};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
			var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
			var token = new JwtSecurityToken(
				_configuration["Jwt:Issuer"],
				_configuration["Jwt:Audience"],
				claims,
				expires: DateTime.UtcNow.AddMinutes(10),
				signingCredentials: signIn);

			var stringToken = new JwtSecurityTokenHandler().WriteToken(token);
			return stringToken;
		}
	}
}
