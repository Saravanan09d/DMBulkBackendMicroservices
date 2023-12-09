using Microsoft.AspNetCore.Identity;
using UserService.Models;

namespace UserService.Services
{
    public class AccessService
    {
        private readonly UserManager<UserTableModel> _userManager;
        private readonly SignInManager<UserRoleModel> _signInManager;

        public AccessService(UserManager<UserTableModel> userManager, SignInManager<UserRoleModel> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<bool> AuthenticateAsync(string Name, string password)
        {
            var user = await _userManager.FindByEmailAsync(Name);

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(Name, password, false, false);

                if (result.Succeeded)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
