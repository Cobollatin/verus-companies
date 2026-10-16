using System.Reflection;
using NetArchTest.Rules;
using Verus.Application.Abstractions;
using Verus.Domain.Companies;

namespace Verus.ArchitectureTests;

/// <summary>Enforces the Clean Architecture dependency rule: dependencies only point inwards.</summary>
public sealed class LayerDependencyTests
{
    private const string Domain = "Verus.Domain";
    private const string Application = "Verus.Application";
    private const string Infrastructure = "Verus.Infrastructure";
    private const string Api = "Verus.Api";

    private static readonly Assembly DomainAssembly = typeof(Company).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ICompanyRepository).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Verus.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Domain_DependsOnNoOtherLayerOrFramework() =>
        AssertNoDependencies(
            DomainAssembly,
            Application,
            Infrastructure,
            Api,
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "FastEndpoints",
            "Mediator",
            "FluentValidation"
        );

    [Fact]
    public void Application_DoesNotDependOnInfrastructureApiOrWebFrameworks() =>
        AssertNoDependencies(
            ApplicationAssembly,
            Infrastructure,
            Api,
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "FastEndpoints"
        );

    [Fact]
    public void Infrastructure_DoesNotDependOnApi() =>
        AssertNoDependencies(InfrastructureAssembly, Api, "FastEndpoints");

    [Fact]
    public void Endpoints_DoNotTouchPersistenceOrDomainDirectly()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("Verus.Api.Endpoints")
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, $"{Application}.Abstractions", "Microsoft.EntityFrameworkCore", Domain)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void StorageTypes_AreNotPublic()
    {
        var result = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace($"{Infrastructure}.Persistence")
            .ShouldNot()
            .BePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Handlers_AreSealed()
    {
        var result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler", StringComparison.Ordinal)
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    private static void AssertNoDependencies(Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
