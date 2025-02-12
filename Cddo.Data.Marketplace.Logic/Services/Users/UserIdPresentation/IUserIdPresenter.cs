namespace Cddo.Data.Marketplace.Logic.Services.Users.UserIdPresentation;

public interface IUserIdPresenter
{
    Task<string?> GetInitiatingUserIdToken();
}