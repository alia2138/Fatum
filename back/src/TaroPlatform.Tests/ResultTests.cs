using Xunit;
using TaroPlatform.Application.Common.Results;

namespace TaroPlatform.Tests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldBeSuccessful_WithoutError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ShouldContainError()
    {
        var result = Result.Failure("Помилка");

        Assert.True(result.IsFailure);
        Assert.Equal("Помилка", result.Error);
    }

    [Fact]
    public void GenericSuccess_ShouldReturnValue()
    {
        var result = Result.Success(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_AccessingValue_ShouldThrow()
    {
        var result = Result.Failure<int>("Помилка");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
