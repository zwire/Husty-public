﻿using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

// k ... State Vector Length
// m ... Measurement Vector Length
// n ... Control Vector Length
// 
// If you describe Matrix like,
// 
//     M = A B C
//         D E F
//         G H I
//         
// you should write code like this,
// 
// m = { a, b, c, d, e, f, g, h, i }
// 
// Simple version constructor is available if you need.

namespace Husty
{
    /// <summary>
    /// Filtering & control methods subject to Gaussian distribution
    /// </summary>
    public sealed class ParticleFilter
    {

        // ------ fields ------ //

        private readonly int k;                 // State Vector Length
        private readonly int m;                 // Measurement Vector Length
        private readonly int n;                 // Control Vector Length
        private readonly int N;                 // Count of Particle
        private readonly Matrix<double> A;      // Transition Matrix
        private readonly Matrix<double> B;      // Control Matrix
        private readonly Matrix<double> C;      // Measure Matrix
        private readonly Matrix<double> Q;      // Process Noise Matrix
        private readonly Matrix<double> R;      // Measure Noise Matrix
        private readonly Matrix<double> RInv;
        private readonly double _denominator;


        // ------ properties ------ //

        public List<Vector<double>> Particles { private set; get; }


        // ------ constructors ------ //

        /// <summary>
        /// Use same status and observe parameter.
        /// You can input control.
        /// Noise covariance is default value.
        /// </summary>        
        /// <param name="initialStateVec">X (k * 1)</param>
        /// <param name="controlMatrix">B (k * n)</param>
        /// <param name="filterStrength"></param>
        /// <param name="N">Particles Count (default)</param>
        public ParticleFilter(double[] initialStateVec, double[]? controlMatrix, double filterStrength = 1.0, int N = 100)
        {

            k = initialStateVec.Length;
            m = initialStateVec.Length;
            n = controlMatrix is not null ? controlMatrix.Length / k : 0;
            this.N = N;
            B = controlMatrix is not null ? new DenseMatrix(n, k, controlMatrix).Transpose() : null;
            C = DenseMatrix.OfArray(new double[m, k]);
            A = DenseMatrix.OfArray(new double[k, k]);
            R = DenseMatrix.OfArray(new double[m, m]);
            Q = DenseMatrix.OfArray(new double[k, k]);
            Particles = new List<Vector<double>>();
            for (int i = 0; i < k; i++)
            {
                C[i, i] = 1;
                A[i, i] = 1;
                R[i, i] = filterStrength;
                Q[i, i] = 1.0 / filterStrength;
            }
            for (int i = 0; i < N; i++)
                Particles.Add(MakeVectorRandom(new DenseVector(initialStateVec)));
            _denominator = Math.Sqrt(Math.Pow(2 * Math.PI, k) * R.Determinant());
            RInv = R.Inverse();
        }

        /// <summary>
        /// Use same status and observe parameter.
        /// You can input control.
        /// Noise covariance is default value.
        /// </summary>        
        /// <param name="initialStateVec">X (k * 1)</param>
        /// <param name="controlMatrix">B (k * n)</param>
        /// <param name="measurementNoise">R (default)</param>
        /// <param name="processNoise">Q (default)</param>
        /// <param name="N">Particles Count (default)</param>
        public ParticleFilter(double[] initialStateVec, double[]? controlMatrix, double measurementNoise = 0.01, double processNoise = 0.01, int N = 100)
        {

            k = initialStateVec.Length;
            m = initialStateVec.Length;
            n = controlMatrix is not null ? controlMatrix.Length / k : 0;
            this.N = N;
            B = controlMatrix is not null ? new DenseMatrix(n, k, controlMatrix).Transpose() : null;
            C = DenseMatrix.OfArray(new double[m, k]);
            A = DenseMatrix.OfArray(new double[k, k]);
            R = DenseMatrix.OfArray(new double[m, m]);
            Q = DenseMatrix.OfArray(new double[k, k]);
            Particles = new List<Vector<double>>();
            for (int i = 0; i < k; i++)
            {
                C[i, i] = 1;
                A[i, i] = 1;
                R[i, i] = measurementNoise;
                Q[i, i] = processNoise;
            }
            for (int i = 0; i < N; i++)
                Particles.Add(MakeVectorRandom(new DenseVector(initialStateVec)));
            _denominator = Math.Sqrt(Math.Pow(2 * Math.PI, k) * R.Determinant());
            RInv = R.Inverse();
        }

