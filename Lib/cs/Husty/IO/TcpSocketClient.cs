﻿using System.Net.Sockets;
using System.Text;

namespace Husty.IO;

public sealed class TcpSocketClient : ICommunicationProtocol
{

    // ------ fields ------ //

    private TcpClient _client1;
    private TcpClient _client2;
    private readonly Task _connectionTask;


    // ------ properties ------ //

    public string NewLine { init; get; } = "\n";

    public Encoding Encoding { init; get; } = Encoding.UTF8;


    // ------ constructors ------ //

    public TcpSocketClient(string ip, int inoutPort)
    {
        _client1 = new();
        _connectionTask = Task.Run(() =>
        {
            try
            {
                _client1 = new(ip, inoutPort);
            }
            catch
            {
                throw new Exception("failed to connect!");
            }
        });
    }

    public TcpSocketClient(string ip, int inPort, int outPort)
    {
        _client1 = new();
        _client2 = new();
        _connectionTask = Task.Run(() =>
        {
            try
            {
                _client1 = new(ip, outPort);
                _client2 = new(ip, inPort);
            }
            catch
            {
                throw new Exception("failed to connect!");
            }
        });
    }


    // ------ public methods ------ //

    public ResultExpression<IDataTransporter> GetStream()
    {
        return GetStreamAsync().Result;
    }

    public async Task<ResultExpression<IDataTransporter>> GetStreamAsync(
        TimeSpan timeout = default,
        CancellationToken ct = default
    )
    {
        if (ct.IsCancellationRequested) return new(false, default!);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (timeout != default) cts.CancelAfter(timeout);
        await _connectionTask.WaitAsync(cts.Token).ConfigureAwait(false);
        if (_client1 is not null && _client2 is null)
        {
            var stream = _client1.GetStream();
            return new(true, new TcpDataTransporter(stream, stream, Encoding, NewLine));
        }
        else if (_client1 is not null && _client2 is not null)
        {
            var stream1 = _client1.GetStream();
            var stream2 = _client2.GetStream();
            return new(true, new TcpDataTransporter(stream1, stream2, Encoding, NewLine));
        }
        else
        {
            return new(false, default!);
        }
    }

    public void Dispose()
    {
        _client1?.Dispose();
        _client2?.Dispose();
    }

}
