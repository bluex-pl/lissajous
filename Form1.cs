using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml.Serialization;

namespace Lissajous {
    public partial class Form1 : Form {
        int imgCounter = 1;
        string imgPrefix = "img_";
        Settings settings;
        bool paused = false;
        int count = 0;
        Point[] springTemplate = {
            new Point(0, 10),
            new Point(6, 0),
            new Point(4, -10),
            new Point(2, 0),

            new Point(0, 0),
            new Point(6, 0),
            new Point(86, 10),
            new Point(94, 0),
            new Point(100, 0),
        };
        PointF[] spring = new PointF[53];
        PointF[] points = new PointF[53];

        public Form1() {
            InitializeComponent();
            settings = new Settings();
            settings.chart = chart1;
            settings.timer = timer1;
            propertyGrid1.SelectedObject = settings;
            prepareChart();
            prepareSpring();
        }

        private void keepAspect() {
            ChartArea area = chart1.ChartAreas[0];
            Size size = chart1.Size;
            float ratio = (float)size.Width / (float)size.Height;
            if(ratio < 1) {
                area.Position.Width = 96;
                area.Position.Height = ratio * 96;
            }
            else {
                area.Position.Width = 96 / ratio;
                area.Position.Height = 96;
            }
            area.Position.X = (96 - area.Position.Width) / 2 + 2;
            area.Position.Y = (96 - area.Position.Height) / 2 + 2;
        }

        private void prepareSpring() {
            for(int i = 0, j, dX; i < 53; i++) {
                dX = 0;
                if(i < 2)
                    j = i + 4;
                else if(i >= 50)
                    j = i - 44;
                else {
                    j = (i - 2) % 4;
                    dX = (i - 2) / 4 * 6 + 12;
                }
                spring[i].X = springTemplate[j].X + dX;
                spring[i].Y = springTemplate[j].Y;
            }
        }

        private void prepareChart() {
            Axis X = chart1.ChartAreas[0].Axes[0];
            Axis Y = chart1.ChartAreas[0].Axes[1];
            double magnitude = Math.Max(settings.XMagnitude, settings.YMagnitude);

            X.MinorTickMark.Interval = magnitude / 10.0;
            X.MajorTickMark.Interval = magnitude / 2.0;
            X.MajorTickMark.IntervalOffset = X.MinorTickMark.Interval * settings.marginSize;
            X.MajorGrid.Interval = X.MajorTickMark.Interval;
            X.MajorGrid.IntervalOffset = X.MajorTickMark.IntervalOffset;
            X.LabelStyle.Interval = X.MajorTickMark.Interval;
            X.LabelStyle.IntervalOffset = X.MinorTickMark.Interval * settings.marginSize;
            X.Maximum = magnitude + X.MajorGrid.IntervalOffset;
            X.Minimum = -X.Maximum;

            Y.MinorTickMark.Interval = magnitude / 10.0;
            Y.MajorTickMark.Interval = magnitude / 2.0;
            Y.MajorTickMark.IntervalOffset = Y.MinorTickMark.Interval * settings.marginSize;
            Y.MajorGrid.Interval = Y.MajorTickMark.Interval;
            Y.MajorGrid.IntervalOffset = Y.MajorTickMark.IntervalOffset;
            Y.LabelStyle.Interval = Y.MajorTickMark.Interval;
            Y.LabelStyle.IntervalOffset = Y.MinorTickMark.Interval * settings.marginSize;
            Y.Maximum = magnitude + Y.MajorGrid.IntervalOffset;
            Y.Minimum = -Y.Maximum;
            keepAspect();
        }

