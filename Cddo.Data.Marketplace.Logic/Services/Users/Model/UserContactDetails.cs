namespace Cddo.Data.Marketplace.Logic.Services.Users.Model;

public class UserContactDetails : IUserContactDetails
{
    public required string UserName { get; init; }

    public required string EmailAddress { get; init; }
}