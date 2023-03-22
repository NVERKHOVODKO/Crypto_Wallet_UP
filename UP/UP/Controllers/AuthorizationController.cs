using Microsoft.AspNetCore.Mvc;

namespace UP.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthorizationController : ControllerBase {
        
        [HttpGet, Route("Authorization")]
        public async Task<ActionResult> Login(String login, String password) {
            /*try
            {
                var db = new Database.Database();
                var user = db.Login(login, password);
                if (user != null) {
                    db.SaveAccountLoginHistory(user.Id);
                    return Ok(user);
                } else {
                    return NotFound("There is no such user. Try again");
                }
            }
            catch (Exception e)
            {
                return BadRequest("Error");
            }*/
            var ur = new Repositories.UserRepository();
            var user = ur.Login(login, password);
            if (user != null) {
                ur.SaveAccountLoginHistory(user.Id);
                return Ok(user);
            } else {
                return NotFound("There is no such user. Try again");
            }
        }

        [HttpPost, Route("Register")]
        public async Task<ActionResult> RegisterNewUser(String login, String password, String passwordRepeat, String email) {
            try
            {
                if (password.Length < 4)
                {
                    return BadRequest("Passwords must be more that 4 symbols");
                }
                if (password != passwordRepeat)
                {
                    return BadRequest("Passwords doesn't match");
                }
                var ur = new Repositories.UserRepository();
                if (ur.IsLoginUnique(login))
                {
                    return BadRequest("Username already used");
                }
                if (email.Length < 6)
                {
                    return BadRequest("Email isn't correct");
                }
                ur.WriteNewUserToDatabase(login, password, email);
                return Ok("User created successfully");
            }
            catch (Exception)
            {
                return BadRequest("User not created");
            }
        }
    }
}