using System;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Attribute to mark integration tests that require external dependencies
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class IntegrationTestAttribute : Attribute
    {
    }
} 