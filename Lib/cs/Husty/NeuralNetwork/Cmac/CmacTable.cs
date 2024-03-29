﻿namespace Husty.NeuralNetwork.Cmac;

public sealed class CmacTable
{

  // ------ fields ------ //

  private readonly int[] _activeLocation;
  private readonly float _min, _max;
  private readonly float[] _offsets, _steps;
  private readonly CmacLabelInfo[] _infos;
  private readonly MultidimensionalArray _array;


  // ------ properties ------ //

  public int DimensionCount => _activeLocation.Length;

  public int[] ActiveLocationIndex => _activeLocation;

  public float ActiveValue => _array.GetAt(_activeLocation);


  // ------ constructors ------ //

  public CmacTable(
      IEnumerable<CmacLabelInfo> labelInfos,
      float min,
      float max,
      float initialValue
  ) : this(
      labelInfos,
      min, max,
      Enumerable.Repeat(initialValue, labelInfos.Aggregate(1, (s, v) => s *= v.GridCount)).ToArray()
  )
  { }

  public CmacTable(
      IEnumerable<CmacLabelInfo> labelInfos,
      float min,
      float max,
      float[] initialValues
  )
  {
    _min = min;
    _max = max;

    _infos = labelInfos.ToArray();
    var dims = new int[_infos.Length];
    _offsets = new float[_infos.Length];
    _steps = new float[_infos.Length];
    for (int i = 0; i < _infos.Length; i++)
    {
      dims[i] = _infos[i].GridCount;
      _offsets[i] = _infos[i].Lower;
      _steps[i] = (_infos[i].Upper - _infos[i].Lower) / _infos[i].GridCount;
    }

    _array = new(dims);
    if (initialValues.Length != _array.GetTotalSize())
      throw new ArgumentException("invalid input values length");
    _array.SetAll(initialValues);
    _activeLocation = new int[_infos.Length];
  }


  // ------ public methods ------ //

  public void FixLocation(float[] state)
  {
    for (int i = 0; i < _activeLocation.Length; i++)
    {
      var maxIndex = _array.GetLength(i);
      _activeLocation[i] = maxIndex - 1;
      for (int x = 0; x < maxIndex; x++)
      {
        if (state[i] < _offsets[i] + x * _steps[i])
        {
          _activeLocation[i] = x;
          break;
        }
      }
    }
  }

  public void ApplyPenalty(float value)
  {
    _array.MinusAt(_activeLocation, value);
    if (_array.GetAt(_activeLocation) < _min)
      _array.SetAt(_activeLocation, _min);
    else if (_array.GetAt(_activeLocation) > _max)
      _array.SetAt(_activeLocation, _max);
  }

  public float[] GetParams() => _array.GetAll();

  public void SetParams(float[] value) => _array.SetAll(value);

  public CmacTable Clone() => new(_infos, _min, _max, _array.GetAll().Clone() as float[]);

}

