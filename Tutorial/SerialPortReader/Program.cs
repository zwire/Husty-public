﻿using System.Reactive.Linq;
using Husty.Communication;
using Husty.Extensions;

// get ports
var names = SerialPortDataTransporter.GetPortNames();
if (names.Length is 0) throw new Exception("find no port!");

// access first found port
var port = new SerialPortDataTransporter(names.First(), 115250);

// read messages until key interrupt
ObservableEx2.Loop(async () =>
  {
    var result = await port.TryReadLineAsync();
    return result.Match(x => x, e => e.ToString());
  })
  .Where(x => x != null)
  .Subscribe(Console.WriteLine);
ConsoleEx.WaitKey(ConsoleKey.Enter);

// finalize
Console.WriteLine("completed.");
port.Dispose();