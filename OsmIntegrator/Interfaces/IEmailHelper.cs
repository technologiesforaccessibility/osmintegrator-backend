using System.Security.Claims;
using System.Threading.Tasks;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.Interfaces
{
  public interface IEmailHelper
  {
    Task SendChangeEmailMessageAsync(string newEmailAddress, ClaimsPrincipal user);
    Task SendConfirmRegistrationMessageAsync(ApplicationUser user);
    Task SendForgotPasswordMessageAsync(string userEmailAddress);
    Task SendRegisterMessageAsync(RegisterData model, ApplicationUser user);
    Task SendTileOccupiedMessageAsync(ApplicationUser user);
  }
}