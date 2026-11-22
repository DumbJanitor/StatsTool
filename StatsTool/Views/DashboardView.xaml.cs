using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatsTool.Views
{
    public partial class DashboardView : UserControl
    {
        // =====================================================
        // DASHBOARD DATA
        // =====================================================

        private DashboardSnapshot _dashboardData;

        private readonly Brush _navyBrush =
            new SolidColorBrush(Color.FromRgb(11, 21, 48));

        private readonly Brush _tealBrush =
            new SolidColorBrush(Color.FromRgb(0, 169, 137));

        private readonly Brush _gridBrush =
            new SolidColorBrush(Color.FromRgb(226, 233, 241));

        public DashboardView()
        {
            InitializeComponent();

            // FRONTEND SAMPLE DATA:
            // Replace through UpdateDashboard(...) once
            // backend integration is available.
            _dashboardData = CreateSampleData();

            DataContext = _dashboardData;
        }

        // =====================================================
        // RESPONSIVE LAYOUT
        // =====================================================

        private void DashboardView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            UpdateLayoutMode(ActualWidth);
            DrawCharts();
        }

        private void DashboardView_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            UpdateLayoutMode(e.NewSize.Width);
        }

        private void UpdateLayoutMode(double availableWidth)
        {
            if (CardsPanel == null)
                return;

            // Wider screens: two columns.
            // Narrower screens: one column.
            //
            // FRONTEND NOTE:
            // Keep this responsive behavior when integrating
            // actual dashboard data.

            int requiredColumns = availableWidth >= 850 ? 2 : 1;

            if (CardsPanel.Columns != requiredColumns)
            {
                CardsPanel.Columns = requiredColumns;
            }
        }

        private void ChartCanvas_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            if (sender == TopProductsCanvas)
            {
                DrawTopProductsChart();
            }
            else if (sender == TrendCanvas)
            {
                DrawWeeklyTrendChart();
            }
        }

        // =====================================================
        // PUBLIC DATA INTEGRATION POINT
        // =====================================================

        public void UpdateDashboard(DashboardSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            // BACKEND TEAM:
            //
            // Call this method with the calculated dashboard
            // data once it has been retrieved.
            //
            // Ensure this method runs on the WPF UI thread.
            //
            // Expected data:
            // - WeekRange
            // - TotalUnits
            // - ProductCount
            // - SalesRows
            // - StockFlags
            // - WeeklyTrend
            //
            // Do not connect SQL directly inside this view.

            _dashboardData = snapshot;
            DataContext = _dashboardData;

            DrawCharts();
        }

        // =====================================================
        // SAMPLE DATA
        // =====================================================

        private static DashboardSnapshot CreateSampleData()
        {
            return new DashboardSnapshot
            {
                WeekRange = "Nov 16–22, 2026",

                TotalUnits = 284,

                ProductCount = 5,

                SalesRows = new List<DashboardSalesRow>
                {
                    new DashboardSalesRow
                    {
                        Product = "Bottled Water",
                        Units = 84
                    },

                    new DashboardSalesRow
                    {
                        Product = "Instant Noodles",
                        Units = 72
                    },

                    new DashboardSalesRow
                    {
                        Product = "Rice 5kg",
                        Units = 52
                    },

                    new DashboardSalesRow
                    {
                        Product = "Canned Tuna",
                        Units = 46
                    },

                    new DashboardSalesRow
                    {
                        Product = "Cooking Oil",
                        Units = 30
                    }
                },

                StockFlags = new List<DashboardStockFlag>
                {
                    new DashboardStockFlag
                    {
                        Product = "Rice 5kg",
                        CurrentStock = 18,
                        WeeklyAverage = 46
                    },

                    new DashboardStockFlag
                    {
                        Product = "Canned Tuna",
                        CurrentStock = 12,
                        WeeklyAverage = 32
                    }
                },

                WeeklyTrend = new List<DashboardWeeklyPoint>
                {
                    new DashboardWeeklyPoint
                    {
                        WeekLabel = "Oct 12",
                        Units = 213
                    },

                    new DashboardWeeklyPoint
                    {
                        WeekLabel = "Oct 19",
                        Units = 227
                    },

                    new DashboardWeeklyPoint
                    {
                        WeekLabel = "Oct 26",
                        Units = 245
                    },

                    new DashboardWeeklyPoint
                    {
                        WeekLabel = "Nov 2",
                        Units = 258
                    },

                    new DashboardWeeklyPoint
                    {
                        WeekLabel = "Nov 9",
                        Units = 270
                    },

                    new DashboardWeeklyPoint
                    {
                        WeekLabel = "Nov 16",
                        Units = 284
                    }
                }
            };
        }

        // =====================================================
        // CHART RENDERING
        // =====================================================

        private void DrawCharts()
        {
            DrawTopProductsChart();
            DrawWeeklyTrendChart();
        }

        // =====================================================
        // TOP SELLING PRODUCTS - HORIZONTAL BAR CHART
        // =====================================================

        private void DrawTopProductsChart()
        {
            if (TopProductsCanvas == null)
                return;

            TopProductsCanvas.Children.Clear();

            List<DashboardSalesRow> products =
                _dashboardData.SalesRows
                    .OrderByDescending(p => p.Units)
                    .Take(5)
                    .ToList();

            if (products.Count == 0)
                return;

            double chartWidth = TopProductsCanvas.ActualWidth;

            if (chartWidth < 180)
                return;

            int maximumUnits = Math.Max(
                1,
                products.Max(p => p.Units));

            double labelWidth = Math.Min(
                130,
                chartWidth * 0.34);

            double barStart = labelWidth + 10;

            double availableBarWidth = Math.Max(
                45,
                chartWidth - barStart - 38);

            double rowHeight = 40;
            double barHeight = 22;
            double topPadding = 8;

            // Vertical axis
            Line axis = new Line
            {
                X1 = barStart,
                X2 = barStart,
                Y1 = 4,
                Y2 = topPadding + products.Count * rowHeight,
                Stroke = _navyBrush,
                StrokeThickness = 1
            };

            TopProductsCanvas.Children.Add(axis);

            Color[] barColors =
            {
                Color.FromRgb(0, 112, 82),
                Color.FromRgb(0, 155, 130),
                Color.FromRgb(0, 169, 145),
                Color.FromRgb(38, 188, 160),
                Color.FromRgb(87, 207, 177)
            };

            for (int i = 0; i < products.Count; i++)
            {
                DashboardSalesRow product = products[i];

                double y = topPadding + i * rowHeight;

                double barWidth =
                    availableBarWidth *
                    product.Units /
                    maximumUnits;

                // Product label
                AddCanvasLabel(
                    TopProductsCanvas,
                    product.Product,
                    0,
                    y + 3,
                    labelWidth - 5,
                    12,
                    false,
                    TextAlignment.Right,
                    _navyBrush);

                // Bar
                Rectangle bar = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    RadiusX = 3,
                    RadiusY = 3,
                    Fill = new LinearGradientBrush(
                        barColors[i],
                        Color.FromRgb(105, 217, 186),
                        0)
                };

                Canvas.SetLeft(bar, barStart);
                Canvas.SetTop(bar, y);

                TopProductsCanvas.Children.Add(bar);

                // Units label
                AddCanvasLabel(
                    TopProductsCanvas,
                    product.Units.ToString(),
                    barStart + barWidth + 6,
                    y + 2,
                    34,
                    13,
                    true,
                    TextAlignment.Left,
                    _navyBrush);
            }
        }

        // =====================================================
        // WEEKLY SALES TREND - LINE CHART
        // =====================================================

        private void DrawWeeklyTrendChart()
        {
            if (TrendCanvas == null)
                return;

            TrendCanvas.Children.Clear();

            List<DashboardWeeklyPoint> points =
                _dashboardData.WeeklyTrend;

            if (points.Count == 0)
                return;

            double canvasWidth = TrendCanvas.ActualWidth;
            double canvasHeight = TrendCanvas.ActualHeight;

            if (canvasWidth < 180 || canvasHeight < 100)
                return;

            double left = 44;
            double right = 18;
            double top = 24;
            double bottom = 35;

            double plotWidth =
                canvasWidth - left - right;

            double plotHeight =
                canvasHeight - top - bottom;

            int lowestValue = points.Min(p => p.Units);
            int highestValue = points.Max(p => p.Units);

            int valueRange = Math.Max(
                1,
                highestValue - lowestValue);

            // Select a readable vertical axis interval.
            double step;

            if (valueRange <= 80)
                step = 20;
            else if (valueRange <= 250)
                step = 50;
            else if (valueRange <= 600)
                step = 100;
            else
                step = Math.Ceiling(
                    valueRange / 500.0) * 100;

            double minY = Math.Max(
                0,
                Math.Floor(
                    (lowestValue - 2 * step) / step) * step);

            double maxY = Math.Ceiling(
                (highestValue + step / 2) / step) * step;

            if (maxY <= minY)
                maxY = minY + step;

            // -----------------------------------------------
            // HORIZONTAL GRIDLINES AND Y-AXIS LABELS
            // -----------------------------------------------

            for (double tick = minY;
                 tick <= maxY + 0.001;
                 tick += step)
            {
                double y = top +
                    (maxY - tick) /
                    (maxY - minY) * plotHeight;

                Line gridline = new Line
                {
                    X1 = left,
                    X2 = left + plotWidth,
                    Y1 = y,
                    Y2 = y,
                    Stroke = _gridBrush,
                    StrokeThickness = 1
                };

                TrendCanvas.Children.Add(gridline);

                AddCanvasLabel(
                    TrendCanvas,
                    tick.ToString("0"),
                    0,
                    y - 8,
                    left - 8,
                    11,
                    false,
                    TextAlignment.Right,
                    _navyBrush);
            }

            // -----------------------------------------------
            // X-AXIS
            // -----------------------------------------------

            Line xAxis = new Line
            {
                X1 = left,
                X2 = left + plotWidth,
                Y1 = top + plotHeight,
                Y2 = top + plotHeight,
                Stroke = _navyBrush,
                StrokeThickness = 1
            };

            TrendCanvas.Children.Add(xAxis);

            // -----------------------------------------------
            // LINE POINTS
            // -----------------------------------------------

            PointCollection linePoints =
                new PointCollection();

            for (int i = 0; i < points.Count; i++)
            {
                DashboardWeeklyPoint week = points[i];

                double position = points.Count == 1
                    ? 0.5
                    : (double)i / (points.Count - 1);

                double x = left + plotWidth * position;

                double y = top +
                    (maxY - week.Units) /
                    (maxY - minY) * plotHeight;

                linePoints.Add(new Point(x, y));
            }

            Polyline trendLine = new Polyline
            {
                Points = linePoints,
                Stroke = _tealBrush,
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            };

            TrendCanvas.Children.Add(trendLine);

            // -----------------------------------------------
            // POINT MARKERS, VALUES, AND WEEK LABELS
            // -----------------------------------------------

            for (int i = 0; i < points.Count; i++)
            {
                DashboardWeeklyPoint week = points[i];
                Point location = linePoints[i];

                Ellipse marker = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Fill = Brushes.White,
                    Stroke = _tealBrush,
                    StrokeThickness = 2.5
                };

                Canvas.SetLeft(
                    marker,
                    location.X - 4.5);

                Canvas.SetTop(
                    marker,
                    location.Y - 4.5);

                TrendCanvas.Children.Add(marker);

                // Numeric value above the marker
                AddCanvasLabel(
                    TrendCanvas,
                    week.Units.ToString(),
                    location.X - 25,
                    location.Y - 26,
                    50,
                    11,
                    true,
                    TextAlignment.Center,
                    _navyBrush);

                // Week label beneath the graph
                AddCanvasLabel(
                    TrendCanvas,
                    week.WeekLabel,
                    location.X - 30,
                    top + plotHeight + 9,
                    60,
                    10,
                    false,
                    TextAlignment.Center,
                    _navyBrush);
            }
        }

        // =====================================================
        // SHARED CHART TEXT HELPER
        // =====================================================

        private static void AddCanvasLabel(
            Canvas canvas,
            string text,
            double x,
            double y,
            double width,
            double fontSize,
            bool isBold,
            TextAlignment alignment,
            Brush foreground)
        {
            TextBlock label = new TextBlock
            {
                Text = text,
                Width = width,
                FontSize = fontSize,
                FontWeight = isBold
                    ? FontWeights.Bold
                    : FontWeights.Normal,
                TextAlignment = alignment,
                Foreground = foreground
            };

            Canvas.SetLeft(label, x);
            Canvas.SetTop(label, y);

            canvas.Children.Add(label);
        }

        // =====================================================
        // COMPARE WEEKS NAVIGATION
        // =====================================================

        private void CompareWeeksButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // FRONTEND:
            // Opens the existing ComparisonsView.
            // MainWindow handles the sidebar selection.

            if (Window.GetWindow(this) is StatsTool.MainWindow mainWindow)
            {
                mainWindow.NavigateTo("Comparisons");
            }
        }
    }

    // =========================================================
    // DASHBOARD DATA CONTRACT
    // =========================================================
    //
    // BACKEND TEAM:
    //
    // Return a DashboardSnapshot containing all fields.
    //
    // Do not calculate business statistics in the UI.
    //
    // DATABASE TEAM:
    //
    // Coordinate the source records and aggregation rules:
    //
    // 1. Weekly sales:
    //    Sum recorded daily sold quantities for the week.
    //
    // 2. Products sold:
    //    Count distinct products with positive sales.
    //
    // 3. Top selling products:
    //    Rank products by units sold during the week.
    //
    // 4. Inventory flags:
    //    Identify products where current stock is lower
    //    than their historical average weekly units sold.
    //
    // 5. Weekly trend:
    //    Return total sold units grouped by reporting week.
    //
    // Coordinate the exact historical averaging period
    // with the approved research scope.
    //
    // Product IDs should be used for database relationships,
    // even though this dashboard displays product names.
    //
    // =========================================================

    public sealed class DashboardSnapshot
    {
        public string WeekRange { get; set; } = "";

        public int TotalUnits { get; set; }

        public int ProductCount { get; set; }

        public List<DashboardSalesRow> SalesRows { get; set; }
            = new List<DashboardSalesRow>();

        public List<DashboardStockFlag> StockFlags { get; set; }
            = new List<DashboardStockFlag>();

        public int FlaggedCount => StockFlags.Count;

        public List<DashboardWeeklyPoint> WeeklyTrend { get; set; }
            = new List<DashboardWeeklyPoint>();
    }

    public sealed class DashboardSalesRow
    {
        public string Product { get; set; } = "";

        public int Units { get; set; }
    }

    public sealed class DashboardStockFlag
    {
        public string Product { get; set; } = "";

        public int CurrentStock { get; set; }

        public double WeeklyAverage { get; set; }
    }

    public sealed class DashboardWeeklyPoint
    {
        public string WeekLabel { get; set; } = "";

        public int Units { get; set; }
    }
}