namespace login_api.Model
{
    public class User
    {
        public string? Email { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int PasswordHash { get; set; }
        public int ConfirmPasswordHasn { get; set; }

    }
}
