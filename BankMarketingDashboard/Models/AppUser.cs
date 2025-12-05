public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;  // sin encriptar para PoC
    public string Role { get; set; } = null!;      // "ejecutivoCuentas" o "gerencia"
}