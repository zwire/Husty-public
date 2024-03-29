﻿using OpenCvSharp;

namespace Husty.OpenCvSharp.Transform;

public sealed class Fourier2D
{

  // ------ fields ------ //

  private readonly Mat _complex;


  // ------ properties ------ //

  public Mat ViewImage { private set; get; }


  // ------ constructors ------ //

  /// <summary>
  /// 2D Fourier transformation for image data.
  /// </summary>
  /// <param name="image">Must input CV_8U type</param>
  public Fourier2D(Mat image)
  {
    if (image.Type() != MatType.CV_8U) throw new Exception("MatType must be CV_8U.");
    using var padded = new Mat();
    var r = Cv2.GetOptimalDFTSize(image.Rows);
    var c = Cv2.GetOptimalDFTSize(image.Cols);
    Cv2.CopyMakeBorder(image, padded, 0, r - image.Rows, 0, c - image.Cols, BorderTypes.Constant, new Scalar(0));
    padded.ConvertTo(padded, MatType.CV_32F);
    var planes = new Mat[] { padded, new Mat(padded.Rows, padded.Cols, MatType.CV_32F, new Scalar(0)) };
    ViewImage = new Mat(padded.Rows, padded.Cols, MatType.CV_8U, new Scalar(0));
    _complex = new Mat();
    Cv2.Merge(planes, _complex);
  }


  // ------ public methods ------ //

  /// <summary>
  /// Discrete Fourier Transformation
  /// </summary>
  /// <remarks>Frequency feature of image data</remarks>
  public unsafe float[] Dft()
  {
    Cv2.Dft(_complex, _complex);
    Cv2.Split(_complex, out var planes);
    Cv2.Magnitude(planes[0], planes[1], planes[0]);
    var tmp = planes[0];
    var feature = new float[tmp.Rows * tmp.Cols];
    var data = (float*)tmp.Data;
    for (int i = 0; i < feature.Length; i++)
      feature[i] = data[i];
    tmp += new Scalar(1);
    Cv2.Log(tmp, tmp);
    Cv2.Normalize(tmp, tmp, 255, 0, NormTypes.MinMax);
    tmp.ConvertTo(tmp, MatType.CV_8U);
    var cx = tmp.Width / 2;
    var cy = tmp.Height / 2;
    var tl = new Rect(0, 0, cx, cy);
    var tr = new Rect(cx, 0, cx, cy);
    var bl = new Rect(0, cy, cx, cy);
    var br = new Rect(cx, cy, cx, cy);
    using var tl1 = tmp[tl];
    using var tr1 = tmp[tr];
    using var bl1 = tmp[bl];
    using var br1 = tmp[br];
    ViewImage = new Mat(tmp.Rows, tmp.Cols, MatType.CV_8U);
    tl1.CopyTo(ViewImage[br]);
    tr1.CopyTo(ViewImage[bl]);
    bl1.CopyTo(ViewImage[tr]);
    br1.CopyTo(ViewImage[tl]);
    return feature;
  }

  /// <summary>
  /// Inverse Discrete Fourier Transformation
  /// </summary>
  /// <returns>Feature of values</returns>
  public unsafe float[] Idft()
  {
    Cv2.Idft(_complex, _complex, DftFlags.Scale);
    Cv2.Split(_complex, out Mat[] planes);
    using var tmp = planes[0][new Rect(0, 0, planes[0].Width, planes[0].Height)].Clone();
    var feature = new float[tmp.Rows * tmp.Cols];
    var data = (float*)tmp.Data;
    for (int i = 0; i < feature.Length; i++)
      feature[i] = data[i];
    Cv2.Normalize(tmp, tmp, 255, 0, NormTypes.MinMax);
    tmp.ConvertTo(ViewImage, MatType.CV_8U);
    return feature;
  }

}
