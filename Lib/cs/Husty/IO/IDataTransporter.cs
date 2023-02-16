﻿using System.Text;

namespace Husty.IO;

public interface IDataTransporter : IDisposable
{

    public string NewLine { get; }
    public Encoding Encoding { get; }

    public Task<bool> TryWriteAsync(
        byte[] data,
        TimeSpan timeout = default,
        CancellationToken ct = default
    );

    public Task<ResultExpression<byte[]>> TryReadAsync(
        int count,
        TimeSpan timeout = default,
        CancellationToken ct = default
    );

    public Task<bool> TryWriteLineAsync(
        string data,
        TimeSpan timeout = default,
        CancellationToken ct = default
    );

    public Task<ResultExpression<string>> TryReadLineAsync(
        TimeSpan timeout = default,
        CancellationToken ct = default
    );

    public Task<bool> TryWriteAsJsonAsync<T>(
        T data,
        TimeSpan timeout = default,
        CancellationToken ct = default
    );

    public Task<ResultExpression<T>> TryReadAsJsonAsync<T>(
        TimeSpan timeout = default,
        CancellationToken ct = default 
    );

}
