using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NAudio.Dsp;
using NAudio.Wave;

namespace Kolorofon
{
    public class SampleAggregator : ISampleProvider
    {
        //moje zmiany
        private readonly BiQuadFilter[] filters;

        // volume
        public event EventHandler<MaxSampleEventArgs> MaximumCalculated;
        private float maxValue;
        private float minValue;
        public int NotificationCount { get; set; }
        int count;

        // FFT
        public event EventHandler<FftEventArgs> FftCalculated;
        public bool PerformFFT { get; set; }
        public bool PerformFilter { get; set; }
        private readonly Complex[][] fftBuffer = new Complex[3][];
        private readonly FftEventArgs fftArgs;
        private int []fftPos = new int[3];
        private readonly int fftLength;
        private int m;
        private readonly ISampleProvider source;

        private readonly int channels;

        private int currentFilter = 0;

        public SampleAggregator(ISampleProvider source, int fftLength = 2048)
        {
            channels = source.WaveFormat.Channels;
            if (!IsPowerOfTwo(fftLength))
            {
                throw new ArgumentException("FFT Length must be a power of two");
            }
            this.m = (int)Math.Log(fftLength, 2.0);
            this.fftLength = fftLength;
            this.fftBuffer[0] = new Complex[fftLength];
            this.fftBuffer[1] = new Complex[fftLength];
            this.fftBuffer[2] = new Complex[fftLength];
            this.fftArgs = new FftEventArgs(fftBuffer[0]);
            this.source = source;


            // moje zmiany
            filters = new BiQuadFilter[3];
            filters[0] = BiQuadFilter.LowPassFilter(44100, 500, 1);
            filters[1] = BiQuadFilter.BandPassFilterConstantPeakGain(44100, 1750, 1);
            filters[2] = BiQuadFilter.HighPassFilter(44100, 3000, 1);

        }

        bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }


        public void Reset()
        {
            count = 0;
            maxValue = minValue = 0;
        }

        private void Add(float value)
        {
            if (PerformFFT && FftCalculated != null)
            {
                int currentFFTPos = fftPos[currentFilter];
                fftBuffer[currentFilter][currentFFTPos].X = (float)(value * FastFourierTransform.HammingWindow(currentFFTPos, fftLength));
                fftBuffer[currentFilter][currentFFTPos].Y = 0;
                currentFFTPos++;
                if (currentFFTPos >= fftBuffer[currentFilter].Length)
                {
                    currentFFTPos = 0;
                    // 1024 = 2^10
                    FastFourierTransform.FFT(true, m, fftBuffer[currentFilter]);
                    fftArgs.currentFilterIndex = currentFilter;
                    fftArgs.Result = fftBuffer[currentFilter];
                    FftCalculated(this, fftArgs);
                }
                fftPos[currentFilter] = currentFFTPos;
            }

            /*maxValue = Math.Max(maxValue, value);
            minValue = Math.Min(minValue, value);
            count++;
            if (count >= NotificationCount && NotificationCount > 0)
            {
                if (MaximumCalculated != null)
                {
                    MaximumCalculated(this, new MaxSampleEventArgs(minValue, maxValue));
                }
                Reset();
            } */
        }

        public WaveFormat WaveFormat { get { return source.WaveFormat; } }

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);
            for (int k=0; k < filters.Length; k++)
            { 
                if (PerformFilter)
                {
                    currentFilter = k;
                    float[] dataToFilter = new float[samplesRead / channels];

                    // sumowanie dwoch kanalow
                    for (int i = 0; i < dataToFilter.Length; i++)
                    {
                        dataToFilter[i] = buffer[offset + (i * 2)] + buffer[offset + (i * 2) + 1];
                    }

                    // filtrowanie
                    for (int i = 0; i < dataToFilter.Length; i++)
                    {
                        dataToFilter[i] = filters[k].Transform(dataToFilter[i]);
                    }

                    // dodanie punktu do liczenia FFT
                    for (int n = 0; n < dataToFilter.Length; n++)
                    {
                        Add(dataToFilter[n]);
                    }
                }
                else
                {
                    for (int n = 0; n < samplesRead; n += channels)
                    {
                        Add(buffer[n]);
                    }
                }
            }

        return samplesRead;
        }
    }

    public class MaxSampleEventArgs : EventArgs
    {
        [DebuggerStepThrough]
        public MaxSampleEventArgs(float minValue, float maxValue)
        {
            this.MaxSample = maxValue;
            this.MinSample = minValue;
        }
        public float MaxSample { get; private set; }
        public float MinSample { get; private set; }
    }

    public class FftEventArgs : EventArgs
    {
        [DebuggerStepThrough]
        public FftEventArgs(Complex[] result)
        {
            this.Result = result;
        }
        public Complex[] Result { get; set; }

        public int currentFilterIndex { get; set; }
    }
}
