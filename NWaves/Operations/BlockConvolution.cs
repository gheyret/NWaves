﻿using System;
using NWaves.Signals;
using NWaves.Transforms;
using NWaves.Utils;

namespace NWaves.Operations
{
    public static partial class Operation
    {
        /// <summary>
        /// Method implements the Overlap-Add algorithm of a block convolution.
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="kernel"></param>
        /// <param name="fftSize"></param>
        /// <returns></returns>
        public static DiscreteSignal OverlapAdd(DiscreteSignal signal, DiscreteSignal kernel, int fftSize)
        {
            var m = kernel.Length;

            if (m > fftSize)
            {
                throw new ArgumentException("Kernel length must not exceed the size of FFT!");
            }

            // pre-compute kernel's FFT:

            var kernelReal = FastCopy.PadZeros(kernel.Samples, fftSize);
            var kernelImag = new double[fftSize];
            Transform.Fft(kernelReal, kernelImag, fftSize);

            // reserve space for current signal block:

            var blockReal = new double[fftSize];
            var blockImag = new double[fftSize];
            var zeroblock = new double[fftSize];

            // reserve space for resulting spectrum at each step:

            var spectrumReal = new double[fftSize];
            var spectrumImag = new double[fftSize];

            var filtered = new DiscreteSignal(signal.SamplingRate, signal.Length);

            var hopSize = fftSize - m + 1;
            var i = 0;
            while (i + hopSize < signal.Length)
            {
                // ============================== FFT CONVOLUTION SECTION =================================

                // for better performance we inline FFT convolution here;
                // alternatively, we could simply write:
                //
                //        var res = Convolve(signal[i, i + hopSize], kernel);
                //
                // ...but that would require too many unnecessary memory allocations.
                
                FastCopy.ToExistingArray(zeroblock, blockReal, fftSize);
                FastCopy.ToExistingArray(zeroblock, blockImag, fftSize);
                FastCopy.ToExistingArray(signal.Samples, blockReal, hopSize, i);

                // 1) do FFT of a signal block:
                
                Transform.Fft(blockReal, blockImag, fftSize);

                // 2) do complex multiplication of spectra

                for (var j = 0; j < fftSize; j++)
                {
                    spectrumReal[j] = (blockReal[j] * kernelReal[j] - blockImag[j] * kernelImag[j]) / fftSize;
                    spectrumImag[j] = (blockReal[j] * kernelImag[j] + blockImag[j] * kernelReal[j]) / fftSize;
                }

                // 3) do inverse FFT of resulting spectrum

                Transform.Ifft(spectrumReal, spectrumImag, fftSize);

                // ========================================================================================


                for (var j = 0; j < m - 1; j++)
                {
                    filtered[i + j] += spectrumReal[j];
                }

                for (var j = m - 1; j < spectrumReal.Length; j++)
                {
                    filtered[i + j] = spectrumReal[j];
                }

                i += hopSize;
            }


            // process last portion of data:

            FastCopy.ToExistingArray(zeroblock, blockReal, fftSize);
            FastCopy.ToExistingArray(zeroblock, blockImag, fftSize);
            FastCopy.ToExistingArray(signal.Samples, blockReal, signal.Length - i, i);

            Transform.Fft(blockReal, blockImag, fftSize);

            for (var j = 0; j < fftSize; j++)
            {
                spectrumReal[j] = (blockReal[j] * kernelReal[j] - blockImag[j] * kernelImag[j]) / fftSize;
                spectrumImag[j] = (blockReal[j] * kernelImag[j] + blockImag[j] * kernelReal[j]) / fftSize;
            }

            Transform.Ifft(spectrumReal, spectrumImag, fftSize);
            
            for (var j = 0; j < m - 1 && i + j < filtered.Length; j++)
            {
                filtered[i + j] += spectrumReal[j];
            }
            for (var j = m - 1; i + j < filtered.Length; j++)
            {
                filtered[i + j] = spectrumReal[j];
            }

            return filtered;
        }

        /// <summary>
        /// Method implements the Overlap-Save algorithm of a block convolution
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="kernel"></param>
        /// <param name="fftSize"></param>
        /// <returns></returns>
        public static DiscreteSignal OverlapSave(DiscreteSignal signal, DiscreteSignal kernel, int fftSize)
        {
            var m = kernel.Length;

            if (m > fftSize)
            {
                throw new ArgumentException("Kernel length must not exceed the size of FFT!");
            }

            // pre-compute kernel's FFT:

            var kernelReal = FastCopy.PadZeros(kernel.Samples, fftSize);
            var kernelImag = new double[fftSize];
            Transform.Fft(kernelReal, kernelImag, fftSize);

            // reserve space for current signal block:

            var blockReal = new double[fftSize];
            var blockImag = new double[fftSize];
            var zeroblock = new double[fftSize];

            // reserve space for resulting spectrum at each step:

            var spectrumReal = new double[fftSize];
            var spectrumImag = new double[fftSize];

            var filtered = new DiscreteSignal(signal.SamplingRate, signal.Length);

            signal = new DiscreteSignal(signal.SamplingRate, m - 1) + signal;
            
            var hopSize = fftSize - m + 1;
            var i = 0;
            while (i + fftSize < signal.Length)
            {
                // ============================== FFT CONVOLUTION SECTION =================================

                // for better performance we inline FFT convolution here;
                // alternatively, we could simply write:
                //
                //        var res = Convolve(signal[i, i + fftSize], kernel);
                //
                // ...but that would require too many unnecessary memory allocations.

                FastCopy.ToExistingArray(signal.Samples, blockReal, fftSize, i);
                FastCopy.ToExistingArray(zeroblock, blockImag, fftSize);

                // 1) do FFT of a signal block:

                Transform.Fft(blockReal, blockImag, fftSize);

                // 2) do complex multiplication of spectra

                for (var j = 0; j < fftSize; j++)
                {
                    spectrumReal[j] = (blockReal[j] * kernelReal[j] - blockImag[j] * kernelImag[j]) / fftSize;
                    spectrumImag[j] = (blockReal[j] * kernelImag[j] + blockImag[j] * kernelReal[j]) / fftSize;
                }

                // 3) do inverse FFT of resulting spectrum

                Transform.Ifft(spectrumReal, spectrumImag, fftSize);

                // ========================================================================================
                

                for (var j = 0; j + m - 1 < spectrumReal.Length; j++)
                {
                    filtered[i + j] = spectrumReal[j + m - 1];
                }

                i += hopSize;
            }

            // process last portion of data:

            FastCopy.ToExistingArray(zeroblock, blockReal, fftSize);
            FastCopy.ToExistingArray(zeroblock, blockImag, fftSize);
            FastCopy.ToExistingArray(signal.Samples, blockReal, signal.Length - i, i);

            Transform.Fft(blockReal, blockImag, fftSize);

            for (var j = 0; j < fftSize; j++)
            {
                spectrumReal[j] = (blockReal[j] * kernelReal[j] - blockImag[j] * kernelImag[j]) / fftSize;
                spectrumImag[j] = (blockReal[j] * kernelImag[j] + blockImag[j] * kernelReal[j]) / fftSize;
            }

            Transform.Ifft(spectrumReal, spectrumImag, fftSize);

            for (var j = 0; i + j < filtered.Length; j++)
            {
                filtered[i + j] = spectrumReal[j + m - 1];
            }

            return filtered;
        }
    }
}
