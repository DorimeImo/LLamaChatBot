namespace LLamaWebAPI.Configurations
{
    public class JwtConfigs
    {
        public string CertificateThumbprint { get; set; }
        public int AccessTokenExpirationMinutes { get; set; }
        public int RefreshTokenExpirationDays { get; set; }
        public string Audience { get; set; }
        public string Issuer { get; set; }

    }
}
