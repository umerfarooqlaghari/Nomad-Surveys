namespace Nomad.Api.Services.Interfaces;

public interface IPasswordGenerator
{
    string Generate(string email);
    bool IsGeneratedPassword(string email, string hashedPassword);
}
