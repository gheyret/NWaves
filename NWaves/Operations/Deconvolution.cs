﻿using NWaves.Signals;
using NWaves.Transforms;
using NWaves.Utils;

namespace NWaves.Operations
{
    public static partial class Operation
    {
        /// <summary>
        /// Fast deconvolution via FFT for general complex-valued case
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static ComplexDiscreteSignal Deconvolve(ComplexDiscreteSignal signal, ComplexDiscreteSignal kernel)
        {
            var length = signal.Real.Length - kernel.Real.Length + 1;

            var fftSize = MathUtils.NextPowerOfTwo(signal.Real.Length);

            signal = signal.ZeroPadded(fftSize);
            kernel = kernel.ZeroPadded(fftSize);

            // 1) do FFT of both signals

            Transform.Fft(signal.Real, signal.Imag, fftSize);
            Transform.Fft(kernel.Real, kernel.Imag, fftSize);

            for (var i = 0; i < fftSize; i++)
            {
                signal.Real[i] += 1e-10;
                kernel.Real[i] += 1e-10;
                signal.Imag[i] += 1e-10;
                kernel.Imag[i] += 1e-10;
            }

            // 2) do complex division of spectra

            var spectrum = signal.Divide(kernel);

            // 3) do inverse FFT of resulting spectrum

            Transform.Ifft(spectrum.Real, spectrum.Imag, fftSize);

            // 4) return resulting meaningful part of the signal (truncate to N - M + 1)

            return new ComplexDiscreteSignal(signal.SamplingRate,
                                FastCopy.ArrayFragment(spectrum.Real, length),
                                FastCopy.ArrayFragment(spectrum.Imag, length));
        }

        /// <summary>
        /// Fast deconvolution via FFT for real-valued signals
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static DiscreteSignal Deconvolve(DiscreteSignal signal, DiscreteSignal kernel)
        {
            var complexResult = Deconvolve(signal.ToComplex(), kernel.ToComplex());
            return new DiscreteSignal(signal.SamplingRate, complexResult.Real);
        }
    }
}