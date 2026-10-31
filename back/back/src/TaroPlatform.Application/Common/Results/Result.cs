namespace TaroPlatform.Application.Common.Results;

/// <summary>
/// Result-патерн: повертає успіх або помилку замість винятків для очікуваних ситуацій.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("Успішний результат не може містити помилку.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("Неуспішний результат має містити помилку.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, string? error) : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("У невдалого результату немає значення.");
}
