using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebAdvert.Models.Accounts;
using Amazon.AspNetCore.Identity.Cognito;
using WebAdvert.Web.Models.Accounts;

namespace WebAdvert.Controllers
{
    public class AccountsController : Controller
    {
        private readonly SignInManager<CognitoUser> _signInManager;
        //private readonly UserManager<CognitoUser> _userManager;
        private readonly CognitoUserManager<CognitoUser> _userManager;
        private readonly CognitoUserPool _pool;

        public AccountsController(SignInManager<CognitoUser> signInManager, UserManager<CognitoUser> userManager, CognitoUserPool pool)
        {
            _signInManager = signInManager;
            //_userManager = userManager;
            _userManager = userManager as CognitoUserManager<CognitoUser>;
            _pool = pool;
        }

        public async Task<IActionResult> Signup()
        {
            var model = new SignupModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Signup(SignupModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _pool.GetUser( model.Email );

                if( user.Status != null)
                {
                    ModelState.AddModelError( "UserExists", "User with this email already exists" );
                    return View(model);
                }

                user.Attributes.Add( CognitoAttribute.Name.ToString(), model.Email );

                var createdUser = await (_userManager as CognitoUserManager<CognitoUser>).CreateAsync(user, model.Password);

                if (createdUser.Succeeded)
                {
                    return RedirectToAction( "Confirm" );
                }
                else
                {
                    foreach (var error in createdUser.Errors)
                    {
                        ModelState.AddModelError( error.Code, error.Description );
                    }
                    return View( model );
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Confirm()
        {
            return View();
        }
        [HttpPost]
        [ActionName("Confirm")]
        public async Task<IActionResult> Confirm_Post(ConfirmModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync( model.Email );

                if (user == null)
                {
                    ModelState.AddModelError( "NotFound", "User Email address not found" );

                    return View(model );
                }

                //var result = await (_userManager as CognitoUserManager<CognitoUser>).ConfirmEmailAsync( user, model.Code );
                var result = await _userManager.ConfirmSignUpAsync( user, model.Code, true );

                if (result.Succeeded)
                {
                    return RedirectToAction( "Index", "Home" );
                }
                else
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError( item.Code, item.Description );
                    }
                    return View( model );
                }
            }
            return View(model);
        }


        [HttpGet]
        public IActionResult Login( LoginModel model )
        {
            return View( model );
        }

        [HttpPost]
        [ActionName( "Login" )]
        public async Task<IActionResult> LoginPost( LoginModel model )
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync( model.Email,
                    model.Password, model.RememberMe, false ).ConfigureAwait( false );
                if (result.Succeeded)
                    return RedirectToAction( "Index", "Home" );
                ModelState.AddModelError( "LoginError", "Email and password do not match" );
            }

            return View( "Login", model );
        }
    }
}
