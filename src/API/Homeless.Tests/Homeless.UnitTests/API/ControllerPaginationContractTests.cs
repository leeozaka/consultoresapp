using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Ardalis.Result;
using Homeless.API.Controllers;
using Homeless.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Homeless.UnitTests.API;

public class ControllerPaginationContractTests
{
    private static readonly HashSet<string> ExcludedControllerNames =
    [
        nameof(AuthController),
        nameof(ConnectController),
        nameof(WebhooksController),
    ];

    /// <summary>POST command-style list bodies (e.g. sync results) are not collection list resources.</summary>
    private static readonly HashSet<string> ResultCollectionAllowlistedMethods =
    [
        nameof(PlansController.BackfillStripeCatalog),
    ];

    [Fact]
    public void CollectionActions_ShouldReturnPaginationDtosAtTheHttpBoundary()
    {
        var assembly = typeof(PlansController).Assembly;
        var failures = new List<string>();

        foreach (var type in GetApiControllerTypes(assembly))
        {
            if (ExcludedControllerNames.Contains(type.Name))
            {
                continue;
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute<NonActionAttribute>() is not null)
                {
                    continue;
                }

                if (!HasHttpMethodAttribute(method))
                {
                    continue;
                }

                if (!TryUnwrapTaskReturnType(method.ReturnType, out var returnInner))
                {
                    continue;
                }

                if (typeof(IActionResult).IsAssignableFrom(returnInner))
                {
                    continue;
                }

                if (ResultCollectionAllowlistedMethods.Contains(method.Name))
                {
                    continue;
                }

                if (IsResultType(returnInner, out var valueType))
                {
                    if (IsProhibitedCollectionValueType(valueType!))
                    {
                        failures.Add(
                            $"{type.Name}.{method.Name} returns {returnInner} - " +
                            "use PaginatedResponse<T> or CursorPaginatedResponse<T> directly for list resources.");
                    }

                    continue;
                }

                if (IsPlainCollectionValueType(returnInner))
                {
                    failures.Add(
                        $"{type.Name}.{method.Name} returns {returnInner} - " +
                        "use PaginatedResponse<T> or CursorPaginatedResponse<T> for list resources.");
                }
            }
        }

        Assert.True(
            failures.Count == 0,
            "Controllers must expose list/collection success bodies as pagination DTOs at the HTTP boundary:\n" + string.Join("\n", failures));
    }

    private static IEnumerable<Type> GetApiControllerTypes(Assembly assembly) =>
        assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Controller", StringComparison.Ordinal));

    private static bool HasHttpMethodAttribute(MethodInfo method) =>
        method
            .GetCustomAttributes(true)
            .Select(a => a.GetType().Name)
            .Any(n => n
                is "HttpGetAttribute" or "HttpPostAttribute" or "HttpPutAttribute" or "HttpDeleteAttribute" or "HttpPatchAttribute" or "HttpHeadAttribute" or "RouteAttribute");

    private static bool TryUnwrapTaskReturnType(Type returnType, [NotNullWhen(true)] out Type? inner)
    {
        inner = null;
        if (!returnType.IsGenericType)
        {
            return false;
        }

        var def = returnType.GetGenericTypeDefinition();
        if (def == typeof(Task<>))
        {
            inner = returnType.GetGenericArguments()[0];
            return true;
        }

        if (def == typeof(ValueTask<>))
        {
            inner = returnType.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    private static bool IsResultType(Type type, [NotNullWhen(true)] out Type? valueType)
    {
        valueType = null;
        if (type is not { IsGenericType: true } || type.GetGenericTypeDefinition() != typeof(Result<>))
        {
            return false;
        }

        valueType = type.GetGenericArguments()[0];
        return true;
    }

    private static bool IsProhibitedCollectionValueType(Type valueType) =>
        IsPlainCollectionValueType(valueType)
        || (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(PaginatedResponse<>))
        || (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(CursorPaginatedResponse<>));

    private static bool IsPlainCollectionValueType(Type valueType) =>
        valueType != typeof(string)
        && (valueType.IsArray
            || valueType.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
}
