using FluentAssertions;
using NetArchTest.Rules;
using OrderProcessing.Domain.Orders;
using OrderProcessing.Persistence;

namespace Architecture.Tests;

public sealed class CleanArchitectureTests
{
    [Fact]
    public void Domain_should_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(typeof(Order).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "OrderProcessing.Application",
                "OrderProcessing.Infrastructure",
                "OrderProcessing.Persistence",
                "OrderProcessing.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_persistence()
    {
        var result = Types.InAssembly(typeof(OrderProcessing.Application.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("OrderProcessing.Infrastructure", "OrderProcessing.Persistence", "OrderProcessing.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_and_persistence_should_not_depend_on_api()
    {
        var infrastructure = Types.InAssembly(typeof(OrderProcessing.Infrastructure.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOn("OrderProcessing.Api")
            .GetResult();

        var persistence = Types.InAssembly(typeof(OrderProcessingDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOn("OrderProcessing.Api")
            .GetResult();

        infrastructure.IsSuccessful.Should().BeTrue();
        persistence.IsSuccessful.Should().BeTrue();
    }
}
