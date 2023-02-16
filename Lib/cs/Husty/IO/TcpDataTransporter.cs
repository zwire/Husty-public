﻿using System.Net.Sockets;
using System.Text;

namespace Husty.IO;

public sealed class TcpDataTransporter : DataTransporterBase
{

    // ------ fields ------ //

    private readonly Stream _writingStream;
    private readonly Stream _readingStream;
    private readonly StreamWriter _writer;
    private readonly StreamReader _reader;


    // ------ properties ------ //

    public Stream BaseWritingStream => _writingStream;

    public Stream BaseReadingStream => _readingStream;

    
    // ------ constructors ------ //

    internal TcpDataTransporter(
        Stream writingStream,
        Stream readingStream, 
        Encoding encoding,
        string newLine
    )
    {
        _writingStream = writingStream;
        _readingStream = readingStream;
        _writer = new(_writingStream, encoding) { NewLine = newLine };
        _reader = new(_readingStream, encoding);
    }


    // ------ public methods ------ //

    protected override void DoDispose()
    {
        _writingStream?.Dispose();
        _readingStream?.Dispose();
    }

    protected override async Task<bool> DoTryWriteAsync(byte[] data, CancellationToken ct)
    {
        try
        {
            await _writingStream.WriteAsync(data, 0, data.Length, ct).ConfigureAwait(false);
            await _writingStream.FlushAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override async Task<ResultExpression<byte[]>> DoTryReadAsync(int count, CancellationToken ct)
    {
        var bytes = new byte[count];
        try
        {
            var offset = 0;
            do
            {
                var size = await _readingStream.ReadAsync(bytes, offset, count, ct).ConfigureAwait(false);
                if (size is 0) break;
                offset += size;
                count -= size;
            } while (count > 0);
            if (offset is 0) return new(false, default!);
            return new(true, bytes);
        }
        catch
        {
            return new(false, default!);
        }
    }

    protected override async Task<bool> DoTryWriteLineAsync(string data, CancellationToken ct)
    {
        try
        {
            await _writer.WriteLineAsync(new StringBuilder(data), ct).ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override async Task<ResultExpression<string>> DoTryReadLineAsync(CancellationToken ct)
    {
        try
        {
            var line = await _reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null) return new(false, default!);
            return new(true, line);
        }
        catch
        {
            return new(false, default!);
        }
    }

}
