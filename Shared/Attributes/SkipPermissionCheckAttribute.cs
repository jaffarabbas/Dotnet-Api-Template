using System;

namespace ApiTemplate.Attributes
{
    /// <summary>
    /// Attribute to skip permission checking for specific endpoints.
    /// Use this on controllers or actions that should bypass global permission checking.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SkipPermissionCheckAttribute : Attribute
    {
        /* no code needed – it's just a marker */
    }
}
