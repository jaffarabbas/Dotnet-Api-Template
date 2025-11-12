using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace ApiTemplate.Middleware
{
    /// <summary>
    /// Middleware that adds comprehensive security headers to HTTP responses
    /// Protects against XSS, clickjacking, MIME-type sniffing, and other web vulnerabilities
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecurityHeadersConfiguration _config;

        public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _config = configuration.GetSection("SecurityHeaders").Get<SecurityHeadersConfiguration>()
                      ?? new SecurityHeadersConfiguration();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers before processing the request
            AddSecurityHeaders(context);

            await _next(context);

            // Ensure headers are set even after response
            EnsureSecurityHeaders(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Strict-Transport-Security (HSTS)
            // Forces browsers to use HTTPS for all future requests
            if (_config.EnableHSTS && context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] =
                    $"max-age={_config.HSTSMaxAge}; includeSubDomains{(_config.HSTSPreload ? "; preload" : "")}";
            }

            // X-Frame-Options
            // Prevents clickjacking attacks by controlling iframe embedding
            if (_config.EnableXFrameOptions)
            {
                headers["X-Frame-Options"] = _config.XFrameOptions;
            }

            // X-Content-Type-Options
            // Prevents MIME-type sniffing attacks
            if (_config.EnableXContentTypeOptions)
            {
                headers["X-Content-Type-Options"] = "nosniff";
            }

            // X-XSS-Protection
            // Legacy XSS protection (for older browsers)
            if (_config.EnableXXSSProtection)
            {
                headers["X-XSS-Protection"] = "1; mode=block";
            }

            // Referrer-Policy
            // Controls how much referrer information should be included with requests
            if (_config.EnableReferrerPolicy)
            {
                headers["Referrer-Policy"] = _config.ReferrerPolicy;
            }

            // Content-Security-Policy (CSP)
            // Prevents XSS, data injection, and other code execution attacks
            if (_config.EnableCSP)
            {
                headers["Content-Security-Policy"] = BuildCSP();
            }

            // Permissions-Policy (formerly Feature-Policy)
            // Controls which browser features and APIs can be used
            if (_config.EnablePermissionsPolicy)
            {
                headers["Permissions-Policy"] = BuildPermissionsPolicy();
            }

            // X-Permitted-Cross-Domain-Policies
            // Restricts Adobe Flash and PDF cross-domain requests
            if (_config.EnableXPermittedCrossDomainPolicies)
            {
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
            }

            // Cross-Origin-Embedder-Policy (COEP)
            if (_config.EnableCOEP)
            {
                headers["Cross-Origin-Embedder-Policy"] = _config.COEP;
            }

            // Cross-Origin-Opener-Policy (COOP)
            if (_config.EnableCOOP)
            {
                headers["Cross-Origin-Opener-Policy"] = _config.COOP;
            }

            // Cross-Origin-Resource-Policy (CORP)
            if (_config.EnableCORP)
            {
                headers["Cross-Origin-Resource-Policy"] = _config.CORP;
            }

            // Remove potentially dangerous headers that expose server info
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            headers.Remove("X-AspNet-Version");
            headers.Remove("X-AspNetMvc-Version");
        }

        private void EnsureSecurityHeaders(HttpContext context)
        {
            // Double-check critical headers are present
            var headers = context.Response.Headers;

            if (_config.EnableXContentTypeOptions && !headers.ContainsKey("X-Content-Type-Options"))
            {
                headers["X-Content-Type-Options"] = "nosniff";
            }

            if (_config.EnableXFrameOptions && !headers.ContainsKey("X-Frame-Options"))
            {
                headers["X-Frame-Options"] = _config.XFrameOptions;
            }
        }

        private string BuildCSP()
        {
            var cspParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(_config.CSP_DefaultSrc))
                cspParts.Add($"default-src {_config.CSP_DefaultSrc}");

            if (!string.IsNullOrWhiteSpace(_config.CSP_ScriptSrc))
                cspParts.Add($"script-src {_config.CSP_ScriptSrc}");

            if (!string.IsNullOrWhiteSpace(_config.CSP_StyleSrc))
                cspParts.Add($"style-src {_config.CSP_StyleSrc}");

            if (!string.IsNullOrWhiteSpace(_config.CSP_ImgSrc))
                cspParts.Add($"img-src {_config.CSP_ImgSrc}");

            if (!string.IsNullOrWhiteSpace(_config.CSP_ConnectSrc))
                cspParts.Add($"connect-src {_config.CSP_ConnectSrc}");

            if (!string.IsNullOrWhiteSpace(_config.CSP_FontSrc))
                cspParts.Add($"font-src {_config.CSP_FontSrc}");

            if (!string.IsNullOrWhiteSpace(_config.CSP_ObjectSrc))
                cspParts.Add($"object-src {_config.CSP_ObjectSrc}");

            if (!string.IsNullOrWhiteSpace(_config.CSP_MediaSrc))
                cspParts.Add($"media-src {_config.CSP_MediaSrc}");

            if (!string.IsNullOrWhiteSpace(_config.CSP_FrameSrc))
                cspParts.Add($"frame-src {_config.CSP_FrameSrc}");

            if (!string.IsNullOrWhiteSpace(_config.CSP_BaseUri))
                cspParts.Add($"base-uri {_config.CSP_BaseUri}");

            if (!string.IsNullOrWhiteSpace(_config.CSP_FormAction))
                cspParts.Add($"form-action {_config.CSP_FormAction}");

            if (_config.CSP_UpgradeInsecureRequests)
                cspParts.Add("upgrade-insecure-requests");

            if (_config.CSP_BlockAllMixedContent)
                cspParts.Add("block-all-mixed-content");

            return string.Join("; ", cspParts);
        }

        private string BuildPermissionsPolicy()
        {
            var policies = new List<string>
            {
                "accelerometer=()",
                "camera=()",
                "geolocation=()",
                "gyroscope=()",
                "magnetometer=()",
                "microphone=()",
                "payment=()",
                "usb=()"
            };

            return string.Join(", ", policies);
        }
    }

    /// <summary>
    /// Configuration class for security headers
    /// Allows fine-grained control over which headers to enable and their values
    /// </summary>
    public class SecurityHeadersConfiguration
    {
        // HSTS Configuration
        public bool EnableHSTS { get; set; } = true;
        public int HSTSMaxAge { get; set; } = 31536000; // 1 year in seconds
        public bool HSTSPreload { get; set; } = false;

        // X-Frame-Options
        public bool EnableXFrameOptions { get; set; } = true;
        public string XFrameOptions { get; set; } = "DENY"; // DENY, SAMEORIGIN, or ALLOW-FROM uri

        // X-Content-Type-Options
        public bool EnableXContentTypeOptions { get; set; } = true;

        // X-XSS-Protection (legacy)
        public bool EnableXXSSProtection { get; set; } = true;

        // Referrer-Policy
        public bool EnableReferrerPolicy { get; set; } = true;
        public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
        // Options: no-referrer, no-referrer-when-downgrade, origin, origin-when-cross-origin,
        // same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url

        // Content Security Policy (CSP)
        public bool EnableCSP { get; set; } = true;
        public string CSP_DefaultSrc { get; set; } = "'self'";
        public string CSP_ScriptSrc { get; set; } = "'self'";
        public string CSP_StyleSrc { get; set; } = "'self' 'unsafe-inline'";
        public string CSP_ImgSrc { get; set; } = "'self' data: https:";
        public string CSP_ConnectSrc { get; set; } = "'self'";
        public string CSP_FontSrc { get; set; } = "'self'";
        public string CSP_ObjectSrc { get; set; } = "'none'";
        public string CSP_MediaSrc { get; set; } = "'self'";
        public string CSP_FrameSrc { get; set; } = "'none'";
        public string CSP_BaseUri { get; set; } = "'self'";
        public string CSP_FormAction { get; set; } = "'self'";
        public bool CSP_UpgradeInsecureRequests { get; set; } = true;
        public bool CSP_BlockAllMixedContent { get; set; } = false;

        // Permissions Policy
        public bool EnablePermissionsPolicy { get; set; } = true;

        // X-Permitted-Cross-Domain-Policies
        public bool EnableXPermittedCrossDomainPolicies { get; set; } = true;

        // Cross-Origin Policies
        public bool EnableCOEP { get; set; } = false; // Can break functionality if not carefully configured
        public string COEP { get; set; } = "require-corp";

        public bool EnableCOOP { get; set; } = true;
        public string COOP { get; set; } = "same-origin";

        public bool EnableCORP { get; set; } = true;
        public string CORP { get; set; } = "same-origin";
    }
}
