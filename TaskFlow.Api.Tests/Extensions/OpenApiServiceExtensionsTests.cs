using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Extensions;

namespace TaskFlow.Api.Tests.Extensions;

public class OpenApiServiceExtensionsTests
{
    private static OpenApiDocumentTransformerContext CreateContext(string documentName)
    {
        return new OpenApiDocumentTransformerContext
        {
            DocumentName = documentName,
            DescriptionGroups = new List<ApiDescriptionGroup>(),
            ApplicationServices = new ServiceCollection().BuildServiceProvider()
        };
    }

    [Fact]
    public async Task ApiVersionDocumentTransformer_TransformAsync_SetsDocumentInfo_WithoutVersionProvider()
    {
        // Arrange
        var transformer = new ApiVersionDocumentTransformer();
        var document = new Microsoft.OpenApi.OpenApiDocument();
        var context = CreateContext("v1");

        // Act
        await transformer.TransformAsync(document, context, CancellationToken.None);

        // Assert
        document.Info.Should().NotBeNull();
        document.Info.Title.Should().Be("TaskFlow API");
        document.Info.Version.Should().Be("v1");
        document.Info.Description.Should().Contain("RESTful");
    }

    [Fact]
    public async Task ApiVersionDocumentTransformer_TransformAsync_ReturnsCompletedTask()
    {
        // Arrange
        var transformer = new ApiVersionDocumentTransformer();
        var document = new Microsoft.OpenApi.OpenApiDocument();
        var context = CreateContext("v2");

        // Act
        var task = transformer.TransformAsync(document, context, CancellationToken.None);
        await task;

        // Assert
        task.IsCompleted.Should().BeTrue();
        document.Info.Version.Should().Be("v2");
    }
}
