using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using jobconnect.Data;
using jobconnect.Dtos;
using jobconnect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

//AuthController about login and register users by Radwa Khaled

namespace jobconnect.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
    
        private readonly IAuthRepository _authRepository;

        public AuthController(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
    
        }
  /*********************************************************** Login **********************************************************/
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var userInData = await _authRepository.Login(loginDto.Email, loginDto.Password);
            //if user not in data
            if (userInData == null)
                return Unauthorized();
            //else 
            var token = GenerateJwtToken(userInData);
            return Ok(new{token});
        }

  /*********************************************************** Register **********************************************************/

        [HttpPost("registeruser")]  // localhost:7163/api/Auth/registeruser
        public async Task<IActionResult> Register([FromBody] JobSeekerDto jobSeekerForRegisterDto)
        {
            if (jobSeekerForRegisterDto == null) return BadRequest();

            jobSeekerForRegisterDto.Email = jobSeekerForRegisterDto.Email.ToLower();
            if (await _authRepository.UserExist(jobSeekerForRegisterDto.Email))
            {
                return BadRequest("Email already exists");
            }

            var userToCreate = new JobSeeker()
            {
                first_name = jobSeekerForRegisterDto.first_name,
                last_name = jobSeekerForRegisterDto.last_name,
                latest_certificate = jobSeekerForRegisterDto.latest_certificate,
                phone = jobSeekerForRegisterDto.phone,
                gender = jobSeekerForRegisterDto.gender
            };

            var user = new User()
            {
                Username = jobSeekerForRegisterDto.Username,
                Email = jobSeekerForRegisterDto.Email,
                UserType = "Job Seeker"
            };
            user.JobSeeker = userToCreate;

            await _authRepository.Register(user, jobSeekerForRegisterDto.Password);

            return Ok();

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDto userDto)
        {
            //if user in data already
            if (await _authRepository.UserExist(userDto.Email))
                return BadRequest("User already Exists");
            //else
            var CreateUser = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                UserType = userDto.UserType 
            };

            var createdUser = await _authRepository.Register(CreateUser, userDto.Password);

            if (createdUser != null)
                return StatusCode(200); 

            return StatusCode(500); 
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserType)
                  
            };

            // size of key
            var key = new byte[64];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }

            var securityKey = new SymmetricSecurityKey(key);

            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = DateTime.Now,
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }


    }
}
