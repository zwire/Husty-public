﻿using System;
using System.IO;
using OpenCvSharp;

namespace Husty.OpenCvSharp
{

	public class IntrinsicCameraParameters
	{

		public Size ImageSize { get; }

		public Mat CameraMatrix { get; }

		public Mat DistortionCoeffs { get; }

		public IntrinsicCameraParameters WithoutDistCoeffs
			=> new(ImageSize, CameraMatrix, new Mat(5, 1, MatType.CV_64F, 0));


		public IntrinsicCameraParameters(Size imageSize, Mat cameraMatrix, Mat distortionCoeffs)
		{
			if (!(cameraMatrix.Rows == 3 && cameraMatrix.Cols == 3))
				throw new ArgumentException("Requires: 3x3 matrix.", nameof(cameraMatrix));
			if (!(distortionCoeffs.Cols == 1 &&
				(distortionCoeffs.Rows == 4 || distortionCoeffs.Rows == 5 || distortionCoeffs.Rows == 8 || distortionCoeffs.Rows == 14)))
				throw new ArgumentException("Requires: 4x1, 5x1, 8x1 or 14x1 matrix.", nameof(distortionCoeffs));
			ImageSize = imageSize;
			CameraMatrix = cameraMatrix;
			DistortionCoeffs = distortionCoeffs;
		}

		public IntrinsicCameraParameters Clone()
			=> new(ImageSize, CameraMatrix, DistortionCoeffs);

		public void Save(string fileName)
		{
			var obj = new IntrinsicJson(this);
			File.WriteAllText(fileName, obj.Serialize());
		}

		public static IntrinsicCameraParameters Load(string fileName)
		{
			var str = File.ReadAllText(fileName);
			return IntrinsicJson.Deserialize(str);
		}

	}

}
