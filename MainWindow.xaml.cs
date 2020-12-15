using System.Windows;
using System.Windows.Media;
using System.Drawing;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;
using Brushes = System.Windows.Media.Brushes;
using System.IO;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Drawing.Imaging;
using System.Windows.Shapes;
using System.Collections.Generic;
using Image = System.Drawing.Image;
using System;
using System.Windows.Controls;
using System.Windows.Forms;

namespace RGB_analysis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Polyline r_polyline_x = new Polyline(); Polyline r_polyline_y = new Polyline(); 
        Polyline g_polyline_x = new Polyline(); Polyline g_polyline_y = new Polyline();
        Polyline b_polyline_x = new Polyline(); Polyline b_polyline_y = new Polyline();
        Path geometryPath_x = new Path();
        Path geometryPath_y = new Path();

        Dictionary<int, int> colorIncidence = new Dictionary<int, int>();
        (double, double) previous_mouse;
        (double, double) previous_mouse_graph;
        int colour_mode = 0; // 0 = RGB, 1 = Grey scale
        int scalor = 4;
        bool scaled = true;
        (int, int) colors_shown = (4, 2);
        FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap();
        BitmapSource bitmapSource;

        public MainWindow()
        {
            InitializeComponent();
            initImage(@"C:\Users\dyerc\OneDrive - ZESPRI\Pictures\Kiwi Pictures\kiwi1.jpg");

            for (int x = 0; x < colors_shown.Item1; x++)
            {
                colors.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int y = 0; y < colors_shown.Item2; y++)
            {
                colors.RowDefinitions.Add(new RowDefinition());
                for (int x = 0; x < colors_shown.Item1; x++)
                {
                    Canvas canvas = new Canvas();
                    Grid.SetRow(canvas, y);
                    Grid.SetColumn(canvas, x);
                    colors.Children.Add(canvas);
                }
            }

            init_graph();
        }

        private BitmapSource CreateBitmapSourceFromGdiBitmap(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bmpSrc = BitmapSource.Create(bitmap.Width, bitmap.Height, bitmap.HorizontalResolution,
                                           bitmap.VerticalResolution, PixelFormats.Bgra32, null, bitmapData.Scan0, 
                                           bitmap.Width * bitmap.Height * 4, bitmapData.Stride); 
            bitmap.UnlockBits(bitmapData);
            return bmpSrc;
        }

        private void initImage(string fileLoc)
        {
            Stream jpgStream = File.Open(fileLoc, FileMode.Open);
            Image image = Image.FromStream(jpgStream);
            Bitmap bitmap = new Bitmap(image);
            bitmapSource = CreateBitmapSourceFromGdiBitmap(bitmap);

            // Gray
            FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap();
            formatConvertedBitmap.BeginInit();
            formatConvertedBitmap.Source = bitmapSource;
            formatConvertedBitmap.DestinationFormat = PixelFormats.Gray16;
            formatConvertedBitmap.EndInit();

            grayBitmap = formatConvertedBitmap;

            // Set defualt image
            img.Source = bitmapSource;
        }

        private void redraw_elements()
        {
            Bitmap bitmap = CaptureFromScreen();
            GraphFromBitmapData(bitmap);
        }

        public Bitmap CaptureFromScreen()
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)((Left * 1.25) + 10), (int)((Top * 1.25) + 38), (int)(imgBox.Width + 48), (int)(imgBox.Height + 33));
            Bitmap bmpScreenCapture = new Bitmap(rect.Width, rect.Height);
            Graphics p = Graphics.FromImage(bmpScreenCapture);
            p.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            p.Dispose();
            return bmpScreenCapture;
        }

        private void Populate(double[] arr, double value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }

        private void GraphFromBitmapData(Bitmap bitmap)
        {
            colorIncidence.Clear();
            r_polyline_x.Points.Clear(); r_polyline_y.Points.Clear();
            g_polyline_x.Points.Clear(); g_polyline_y.Points.Clear();
            b_polyline_x.Points.Clear(); b_polyline_y.Points.Clear();

            double[] r_vals_x = new double[bitmap.Width / scalor]; double[] r_vals_y = new double[bitmap.Height / scalor];
            double[] g_vals_x = new double[bitmap.Width / scalor]; double[] g_vals_y = new double[bitmap.Height / scalor];
            double[] b_vals_x = new double[bitmap.Width / scalor]; double[] b_vals_y = new double[bitmap.Height / scalor];

            for (int x = 0; x < bitmap.Width / scalor; x++)
            {
                for (int y = 0; y < bitmap.Height / scalor; y++)
                {
                    System.Drawing.Color pixelColor = bitmap.GetPixel(x * scalor, y * scalor);
                    r_vals_x[x] += (double)pixelColor.R / (double)255; // Do not remove cast, IDE is lying
                    g_vals_x[x] += (double)pixelColor.G / (double)255;
                    b_vals_x[x] += (double)pixelColor.B / (double)255;
                    r_vals_y[y] += (double)pixelColor.R / (double)255;
                    g_vals_y[y] += (double)pixelColor.G / (double)255;
                    b_vals_y[y] += (double)pixelColor.B / (double)255;
                    if (colour_mode != 1)
                    {
                        if (colorIncidence.Keys.Contains(pixelColor.ToArgb()))
                            colorIncidence[pixelColor.ToArgb()]++;
                        else
                            colorIncidence.Add(pixelColor.ToArgb(), 1);
                    }
                }
            }

            if (colour_mode != 1)
            {
                List<System.Drawing.Color> mostUsedColors = new List<System.Drawing.Color>();
                colorIncidence = colorIncidence.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                for (int i = 0; i < colors_shown.Item1 * colors_shown.Item2; i++)
                {
                    if (colorIncidence.Count > colors_shown.Item1 * colors_shown.Item2)
                        mostUsedColors.Add(System.Drawing.Color.FromArgb(colorIncidence.ElementAt(i).Key));
                    else
                        mostUsedColors.Add(System.Drawing.Color.FromArgb(240, 240, 240));
                }
                if (colors != null)
                {
                    SolidColorBrush n_color;
                    int i = 0;
                    foreach (Canvas child in colors.Children)
                    {
                        n_color = new SolidColorBrush(System.Windows.Media.Color.FromRgb(mostUsedColors[i].R, mostUsedColors[i].G, mostUsedColors[i].B));
                        child.Background = n_color;
                        i++;
                    }
                }
            }

            double[] min_x = new double[3];
            double[] scale_x = new double[3];
            if (scaled)
            {
                Populate(min_x, Math.Min(r_vals_x.Min(), Math.Min(b_vals_x.Min(), g_vals_x.Min())));
                Populate(scale_x, y_graph.Height / (Math.Max(r_vals_x.Max(), Math.Max(b_vals_x.Max(), g_vals_x.Max())) - min_x[0]));
            }
            else
            {
                min_x[0] = r_vals_x.Min(); min_x[1] = g_vals_x.Min(); min_x[2] = b_vals_x.Min();
                scale_x[0] = y_graph.Height / (r_vals_x.Max() - min_x[0]); 
                scale_x[1] = y_graph.Height / (g_vals_x.Max() - min_x[1]); 
                scale_x[2] = y_graph.Height / (b_vals_x.Max() - min_x[2]);
            }

            for (int i = 0; i < bitmap.Width / scalor; i++)
            {
                if (colour_mode == 1)
                {
                    r_polyline_x.Points.Add(new Point(y_graph.Width / bitmap.Width * i * scalor, y_graph.Height - (r_vals_x[i] - min_x[0]) * scale_x[0]));
                }
                else
                {
                    r_polyline_x.Points.Add(new Point(y_graph.Width / bitmap.Width * i * scalor, y_graph.Height - (r_vals_x[i] - min_x[0]) * scale_x[0]));
                    g_polyline_x.Points.Add(new Point(y_graph.Width / bitmap.Width * i * scalor, y_graph.Height - (g_vals_x[i] - min_x[1]) * scale_x[1]));
                    b_polyline_x.Points.Add(new Point(y_graph.Width / bitmap.Width * i * scalor, y_graph.Height - (b_vals_x[i] - min_x[2]) * scale_x[2]));
                }
            }

            double[] min_y = new double[3];
            double[] scale_y = new double[3];
            if (scaled)
            {
                Populate(min_y, Math.Min(r_vals_y.Min(), Math.Min(b_vals_y.Min(), g_vals_y.Min())));
                Populate(scale_y, x_graph.Width / (Math.Max(r_vals_y.Max(), Math.Max(b_vals_y.Max(), g_vals_y.Max())) - min_y[0]));
            }
            else
            {
                min_y[0] = r_vals_y.Min(); min_y[1] = g_vals_y.Min(); min_y[2] = b_vals_y.Min();
                scale_y[0] = x_graph.Width / (r_vals_y.Max() - min_y[0]); 
                scale_y[1] = x_graph.Width / (g_vals_y.Max() - min_y[1]); 
                scale_y[2] = x_graph.Width / (b_vals_y.Max() - min_y[2]); 
            }

            for (int i = 0; i < bitmap.Height / scalor; i++)
            {
                if (colour_mode == 1)
                {
                    r_polyline_y.Points.Add(new Point((r_vals_y[i] - min_y[0]) * scale_y[0], x_graph.Height / bitmap.Height * i * scalor));
                }
                else
                {
                    r_polyline_y.Points.Add(new Point((r_vals_y[i] - min_y[0]) * scale_y[0], x_graph.Height / bitmap.Height * i * scalor));
                    g_polyline_y.Points.Add(new Point((g_vals_y[i] - min_y[1]) * scale_y[1], x_graph.Height / bitmap.Height * i * scalor));
                    b_polyline_y.Points.Add(new Point((b_vals_y[i] - min_y[2]) * scale_y[2], x_graph.Height / bitmap.Height * i * scalor));
                }
            }
        }

        private void resort_colors()
        {
            if (colour_mode != 1)
            {
                colors.Children.Clear();
                colors.ColumnDefinitions.Clear();
                colors.RowDefinitions.Clear();
                for (int x = 0; x < colors_shown.Item1; x++)
                {
                    colors.ColumnDefinitions.Add(new ColumnDefinition());
                }

                for (int y = 0; y < colors_shown.Item2; y++)
                {
                    colors.RowDefinitions.Add(new RowDefinition());
                    for (int x = 0; x < colors_shown.Item1; x++)
                    {
                        Canvas canvas = new Canvas();
                        Grid.SetRow(canvas, y);
                        Grid.SetColumn(canvas, x);
                        colors.Children.Add(canvas);
                    }
                }

                List<System.Drawing.Color> mostUsedColors = new List<System.Drawing.Color>();
                for (int i = 0; i < colors_shown.Item1 * colors_shown.Item2; i++)
                {
                    if (colorIncidence.Count != 1)
                    {
                        mostUsedColors.Add(System.Drawing.Color.FromArgb(colorIncidence.ElementAt(i).Key));
                    }
                    else
                        mostUsedColors.Add(System.Drawing.Color.FromArgb(0, 0, 0));
                }
                SolidColorBrush n_color;
                int j = 0;
                foreach (Canvas child in colors.Children)
                {
                    n_color = new SolidColorBrush(System.Windows.Media.Color.FromRgb(mostUsedColors[j].R, mostUsedColors[j].G, mostUsedColors[j].B));
                    child.Background = n_color;
                    j++;
                }
            }
        }

        private void init_graph()
        {
            //GeometryGroup geometryGroup_x = new GeometryGroup();
            //GeometryGroup geometryGroup_y = new GeometryGroup();

            //geometryGroup_x.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, y_graph.Height)));
            //geometryGroup_x.Children.Add(new LineGeometry(new Point(0, y_graph.Height), new Point(y_graph.Width, y_graph.Height)));
            //geometryPath_x.Stroke = Brushes.Black;
            //geometryPath_x.StrokeThickness = 2;
            //geometryPath_x.Data = geometryGroup_x;

            //geometryGroup_y.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, x_graph.Height)));
            //geometryGroup_y.Children.Add(new LineGeometry(new Point(0, x_graph.Height), new Point(x_graph.Width, x_graph.Height)));
            //geometryPath_y.Stroke = Brushes.Black;
            //geometryPath_y.StrokeThickness = 2;
            //geometryPath_y.Data = geometryGroup_y;

            r_polyline_x.Stroke = Brushes.Red; r_polyline_y.Stroke = Brushes.Red;
            g_polyline_x.Stroke = Brushes.Green; g_polyline_y.Stroke = Brushes.Green;
            b_polyline_x.Stroke = Brushes.Blue; b_polyline_y.Stroke = Brushes.Blue;
            r_polyline_x.StrokeThickness = g_polyline_x.StrokeThickness = b_polyline_x.StrokeThickness = 1;
            r_polyline_y.StrokeThickness = g_polyline_y.StrokeThickness = b_polyline_y.StrokeThickness = 1;

            y_graph.Children.Add(geometryPath_x);
            y_graph.Children.Add(r_polyline_x);
            y_graph.Children.Add(g_polyline_x);
            y_graph.Children.Add(b_polyline_x);

            x_graph.Children.Add(geometryPath_y);
            x_graph.Children.Add(r_polyline_y);
            x_graph.Children.Add(g_polyline_y);
            x_graph.Children.Add(b_polyline_y);
        }

        private void imgBox_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                scale.ScaleX /= 1.1;
                scale.ScaleY /= 1.1;
            }
            else
            {
                scale.ScaleX *= 1.1;
                scale.ScaleY *= 1.1;
            }

            redraw_elements();
        }

        private void imgBox_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            previous_mouse = (e.GetPosition(imgBox).X, e.GetPosition(imgBox).Y);
            imgBox.CaptureMouse();
        }

        private void imgBox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (imgBox.IsMouseCaptured)
            {
                translate.X += -previous_mouse.Item1 + e.GetPosition(imgBox).X;
                translate.Y += -previous_mouse.Item2 + e.GetPosition(imgBox).Y;
                previous_mouse = (e.GetPosition(imgBox).X, e.GetPosition(imgBox).Y);
                redraw_elements();
            }
        }

        private void imgBox_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            imgBox.ReleaseMouseCapture();
        }

        private void scalor_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scalor = (int)e.NewValue;
            redraw_elements();
        }

        private void Right_Plus_Button_Click(object sender, RoutedEventArgs e)
        {
            colors_shown.Item1++;
            resort_colors();
        }

        private void Right_Minus_Button_Click(object sender, RoutedEventArgs e)
        {
            if (colors_shown.Item1 == 1 || colorIncidence.Count == 0)
                return;
            colors_shown.Item1--;
            resort_colors();
        }

        private void Bottom_Plus_Button_Click(object sender, RoutedEventArgs e)
        {
            colors_shown.Item2++;
            resort_colors();
        }

        private void Bottom_Minus_Button_Click(object sender, RoutedEventArgs e)
        {
            if (colors_shown.Item2 == 1 || colorIncidence.Count == 0)
                return;
            colors_shown.Item2--;
            resort_colors();
        }
        
        private void graph_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender == y_graph)
                previous_mouse_graph.Item1 = e.GetPosition(y_graph).X;
            else
                previous_mouse_graph.Item2 = e.GetPosition(x_graph).Y;
            ((Canvas)sender).CaptureMouse();
        }

        private void graph_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender == y_graph)
            {
                if (y_graph.IsMouseCaptured)
                {
                    translate.X += -previous_mouse_graph.Item1 + e.GetPosition(y_graph).X;
                    previous_mouse_graph.Item1 = e.GetPosition(y_graph).X;
                    redraw_elements();
                }
            }
            else
            {
                if (x_graph.IsMouseCaptured)
                {
                    translate.Y += -previous_mouse_graph.Item2 + e.GetPosition(x_graph).Y;
                    previous_mouse_graph.Item2 = e.GetPosition(x_graph).Y;
                    redraw_elements();
                }
            }
        }

        private void graph_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ((Canvas)sender).ReleaseMouseCapture();
        }

        private void ColourMode_Button_Click(object sender, RoutedEventArgs e)
        {
            colour_mode++;
            switch (colour_mode)
            {
                case 0:
                    img.Source = bitmapSource;
                    r_polyline_x.Stroke = Brushes.Red; r_polyline_y.Stroke = Brushes.Red;
                    g_polyline_x.Stroke = Brushes.Green; g_polyline_y.Stroke = Brushes.Green;
                    b_polyline_x.Stroke = Brushes.Blue; b_polyline_y.Stroke = Brushes.Blue;
                    break;
                case 1:
                    img.Source = grayBitmap;
                    r_polyline_x.Stroke = Brushes.Black; r_polyline_y.Stroke = Brushes.Black;
                    break;
                case 2:
                    img.Source = bitmapSource;
                    r_polyline_x.Stroke = Brushes.Red; r_polyline_y.Stroke = Brushes.Red;
                    g_polyline_x.Stroke = Brushes.Green; g_polyline_y.Stroke = Brushes.Green;
                    b_polyline_x.Stroke = Brushes.Blue; b_polyline_y.Stroke = Brushes.Blue;
                    colour_mode = 0;
                    break;
                default:
                    break;
            }
            redraw_elements();
            
        }

        private void OpenFile_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                initImage(openFileDialog.FileName);
                r_polyline_x.Stroke = Brushes.Red; r_polyline_y.Stroke = Brushes.Red;
                g_polyline_x.Stroke = Brushes.Green; g_polyline_y.Stroke = Brushes.Green;
                b_polyline_x.Stroke = Brushes.Blue; b_polyline_y.Stroke = Brushes.Blue;
                colour_mode = 0;
                redraw_elements();
            }
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.ToolBar toolBar = sender as System.Windows.Controls.ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }
        }

        private void Recenter_Button_Click(object sender, RoutedEventArgs e)
        {
            translate.X = 0; translate.Y = 0;
            redraw_elements();
        }

        private void Scaled_Button_Click(object sender, RoutedEventArgs e)
        {
            if (scaled)
                scaled = false;
            else
                scaled = true;
            redraw_elements();
        }
    }
}
