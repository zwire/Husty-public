﻿using System.Numerics;

namespace Husty.Extensions;

public static class ArrayEx
{

  public static int ArgMax<T>(this IEnumerable<T> src, out double max) where T : INumber<T>
  {
    max = double.MinValue;
    var index = 0;
    int i = 0;
    foreach (var s in src)
    {
      var num = Convert.ToDouble(s);
      if (num > max)
      {
        max = num;
        index = i;
      }
      i++;
    }
    return index;
  }

  public static int ArgMin<T>(this IEnumerable<T> src, out double min) where T : INumber<T>
  {
    min = double.MaxValue;
    var index = 0;
    int i = 0;
    foreach (var s in src)
    {
      var num = Convert.ToDouble(s);
      if (num < min)
      {
        min = num;
        index = i;
      }
      i++;
    }
    return index;
  }

  public static double Median<T>(this IEnumerable<T> src) where T : INumber<T>
  {
    if (!src.Any()) throw new InvalidOperationException("Cannot compute median for an empty set.");
    var doubleArray = src.Select(a => Convert.ToDouble(a)).OrderBy(x => x).ToArray();
    var len = doubleArray.Length;
    var odd = len % 2 is not 0;
    var median = odd ? doubleArray[len / 2] : (doubleArray[len / 2 - 1] + doubleArray[len / 2]) / 2.0;
    return median;
  }

  public static double Variance<T>(this IEnumerable<T> src) where T : INumber<T>
  {
    if (!src.Any()) throw new InvalidOperationException("Cannot compute median for an empty set.");
    var doubleArray = src.Select(a => Convert.ToDouble(a)).ToArray();
    var mean = doubleArray.Average();
    var sum2 = doubleArray.Select(a => a * a).Sum();
    var variance = sum2 / doubleArray.Length - mean * mean;
    return variance;
  }

  public static double StdDev<T>(this IEnumerable<T> src)
      where T : INumber<T>
  {
    return Math.Sqrt(Variance(src));
  }

}
