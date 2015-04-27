using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Dsp;
using System.Diagnostics;

namespace Kolorofon
{
    public partial class SpectrumAnalyser : UserControl
    {
        private double xScale = 200;
        private int bins = 1024; // guess a 1024 size FFT, bins is half FFT size
        private int filterNumber = 3;

        public int LedSize { get; set; }

        double[][] aggregatedValues;

        public SpectrumAnalyser()
        {
            InitializeComponent();
            CalculateXScale();
            this.SizeChanged += SpectrumAnalyser_SizeChanged;
            this.aggregatedValues = new double[filterNumber][];
            DataContext = this;
            
        }

        void SpectrumAnalyser_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLeds();
            CalculateXScale();
        }

        private void UpdateLeds()
        {
            int ledSize;
            int ledLeftPos;
            int ledTopPos;
            if (redLedCanvas.ActualWidth > redLedCanvas.ActualHeight)
            {
                ledSize = (int)redLedCanvas.ActualHeight / 2;
                ledLeftPos = (int)redLedCanvas.ActualWidth / 2 - ledSize/2;
                ledTopPos = (int)redLedCanvas.ActualHeight / 4;
            }
            else
            {
                ledSize = (int)redLedCanvas.ActualWidth / 2;
                ledLeftPos = (int)redLedCanvas.ActualWidth / 4;
                ledTopPos = (int)redLedCanvas.ActualHeight / 2 - ledSize/2;
            }
            redLed.Height = ledSize;
            redLed.Width = ledSize;
            redLed.Margin = new Thickness(ledLeftPos, ledTopPos, 0, 0);

            greenLed.Height = ledSize;
            greenLed.Width = ledSize;
            greenLed.Margin = new Thickness(ledLeftPos, ledTopPos, 0, 0);

            blueLed.Height = ledSize;
            blueLed.Width = ledSize;
            blueLed.Margin = new Thickness(ledLeftPos, ledTopPos, 0, 0);
        }

        private void CalculateXScale()
        {
            this.xScale = analyserCanvas.ActualWidth / bins;
            addAxis();
        }

        int []agregatorCounter = {0,0,0};
        bool started = false;

        Stopwatch stopwatch = new Stopwatch();

        public void Update(Complex[] fftResults, int indexFilter)
        {

            if (fftResults.Length / 2 != bins)
            {
                this.bins = fftResults.Length / 2;
                CalculateXScale();
            }

            double[] values = new double[fftResults.Length / 2];

            if (started == false)
            {
                for (int i = 0; i < filterNumber; i++)
                    aggregatedValues[i] = new double[values.Length];
                    
                stopwatch.Start();
                started = true;
            }
                
            for (int n = 0; n < values.Length; n++)
            {
                double averageValueDB = getAmplitude(fftResults[n]);

                values[n] = averageValueDB;
            }

            for (int n = 0; n < values.Length; n++)
            {
                aggregatedValues[indexFilter][n] += values[n];
            }

            agregatorCounter[indexFilter]++;

            if (stopwatch.ElapsedMilliseconds > 50)
            {
                for (int i = 0; i < filterNumber; i++) {
                    for (int n = 0; n < values.Length; n++)
                    {
                        aggregatedValues[i][n] /= (agregatorCounter[i]);
                    }

                    for (int n = 0; n < values.Length; n++)
                    {
                        AddResult(n, GetYPosLog(aggregatedValues[i][n]), i);
                        aggregatedValues[i][n] = 0;
                    }
                }

                stopwatch.Stop();
                stopwatch.Reset();
                stopwatch.Start();

                agregatorCounter[0] = 0;
                agregatorCounter[1] = 0;
                agregatorCounter[2] = 0;
            }
        }

        private double getAmplitude(Complex c)
        {
            double intensityDB = Math.Sqrt(c.X * c.X + c.Y * c.Y);

            return intensityDB;
        }

        private double GetYPosLog(double intensityDB)
        {
            double minDB = 0.01;
            if (intensityDB > minDB)
                intensityDB = minDB;
            
            double percent = 1 - intensityDB / minDB;

            double yPos = percent * analyserCanvas.ActualHeight;

            return yPos;
        }

        private void AddResult(int index, double power, int indexFilter)
        {
            double xPos = CalculateXPos(index);
            Point p = new Point(CalculateXPos(index), power);
            if (indexFilter == 0)
            {
                if (index >= polyline1.Points.Count)
                {
                        polyline1.Points.Add(p);
                }
                else
                {
                        polyline1.Points[index] = p;
                }
            }

            if (indexFilter == 1)
            {
                if (index >= polyline2.Points.Count)
                {
                    polyline2.Points.Add(p);
                }
                else
                {
                    polyline2.Points[index] = p;
                }
            }

            if (indexFilter == 2)
            {
                if (index >= polyline3.Points.Count)
                {
                    polyline3.Points.Add(p);
                }
                else
                {
                    polyline3.Points[index] = p;
                }
            }
            
        }

        private double CalculateXPos(int bin)
        {
            if (bin == 0) return 0;
            return bin * xScale; // Math.Log10(bin) * xScale;
        }

        private void addAxis()
        {
            axisPanel.Children.Clear();
            axisPanel2.Children.Clear();
            axisPanel3.Children.Clear();

            if (this.ActualWidth == 0)
                return;

            int axisPoints = (int)analyserCanvas.ActualWidth / 70;

            if (axisPoints == 0)
                return;

            int ratio = (int)2048 / axisPoints;

            for (int i = 0; i < axisPoints; i++)
            {
                TextBlock frequency = new TextBlock();
                TextBlock frequency2 = new TextBlock();
                TextBlock frequency3 = new TextBlock();
                double freqValue = ratio * 11 * i / 1000; //jedna probka to 21 Hz
                frequency.Text = freqValue.ToString() + "kHz";
                frequency2.Text = freqValue.ToString() + "kHz";
                frequency3.Text = freqValue.ToString() + "kHz";
                if (i != 0)
                {
                    double leftMarginPos = (analyserCanvas.ActualWidth / axisPoints) - 25;
                    frequency.Margin = new Thickness(leftMarginPos, 0, 0, 0);
                    frequency2.Margin = new Thickness(leftMarginPos, 0, 0, 0);
                    frequency3.Margin = new Thickness(leftMarginPos, 0, 0, 0);
                }
                
                axisPanel.Children.Add(frequency);
                axisPanel2.Children.Add(frequency2);
                axisPanel3.Children.Add(frequency3);
            }
        }
    }
}
