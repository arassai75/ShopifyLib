using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Base exception for GraphQL errors
    /// </summary>
    public class GraphQLException : Exception
    {
        public List<GraphQLError>? GraphQLErrors { get; }
        public string? ResponseContent { get; }

        public GraphQLException(string message, List<GraphQLError>? errors = null, string? responseContent = null, Exception? inner = null)
            : base(message, inner)
        {
            GraphQLErrors = errors;
            ResponseContent = responseContent;
        }
    }

    /// <summary>
    /// Exception for GraphQL HTTP errors
    /// </summary>
    public class GraphQLHttpException : GraphQLException
    {
        public HttpStatusCode StatusCode { get; }

        public GraphQLHttpException(string message, HttpStatusCode statusCode, string? responseContent = null, Exception? inner = null)
            : base(message, null, responseContent, inner)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Exception for GraphQL user errors (validation, etc.)
    /// </summary>
    public class GraphQLUserException : GraphQLException
    {
        public List<UserError>? UserErrors { get; }

        public GraphQLUserException(string message, List<UserError>? userErrors, string? responseContent = null)
            : base(message, null, responseContent)
        {
            UserErrors = userErrors;
        }
    }

    /// <summary>
    /// Represents a standard GraphQL error (from the 'errors' array)
    /// </summary>
    public class GraphQLError
    {
        public string Message { get; set; } = string.Empty;
        public List<GraphQLLocation>? Locations { get; set; }
        public List<string>? Path { get; set; }
        public Dictionary<string, object>? Extensions { get; set; }
    }

    public class GraphQLLocation
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }
} 