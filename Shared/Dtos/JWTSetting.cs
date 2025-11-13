namespace ApiTemplate.Dtos
{
    public class JWTSetting
    {
        public string securitykey { get; set; }
        public string? ValidIssuer { get; set; }
        public string? ValidAudience { get; set; }
        public int AccessTokenExpirationHours { get; set; } = 10;
        public int RefreshTokenExpirationDays { get; set; } = 7;
        public int MaxActiveRefreshTokensPerUser { get; set; } = 5;
    }
}
