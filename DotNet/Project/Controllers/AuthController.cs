using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using DirectoryNet.Data;
using DirectoryNet.Dtos;
using DirectoryNet.Models;

namespace DirectoryNet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController :ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;

        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto UserForRegisterDto)
        {
            //validate request 
            UserForRegisterDto.Username = UserForRegisterDto.Username.ToLower();

            if (await _repo.UserExists(UserForRegisterDto.Username))
                return BadRequest("Username Already Exists");

            var userToCreate = new User
            {
                Username = UserForRegisterDto.Username
            };

            var createdUser = await _repo.Register(userToCreate, UserForRegisterDto.Password);

            return StatusCode(201); 
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

            if (userFromRepo == null)
                return Unauthorized();
            
            //Begin Building up the JWT token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Username)
            };
            
            //Secret Key From Application
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config
                .GetSection("AppSettings:Token").Value));
                
            //Takes our security key and will use an algorithm to hash that key
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            
            //Add claims, expirey date, and signing credentials
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            
            //Create the JWT token, which will be returned to client
            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            //Return token as an object
            return Ok(new {
                token = tokenHandler.WriteToken(token)
            });
        }
    }
}