        private PointF[] drawSpring(double X, double Y, double dX, double dY, Axis aX, Axis aY) {
            // convert coordinate system to -1..1 from center of spring
            X = (X - dX) / settings.XMagnitude;
            Y = (Y - dY) / settings.YMagnitude;
            
            double scale = Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));
            double angle = Math.Atan2(Y, X) * 180 / Math.PI;
            double scaleX = (aX.ValueToPixelPosition(settings.XMagnitude) - aX.ValueToPixelPosition(0));
            double scaleY = (aY.ValueToPixelPosition(settings.YMagnitude) - aY.ValueToPixelPosition(0));
            Matrix mx = new Matrix();
            mx.Scale((float)scale / 100f, 0.01f, MatrixOrder.Append);
            mx.Rotate((float)angle, MatrixOrder.Append);
            mx.Scale((float)scaleX, (float)scaleY, MatrixOrder.Append);
            mx.Translate((float)aX.ValueToPixelPosition(dX), (float)aY.ValueToPixelPosition(dY), MatrixOrder.Append);
            spring.CopyTo(points, 0);
            mx.TransformPoints(points);
            return points;
        }

        private void animationStep() {
            double X, Y;
            double currentTime = count * settings.SimulationInterval / 1000f;
            count++;
            toolStripStatusLabel1.Text = String.Format("time: {0:F} step: {1}", currentTime, count);
            X = settings.XMagnitude *
                Math.Pow(Math.E, -settings.XDamping * currentTime) *
                Math.Sin(settings.XFrequency * currentTime + Math.PI * settings.PhaseDifference);
            Y = settings.YMagnitude *
                Math.Pow(Math.E, -settings.YDamping * currentTime) *
                Math.Sin(settings.YFrequency * currentTime);
            chart1.Series[0].Points.AddXY(X, Y);
            if(settings.LineLength > 0) {
                while(chart1.Series[0].Points.Count > settings.LineLength) {
                    chart1.Series[0].Points.RemoveAt(0);
                }
            }
        }

        private void chart1_PostPaint(object sender, ChartPaintEventArgs e) {
            Chart chart = sender as Chart;
            if(settings.ShowSprings && e.ChartElement.GetType() == typeof(ChartArea) && chart.Series[0].Points.Count > 0) {
                Graphics graph = e.ChartGraphics.Graphics;
                SolidBrush brush = new SolidBrush(chart.Series[1].Color);
                Pen pen = new Pen(brush, chart.Series[1].BorderWidth);
                Axis aX = chart1.ChartAreas[0].Axes[0];
                Axis aY = chart1.ChartAreas[0].Axes[1];
                double X = chart.Series[0].Points.Last().XValue;
                double Y = chart.Series[0].Points.Last().YValues.First();
                graph.FillEllipse(brush,
                    (float)aX.ValueToPixelPosition(X) - 5,
                    (float)aY.ValueToPixelPosition(Y) - 5,
                    10, 10);

                graph.DrawCurve(pen, drawSpring(X, Y, aX.Minimum + aX.MinorTickMark.Interval, 0, aX, aY), 0.8f);
                graph.FillEllipse(brush, points[0].X - 3, points[0].Y - 3, 6, 6);
                graph.DrawCurve(pen, drawSpring(X, Y, aX.Maximum - aX.MinorTickMark.Interval, 0, aX, aY), 0.8f);
                graph.FillEllipse(brush, points[0].X - 3, points[0].Y - 3, 6, 6);
                graph.DrawCurve(pen, drawSpring(X, Y, 0, aY.Minimum + aY.MinorTickMark.Interval, aX, aY), 0.8f);
                graph.FillEllipse(brush, points[0].X - 3, points[0].Y - 3, 6, 6);
                graph.DrawCurve(pen, drawSpring(X, Y, 0, aY.Maximum - aY.MinorTickMark.Interval, aX, aY), 0.8f);
                graph.FillEllipse(brush, points[0].X - 3, points[0].Y - 3, 6, 6);
            }
        }

        private void startButton_Click(object sender, EventArgs e) {
            if(!paused) {
                chart1.Series[0].Points.Clear();
                prepareChart();
                count = 0;
            }
            paused = false;

            timer1.Enabled = true;
            toolStripProgressBar1.Visible = true;
            pauseButton.Enabled = stopButton.Enabled = true;
            startButton.Enabled = forwardButton.Enabled = rewindButton.Enabled = false;
        }

        private void pauseButton_Click(object sender, EventArgs e) {
            timer1.Enabled = false;
            toolStripProgressBar1.Visible = false;
            startButton.Enabled = forwardButton.Enabled = rewindButton.Enabled = true;
            pauseButton.Enabled = false;
            paused = true;
        }

        private void stopButton_Click(object sender, EventArgs e) {
            timer1.Enabled = false;
            toolStripProgressBar1.Visible = false;
            pauseButton.Enabled = stopButton.Enabled = false;
            startButton.Enabled = forwardButton.Enabled = true;
            rewindButton.Enabled = chart1.Series[0].Points.Count > 0;
            paused = false;
            toolStripStatusLabel1.Text = "";
        }

        private void rewindButton_Click(object sender, EventArgs e) {
            int skip = Convert.ToInt32(skipTextBox.Text);
            if(skip >= chart1.Series[0].Points.Count) {
                chart1.Series[0].Points.Clear();
                rewindButton.Enabled = false;
                count = 0;
            }
            else {
                for(int i = 0; i < skip; i++) {
                    chart1.Series[0].Points.RemoveAt(chart1.Series[0].Points.Count - 1);
                }
                count -= skip;
            }
        }

        private void forwardButton_Click(object sender, EventArgs e) {
            int skip = Convert.ToInt32(skipTextBox.Text);
            if(!paused) {
                chart1.Series[0].Points.Clear();
                prepareChart();
                count = 0;
            }
            toolStripProgressBar1.Visible = true;
            for(int i = 0; i < skip; i++) {
                animationStep();
            }
            toolStripProgressBar1.Visible = false;
            stopButton.Enabled = rewindButton.Enabled = true;
            paused = true;
        }

        private void skipTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e) {
            var textBox = sender as ToolStripTextBox;
            int value;
            try {
                value = Convert.ToInt32(textBox.Text);
                if(value > 0)
                    return;
            }
            catch { }
            e.Cancel = true;
            textBox.Select(0, textBox.Text.Length);
        }

        private void settingsButton_Click(object sender, EventArgs e) {
            splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
        }

        private void timer1_Tick(object sender, EventArgs e) {
            animationStep();
        }

        private void saveImgButton_Click(object sender, EventArgs e) {
            int counter;
            string currentDirectory = Directory.GetCurrentDirectory();
            string fileName = String.Format(imgPrefix + "{0}.png", imgCounter);
            string path = Path.Combine(currentDirectory, fileName);
            for(counter = 0; File.Exists(path) && counter < 10; counter++) {
                string last = Directory.GetFiles(currentDirectory, imgPrefix + "*.png").
                    Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur); // find the longest string
                last = Path.GetFileNameWithoutExtension(last);
                try {
                    imgCounter = Convert.ToInt32(last.Substring(4)) + 1;
                }
                catch {
                    imgPrefix = last + "_";
                    imgCounter = 1;
                }
                fileName = String.Format(imgPrefix + "{0}.png", imgCounter);
                path = Path.Combine(currentDirectory, fileName);
            }
            if(counter < 10) {
                chart1.SaveImage(path, ImageFormat.Png);
                toolStripStatusLabel1.Text = String.Format("Image \"{0}\" saved", fileName);
                imgCounter++;
            }
            else
                toolStripStatusLabel1.Text = "Can't save image";
        }

        private void aboutButton_Click(object sender, EventArgs e) {
            new Info().ShowDialog();
        }

        private void saveButton_Click(object sender, EventArgs e) {
            Stream stream;
            if(saveFileDialog1.ShowDialog() == DialogResult.OK) {
                if((stream = saveFileDialog1.OpenFile()) != null) {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(Settings));
                    try {
                        serializer.WriteObject(stream, settings);
                    }
                    finally {
                        stream.Close();
                    }
                }
            }
        }

        private void loadButton_Click(object sender, EventArgs e) {
            Stream stream;
            if(openFileDialog1.ShowDialog() == DialogResult.OK) {
                if((stream = openFileDialog1.OpenFile()) != null) {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(Settings));
                    try {
                        settings.copyFrom(serializer.ReadObject(stream) as Settings);
                        propertyGrid1.Refresh();
                    }
                    catch (Exception ex){
                        //toolStripStatusLabel1.Text = String.Format("Error while loading settings: {0}", ex.Message);
                        toolStripStatusLabel1.Text = "Error while loading settings";
                    }
                    finally {
                        stream.Close();
                    }
                }
            }
        }

        private void chart1_Resize(object sender, EventArgs e) {
            keepAspect();
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
            prepareChart();
        }
    }
}
