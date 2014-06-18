using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Runtime.Serialization;

namespace Lissajous {
    public enum RestrictedSeriesChartType {
        FastLine = SeriesChartType.FastLine,
        Line = SeriesChartType.Line,
        Spline = SeriesChartType.Spline
    }

    [DataContract(Name = "Settings")]
    public class Settings : IExtensibleDataObject {
        [Browsable(false)]
        public Chart chart;
        [Browsable(false)]
        public Timer timer;
        [Browsable(false)]
        public int marginSize;

        public Settings() {
            chart = null;
            timer = null;
            marginSize = 1;
            XFrequency = 3.0;
            YFrequency = 4.0;
            XMagnitude = YMagnitude = 1.0;
            //AnimationInterval = 50;
            SimulationInterval = 50;
        }

        /**** Simulation ****/
        private int simulationInterval;
        [Category("Simulation"), DefaultValue(50)]
        [Description("Interval between simulation steps (ms)")]
        public int SimulationInterval {
            get {
                return simulationInterval;
            }
            set {
                if(value > 0)
                    simulationInterval = value;
                else
                    throw new ArgumentException("The value should be positive");
            }
        }
        [Category("Simulation"), DefaultValue(3.0), DataMember]
        [Description("Frequency of vibration in the x-axis")]
        public double XFrequency { get; set; }
        [Category("Simulation"), DefaultValue(1.0), DataMember]
        [Description("Magnitude of vibration in the x-axis")]
        public double XMagnitude { get; set; }
        [Category("Simulation"), DefaultValue(0.0), DataMember]
        [Description("Damping of vibration in the x-axis")]
        public double XDamping { get; set; }
        [Category("Simulation"), DefaultValue(4.0), DataMember]
        [Description("Frequency of vibration in the y-axis")]
        public double YFrequency { get; set; }
        [Category("Simulation"), DefaultValue(1.0), DataMember]
        [Description("Magnitude of vibration in the y-axis")]
        public double YMagnitude { get; set; }
        [Category("Simulation"), DefaultValue(0.0), DataMember]
        [Description("Damping of vibration in the y-axis")]
        public double YDamping { get; set; }
        [Category("Simulation"), DefaultValue(0.0), DataMember]
        [Description("Phase difference of vibrations (as a multiple of pi)")]
        public double PhaseDifference { get; set; }
        
        /**** Animation ****/
        [Category("Animation"), DefaultValue(50)]
        [Description("Interval between animation frames (ms)")]
        public int AnimationInterval {
            get {
                return timer.Interval;
            }
            set {
                if(value > 0)
                    timer.Interval = value;
                else
                    throw new ArgumentException("The value should be positive");
            }
        }
        [Category("Animation")]
        [Description("Line color")]
        public Color LineColor {
            get {
                return chart.Series[0].Color;
            }
            set {
                chart.Series[0].Color = value;
            }
        }
        [Category("Animation"), DefaultValue(0)]
        [Description("Number of stored animation steps (0 - unlimited)")]
        public int LineLength { get; set; }
        [Category("Animation"), DefaultValue(RestrictedSeriesChartType.Spline)]
        [Description("Line quality: FastLine (speed), Line, Spline (quality)")]
        public RestrictedSeriesChartType LineQuality {
            get {
                return (RestrictedSeriesChartType)chart.Series[0].ChartType;
            }
            set {
                chart.Series[0].ChartType = (SeriesChartType)value;
            }
        }
        [Category("Animation"), DefaultValue(2)]
        [Description("Line width")]
        public int LineWidth {
            get {
                return chart.Series[0].BorderWidth;
            }
            set {
                chart.Series[0].BorderWidth = value;
            }
        }
        [Category("Animation"), DefaultValue(true)]
        [Description("Show axes")]
        public bool ShowAxes {
            get {
                return chart.ChartAreas[0].Axes[0].Enabled != AxisEnabled.False;
            } 
            set {
                AxisEnabled enabled = value ? AxisEnabled.Auto : AxisEnabled.False;
                chart.ChartAreas[0].Axes[0].Enabled = chart.ChartAreas[0].Axes[1].Enabled = enabled;
            }
        }
        [Category("Animation"), DefaultValue(true)]
        [Description("Show grid lines")]
        public bool ShowGrid {
            get {
                return chart.ChartAreas[0].Axes[0].MajorGrid.Enabled;
            }
            set {
                chart.ChartAreas[0].Axes[0].MajorGrid.Enabled = chart.ChartAreas[0].Axes[1].MajorGrid.Enabled = value;
            }
        }
        [Category("Animation"), DefaultValue(false)]
        [Description("Show labels")]
        public bool ShowLabels {
            get {
                return chart.ChartAreas[0].Axes[0].LabelStyle.Enabled;
            }
            set {
                chart.ChartAreas[0].Axes[0].LabelStyle.Enabled = chart.ChartAreas[0].Axes[1].LabelStyle.Enabled = value;
            }
        }
        [Category("Animation"), DefaultValue(true)]
        [Description("Show major tick marks")]
        public bool ShowMajorTicks {
            get {
                return chart.ChartAreas[0].Axes[0].MajorTickMark.Enabled;
            }
            set {
                chart.ChartAreas[0].Axes[0].MajorTickMark.Enabled = chart.ChartAreas[0].Axes[1].MajorTickMark.Enabled = value;
            }
        }
        [Category("Animation"), DefaultValue(false)]
        [Description("Show minor tick marks")]
        public bool ShowMinorTicks {
            get {
                return chart.ChartAreas[0].Axes[0].MinorTickMark.Enabled;
            }
            set {
                TickMarkStyle style = value ? TickMarkStyle.InsideArea : TickMarkStyle.OutsideArea;
                chart.ChartAreas[0].Axes[0].MajorTickMark.TickMarkStyle = chart.ChartAreas[0].Axes[1].MajorTickMark.TickMarkStyle = style;
                chart.ChartAreas[0].Axes[0].MinorTickMark.Enabled = chart.ChartAreas[0].Axes[1].MinorTickMark.Enabled = value;
            }
        }
        private bool showSprings = false;
        [Category("Animation"), DefaultValue(false)]
        [Description("Show springs")]
        public bool ShowSprings {
            get {
                return showSprings;
            }
            set {
                showSprings = value;
                chart.Refresh();
            }
        }
        [Category("Animation")]
        [Description("Spring color")]
        public Color SpringColor
        {
            get
            {
                return chart.Series[1].Color;
            }
            set
            {
                chart.Series[1].Color = chart.Series[2].Color = chart.Series[3].Color = chart.Series[4].Color = value;
            }
        }
        [Category("Animation"), DefaultValue(2)]
        [Description("Spring line width")]
        public int SpringLineWidth {
            get {
                return chart.Series[1].BorderWidth;
            }
            set {
                chart.Series[1].BorderWidth = value;
            }
        }

        private ExtensionDataObject extensionDataObjectValue;
        [Browsable(false)]
        public ExtensionDataObject ExtensionData {
            get {
                return extensionDataObjectValue;
            }
            set {
                extensionDataObjectValue = value;
            }
        }

        public void copyFrom(Settings settings) {
            XDamping = settings.XDamping;
            XFrequency = settings.XFrequency;
            XMagnitude = settings.XMagnitude;
            YDamping = settings.YDamping;
            YFrequency = settings.YFrequency;
            YMagnitude = settings.YMagnitude;
            PhaseDifference = settings.PhaseDifference;
        }
    }
}
