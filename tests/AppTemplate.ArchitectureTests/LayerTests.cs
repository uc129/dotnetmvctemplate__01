using System.Reflection;
using AppTemplate.Application.Customers;
using AppTemplate.Domain.Customers;
using AppTemplate.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace AppTemplate.ArchitectureTests;

/// <summary>
/// The onion, checked. Dependencies point inwards only:
///   Web -> Application -> Domain, and Infrastructure -> Application -> Domain.
/// Web references Infrastructure only to register it (Program.cs); no controller may use it.
/// Plain reflection, no library: what is checked is visible right here.
/// </summary>
public sealed class LayerTests
{
    private static readonly Assembly Domain = typeof(Customer).Assembly;
    private static readonly Assembly Application = typeof(CustomerService).Assembly;
    private static readonly Assembly Infrastructure = typeof(DapperContext).Assembly;
    private static readonly Assembly Web = typeof(Program).Assembly;

    [Fact]
    public void Domain_depends_on_nothing_but_the_framework()
    {
        var references = ReferencedNames(Domain);

        Assert.All(references, name => Assert.True(name.StartsWith("System", StringComparison.Ordinal), $"Domain references {name}"));
    }

    [Fact]
    public void Application_depends_only_on_Domain_and_framework_abstractions()
    {
        var forbidden = ReferencedNames(Application)
            .Where(name => name.StartsWith("AppTemplate", StringComparison.Ordinal) && name != Domain.GetName().Name
                || name.StartsWith("Dapper", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.Data.SqlClient", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_Web()
    {
        Assert.DoesNotContain(Web.GetName().Name, ReferencedNames(Infrastructure));
    }

    [Fact]
    public void Controllers_do_not_use_Infrastructure_types()
    {
        var offenders = (
            from controller in Web.GetTypes()
            where typeof(ControllerBase).IsAssignableFrom(controller)
            from dependency in DependenciesOf(controller)
            where dependency.Assembly == Infrastructure
            select $"{controller.Name} uses {dependency.Name}").Distinct();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Repository_interfaces_live_in_Application_and_implementations_in_Infrastructure()
    {
        var interfaces = Application.GetTypes().Where(t => t.IsInterface && t.Name.EndsWith("Repository", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(interfaces);

        var misplaced =
            from type in new[] { Domain, Application, Web }.SelectMany(a => a.GetTypes())
            where type.IsClass && interfaces.Any(i => i.IsAssignableFrom(type))
            select type.FullName;

        Assert.Empty(misplaced);
    }

    private static IEnumerable<string> ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!);

    // Constructor parameters (including primary constructors), fields and properties.
    private static IEnumerable<Type> DependenciesOf(Type type)
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        return type.GetConstructors(all).SelectMany(c => c.GetParameters()).Select(p => p.ParameterType)
            .Concat(type.GetFields(all).Select(f => f.FieldType))
            .Concat(type.GetProperties(all).Select(p => p.PropertyType));
    }
}
