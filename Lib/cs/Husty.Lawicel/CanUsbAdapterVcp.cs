﻿using System.Globalization;
using System.IO.Ports;

namespace Husty.Lawicel;

public class CanUsbAdapterVcp : ICanUsbAdapter
{

    // ------ fields ------ //

    private bool _disposed;
    private readonly string _baudrate;
    private readonly SerialPort _port;


    // ------ properties ------ //

    public string AdapterName { get; }

    public string Baudrate { get; }

    public CanUsbStatus Status { private set; get; } = CanUsbStatus.Offline;


    // ------ constructors ------ //

    public CanUsbAdapterVcp(string portName, string baudrate)
    {
        _baudrate = baudrate switch
        {
            CanUsbOption.BAUD_10K => "0",
            CanUsbOption.BAUD_20K => "1",
            CanUsbOption.BAUD_50K => "2",
            CanUsbOption.BAUD_100K => "3",
            CanUsbOption.BAUD_125K => "4",
            CanUsbOption.BAUD_250K => "5",
            CanUsbOption.BAUD_500K => "6",
            CanUsbOption.BAUD_800K => "7",
            CanUsbOption.BAUD_1M => "8",
            _ => throw new Exception()
        };
        _port = new()
        {
            PortName = portName,
            BaudRate = 9600,
            NewLine = "\r"
        };
    }


    // ------ public methods ------ //

    public static string[] FindAdapterNames(string baudrate)
    {
        baudrate = baudrate switch
        {
            CanUsbOption.BAUD_10K => "0",
            CanUsbOption.BAUD_20K => "1",
            CanUsbOption.BAUD_50K => "2",
            CanUsbOption.BAUD_100K => "3",
            CanUsbOption.BAUD_125K => "4",
            CanUsbOption.BAUD_250K => "5",
            CanUsbOption.BAUD_500K => "6",
            CanUsbOption.BAUD_800K => "7",
            CanUsbOption.BAUD_1M => "8",
            _ => throw new Exception()
        };
        var list = new List<string>();
        foreach (var portName in SerialPort.GetPortNames())
        {
            using var port = new SerialPort()
            {
                PortName = portName,
                BaudRate = 9600,
                NewLine = "\r"
            };
            port.Open();
            Thread.Sleep(20);
            if (port.IsOpen)
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                port.Write("C\r");
                port.Write($"S{baudrate}\r");
                port.Write("Z1\r");
                port.Write("O\r");
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(20);
                    port.Write("N\r");
                    var line = port.ReadLine();
                    if (line.FirstOrDefault() is 'N' or 't' or 'T' or 'r' or 'R')
                    {
                        list.Add(portName);
                        break;
                    }
                }
            }
        }
        return list.ToArray();
    }

    public void Open()
    {
        _port.Open();
        _port.DiscardInBuffer();
        _port.DiscardOutBuffer();
        _port.Write("C\r");
        _port.Write($"S{_baudrate}\r");
        _port.Write("Z1\r");
        _port.Write("O\r");
        Status = CanUsbStatus.Online;
    }

    public void Close()
    {
        if (_disposed) return;
        if (Status is CanUsbStatus.Offline) return;
        if (_port.IsOpen)
        {
            Status = CanUsbStatus.Offline;
            _port.Write("C\r");
            Thread.Sleep(100);
            _port.Close();
        }
    }

    public void Write(CanMessage message)
    {
        if (_disposed) return;
        var msg = message.Flags is CanUsbOption.EXTENDED ? "T" : "t";
        msg += message.Id.ToString(msg is "t" ? "X3" : "X8");
        msg += message.Length;
        foreach (var d in BitConverter.GetBytes(message.Data))
            msg += d.ToString("X2");
        msg += "\r";
        _port.Write(msg);
    }

    public CanMessage Read()
    {
        if (_disposed) return null;
        if (_port.IsOpen)
        {
            var line = _port.ReadLine();
            var mode = line.FirstOrDefault();
            if (mode is 't')
            {
                if (
                    line.Length > 6 &&
                    uint.TryParse(line[1..4], NumberStyles.AllowHexSpecifier, null, out var id) &&
                    byte.TryParse(line[4..5], out var len)
                )
                {
                    var pos = 5 + len * 2;
                    if (
                        ulong.TryParse(line[5..pos], NumberStyles.AllowHexSpecifier, null, out var data) &&
                        uint.TryParse(line[pos..], NumberStyles.AllowHexSpecifier, null, out var timestamp)
                    )
                    {
                        var ary = BitConverter.GetBytes(data).Reverse().ToArray();
                        return new(id, ary, default, len, timestamp);
                    }
                }
            }
            else if (mode is 'T')
            {
                if (
                    line.Length > 11 &&
                    uint.TryParse(line[1..9], NumberStyles.AllowHexSpecifier, null, out var id) &&
                    byte.TryParse(line[9..10], out var len)
                )
                {
                    var pos = 10 + len * 2;
                    if (
                        ulong.TryParse(line[10..pos], NumberStyles.AllowHexSpecifier, null, out var data) &&
                        uint.TryParse(line[pos..], NumberStyles.AllowHexSpecifier, null, out var timestamp)
                    )
                    {
                        var ary = BitConverter.GetBytes(data).Reverse().ToArray();
                        return new(id, ary, default, len, timestamp);
                    }
                }
            }
        }
        return null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Close();
            GC.SuppressFinalize(this);
            _disposed = true;
        }
    }

}