        /// <summary>
        /// In the case of that observe differ from status.
        /// You can input control.
        /// </summary>
        /// <param name="initialStateVec">X (k * 1)</param>
        /// <param name="controlMatrix">B (k * n)</param>
        /// <param name="transitionMatrix">A (k * k)</param>
        /// <param name="measurementMatrix">C (m * k)</param>
        /// <param name="measurementNoise">R (default)</param>
        /// <param name="processNoise">Q (default)</param>
        /// <param name="N">Particles Count(default)</param>
        public ParticleFilter(double[] initialStateVec, double[]? controlMatrix, double[] transitionMatrix, double[] measurementMatrix, double measurementNoise = 0.01, double processNoise = 0.01, int N = 100)
        {
            k = initialStateVec.Length;
            m = measurementMatrix.Length / k;
            n = controlMatrix is not null ? controlMatrix.Length / k : 0;
            this.N = N;
            B = controlMatrix is not null ? new DenseMatrix(n, k, controlMatrix).Transpose() : null;
            C = new DenseMatrix(k, m, measurementMatrix).Transpose();
            A = new DenseMatrix(k, k, transitionMatrix).Transpose();
            R = DenseMatrix.OfArray(new double[m, m]);
            Q = DenseMatrix.OfArray(new double[k, k]);
            Particles = new List<Vector<double>>();
            for (int i = 0; i < k; i++)
                Q[i, i] = processNoise;
            for (int i = 0; i < m; i++)
                R[i, i] = measurementNoise;
            for (int i = 0; i < N; i++)
                Particles.Add(MakeVectorRandom(new DenseVector(initialStateVec)));
            _denominator = Math.Sqrt(Math.Pow(2 * Math.PI, k) * R.Determinant());
            RInv = R.Inverse();

        }

        /// <summary>
        /// You can design noise covariance matrix.
        /// But can input control.
        /// </summary>
        /// <param name="initialStateVec">X (k * 1)</param>
        /// <param name="controlMatrix">B (k * n)</param>
        /// <param name="transitionMatrix">A (k * k)</param>
        /// <param name="measurementMatrix">C (m * k)</param>
        /// <param name="measurementNoiseMatrix">R (m * m)</param>
        /// <param name="processNoiseMatrix">Q (k * k)</param>
        /// <param name="N">Particles Count (default)</param>
        public ParticleFilter(double[] initialStateVec, double[]? controlMatrix, double[] transitionMatrix, double[] measurementMatrix, double[] measurementNoiseMatrix, double[] processNoiseMatrix, int N = 100)
        {
            k = initialStateVec.Length;
            m = measurementMatrix.Length / k;
            n = controlMatrix is not null ? controlMatrix.Length / k : 0;
            this.N = N;
            B = controlMatrix is not null ? new DenseMatrix(n, k, controlMatrix).Transpose() : null;
            C = new DenseMatrix(k, m, measurementMatrix).Transpose();
            A = new DenseMatrix(k, k, transitionMatrix).Transpose();
            R = new DenseMatrix(m, m, measurementNoiseMatrix).Transpose();
            Q = new DenseMatrix(k, k, processNoiseMatrix).Transpose();
            Particles = new List<Vector<double>>();
            for (int i = 0; i < N; i++)
                Particles.Add(MakeVectorRandom(new DenseVector(initialStateVec)));
            _denominator = Math.Sqrt(Math.Pow(2 * Math.PI, k) * R.Determinant());
            RInv = R.Inverse();

        }


        // ------ public methods ------ //

        public (double[] Correct, double[] Predict) Update(double[] measurementVec, double[]? controlVec = null)
        {
            var y = new DenseVector(measurementVec);
            var wList = new List<double>();
            foreach (var x in Particles)
                wList.Add(CalcLikelihood(x, y));
            var wSum = wList.Sum();
            var correct = new double[k];
            for (int i = 0; i < N; i++)
            {
                wList[i] /= wSum;
                for (int j = 0; j < k; j++)
                    correct[j] += Particles[i][j] * wList[i];
            }
            Particles = Resample(wList).ToList();
            Particles = PredictNextState(controlVec).ToList();
            var predict = new double[k];
            for (int i = 0; i < N; i++)
                for (int j = 0; j < k; j++)
                    predict[j] += Particles[i][j] * wList[i];
            return (correct, predict);
        }


        // ------ private methods ------ //

        private DenseVector MakeVectorRandom(Vector<double> vec)
        {
            var value = new double[k];
            for (int i = 0; i < k; i++)
                value[i] = Normal.Samples(vec[i], Q[i, i]).Take(1).ToArray()[0];
            return new DenseVector(value);
        }

        private double CalcLikelihood(Vector<double> x, Vector<double> y)
        {
            var err = y - C * x;
            var errT = err.ToRowMatrix();
            var index = (-errT * RInv * err / 2)[0];
            return Math.Exp(index) / _denominator;
        }

        private IEnumerable<Vector<double>> Resample(List<double> likelihoodList)
        {
            for (double d = 1.0 / N / 2; d < 1.0; d += 1.0 / N)
            {
                var sum = 0.0;
                for (int i = 0; i < N; i++)
                {
                    sum += likelihoodList[i];
                    if (sum > d)
                    {
                        yield return Particles[i];
                        break;
                    }
                }
            }
        }

        private IEnumerable<Vector<double>> PredictNextState(double[]? controlVec)
        {
            if (B is null || controlVec is null)
                foreach (var p in Particles)
                    yield return MakeVectorRandom(A * p);
            else
                foreach (var p in Particles)
                    yield return MakeVectorRandom(A * p + B * new DenseVector(controlVec));
        }

    }
}