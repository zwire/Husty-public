﻿using System.Reactive.Concurrency;
using System.Reactive.Linq;
using OpenCvSharp;

namespace Husty.OpenCvSharp.ImageStream;

public sealed class VideoStream : IVideoStream<Mat>
{

    // ------ fields ------ //

    private int _positionIndex;
    private readonly VideoCapture _cap;
    private readonly ObjectPool<Mat> _pool;


    // ------ properties ------ //

    public int Fps { get; }

    public int Channels { get; }

    public Size FrameSize { get; }

    public bool HasFrame { private set; get; }

    public int FrameCount { get; }

    public int CurrentPosition => _positionIndex;

    public bool IsEnd => _positionIndex >= FrameCount - 1;


    // ------ constructors ------ //

    public VideoStream(string src, IEnumerable<Properties> properties = null)
    {
        _cap = new(src);
        _pool = new(2, () => new Mat());
        if (properties is not null)
            foreach (var p in properties)
                _cap.Set(p.Key, p.Value);
        Fps = (int)_cap.Fps;
        Channels = (int)_cap.Get(VideoCaptureProperties.Channel);
        FrameSize = new(_cap.FrameWidth, _cap.FrameHeight);
        FrameCount = _cap.FrameCount;
    }


    // ------ public methods ------ //

    public Mat Read()
    {
        if (_positionIndex == FrameCount - 1) return null;
        _cap.Set(VideoCaptureProperties.PosFrames, _positionIndex++);
        var frame = _pool.GetObject();
        HasFrame = _cap.Read(frame);
        if (HasFrame)
            return frame;
        else
            return null;
    }

    public IObservable<Mat> GetStream()
    {
        return Observable.Interval(TimeSpan.FromMilliseconds(1000 / Fps), ThreadPoolScheduler.Instance)
            .Where(_ => _positionIndex < FrameCount)
            .Select(_ => Read())
            .TakeWhile(x => x is not null)
            .Publish().RefCount();
    }

    public void Seek(int position)
    {
        if (position > -1 && position < FrameCount) _positionIndex = position;
    }

    public void Dispose()
    {
        HasFrame = false;
        _cap?.Dispose();
        _pool?.Dispose();
    }

}
