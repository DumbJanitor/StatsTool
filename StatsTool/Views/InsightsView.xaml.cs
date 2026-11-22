using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StatsTool.Views
{
    public partial class InsightsView : UserControl
    {
        // ============================================================
        // TEAM RESPONSIBILITIES
        // ============================================================

        // FRONTEND TEAM:
        // - Maintain product and reporting-week selectors.
        // - Render statistics, trend chart, and interpretation.
        // - Display the stock indicator and validation messages.
        // - Maintain responsive layout.
        //
        // BACKEND TEAM:
        // - Provide actual recorded weeks and product sales.
        // - Validate selected weeks and quantities.
        // - Calculate total, mean, median, range, and population
        //   standard deviation (divide by N, not N-1).
        // - Calculate inventory indicator from historical averages.
        // - Return predefined interpretations and trend points.
        // - Move sample calculation logic into the final service.
        //
        // DATABASE TEAM:
        // - Store stable product IDs, categories, selling units,
        //   stock, account ownership, dates, and sales quantities.
        // - Query account-scoped actual recorded Monday-Sunday weeks.
        // - Supply grouped product-week quantities.
        // - Distinguish missing records from saved zero quantities.
        //
        // SCOPE:
        // No forecasting, demand prediction, revenue/profit analysis,
        // or live point-of-sale integration. Inventory status compares
        // available stock with average recorded weekly sales only.

        // ============================================================
        // FRONTEND MODELS
        // ============================================================

        public sealed class StatisticsRow
        {
            public string ProductId { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string SellingUnits { get; set; } = "";
            public int AvailableStock { get; set; }
            public int RecordedWeeks { get; set; }
            public int Total { get; set; }
            public double Mean { get; set; }
            public double Median { get; set; }
            public int Range { get; set; }
            public double StandardDeviation { get; set; }

            public List<TrendPoint> Points { get; set; } =
                new List<TrendPoint>();
        }

        public sealed class TrendPoint
        {
            public DateTime WeekStart { get; set; }
            public int Units { get; set; }
        }

        private sealed class PreviewProduct
        {
            public string Id { get; }
            public string Name { get; }
            public string SellingUnits { get; }
            public int Stock { get; }
            public Dictionary<DateTime, int> Sales { get; }

            public PreviewProduct(
                string id,
                string name,
                string units,
                int stock,
                DateTime[] weeks,
                int[] quantities)
            {
                Id = id;
                Name = name;
                SellingUnits = units;
                Stock = stock;

                Sales = new Dictionary<DateTime, int>();

                for (int i = 0; i < weeks.Length; i++)
                {
                    Sales[weeks[i]] = quantities[i];
                }
            }
        }

        // ============================================================
        // SAMPLE RECORDED REPORTING WEEKS
        // ============================================================

        private readonly DateTime[] _sampleWeeks =
        {
            new DateTime(2026, 10, 12),
            new DateTime(2026, 10, 19),
            new DateTime(2026, 10, 26),
            new DateTime(2026, 11, 2),
            new DateTime(2026, 11, 9),
            new DateTime(2026, 11, 16)
        };

        private readonly List<PreviewProduct> _products =
            new List<PreviewProduct>();

        private List<StatisticsRow> _allStatistics =
            new List<StatisticsRow>();

        private StatisticsRow? _focusedRow;

        private string _focusedProductId = "rice-5kg";

        private bool _settingTable;

        // ============================================================
        // CHART COLORS
        // ============================================================

        private static readonly Brush Ink =
            NewBrush(11, 23, 56);

        private static readonly Brush Green =
            NewBrush(0, 151, 111);

        private static readonly Brush GridColor =
            NewBrush(205, 218, 236);

        // ============================================================
        // INITIALIZATION
        // ============================================================

        public InsightsView()
        {
            InitializeComponent();

            LoadPreviewData();

            ProductComboBox.Items.Add("All Products");

            foreach (PreviewProduct product in _products)
            {
                ProductComboBox.Items.Add(product.Name);
            }

            ProductComboBox.SelectedIndex = 0;

            // FRONTEND:
            // Both boundaries use actual recorded Monday-Sunday weeks,
            // matching the Comparisons page.
            //
            // BACKEND:
            // Replace _sampleWeeks with available recorded weeks
            // returned by the statistics service.
            //
            // DATABASE:
            // Supply the account's recorded reporting-week starts.

            foreach (DateTime weekStart in
                     _sampleWeeks.OrderBy(date => date))
            {
                FromWeekComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = FormatWeek(weekStart),
                        Tag = weekStart
                    });

                ThroughWeekComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = FormatWeek(weekStart),
                        Tag = weekStart
                    });
            }

            FromWeekComboBox.SelectedIndex = 0;

            ThroughWeekComboBox.SelectedIndex =
                ThroughWeekComboBox.Items.Count - 1;

            RunAnalysis();
        }

        // ============================================================
        // SAMPLE DATA - UNCHANGED
        // ============================================================

        private void LoadPreviewData()
        {
            // FRONTEND:
            // These sample records match the approved wireframe.
            //
            // BACKEND:
            // Replace the sample source with saved records from
            // the statistics service.
            //
            // DATABASE:
            // Return grouped quantities by product ID and week.
            //
            // Weekly totals:
            // 213, 227, 245, 258, 270, 284

            _products.Add(
                new PreviewProduct(
                    "water",
                    "Bottled Water",
                    "bottles",
                    100,
                    _sampleWeeks,
                    new[] { 58, 62, 70, 75, 80, 84 }));

            _products.Add(
                new PreviewProduct(
                    "noodles",
                    "Instant Noodles",
                    "packs",
                    85,
                    _sampleWeeks,
                    new[] { 53, 56, 60, 63, 66, 72 }));

            _products.Add(
                new PreviewProduct(
                    "rice-5kg",
                    "Rice 5kg",
                    "bags",
                    18,
                    _sampleWeeks,
                    new[] { 40, 43, 45, 47, 49, 52 }));

            _products.Add(
                new PreviewProduct(
                    "tuna",
                    "Canned Tuna",
                    "cans",
                    12,
                    _sampleWeeks,
                    new[] { 37, 39, 42, 44, 45, 46 }));

            _products.Add(
                new PreviewProduct(
                    "oil",
                    "Cooking Oil",
                    "bottles",
                    36,
                    _sampleWeeks,
                    new[] { 25, 27, 28, 29, 30, 30 }));
        }

        // ============================================================
        // RECORDED REPORTING-WEEK RANGE
        // ============================================================

        private bool TryGetSelectedWeekRange(
            out DateTime start,
            out DateTime end)
        {
            start = default;
            end = default;

            // BACKEND:
            // Validate both recorded reporting-week selections.
            //
            // DATABASE:
            // Return the account's actual recorded weeks.
            // Do not create nonexistent reporting periods.

            if (FromWeekComboBox.SelectedItem
                    is not ComboBoxItem fromItem ||
                fromItem.Tag is not DateTime selectedStart ||
                ThroughWeekComboBox.SelectedItem
                    is not ComboBoxItem throughItem ||
                throughItem.Tag is not DateTime selectedEnd)
            {
                ShowStatus("Choose both reporting weeks.");
                return false;
            }

            if (selectedStart > selectedEnd)
            {
                ShowStatus(
                    "The first reporting week must not be " +
                    "after the last week.");

                return false;
            }

            start = selectedStart;
            end = selectedEnd;

            ShowStatus(null);
            return true;
        }

        // ============================================================
        // ANALYZE BUTTON
        // ============================================================

        private void AnalyzeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            RunAnalysis();
        }

        // ============================================================
        // ANALYZE SELECTED PRODUCT AND RECORDED WEEKS
        // ============================================================

        private void RunAnalysis()
        {
            // FRONTEND:
            // Display the statistics returned for the selected
            // product and inclusive range of reporting weeks.
            //
            // BACKEND:
            // Validate the range, retrieve saved quantities,
            // calculate statistics, and return results.
            //
            // DATABASE:
            // Return actual recorded weeks in the selected range.

            if (!TryGetSelectedWeekRange(
                    out DateTime start,
                    out DateTime end))
            {
                return;
            }

            List<DateTime> weeks =
                _sampleWeeks
                    .Where(week =>
                        week >= start &&
                        week <= end)
                    .OrderBy(week => week)
                    .ToList();

            if (weeks.Count == 0)
            {
                ShowStatus(
                    "There are no recorded sales " +
                    "in that reporting period.");

                ClearResults();
                return;
            }

            _allStatistics =
                _products
                    .Select(product =>
                        CalculateStatistics(product, weeks))
                    .Where(row => row.RecordedWeeks > 0)
                    .ToList();

            if (_allStatistics.Count == 0)
            {
                ShowStatus(
                    "No product sales records were found " +
                    "for that period.");

                ClearResults();
                return;
            }

            string chosenProduct =
                ProductComboBox.SelectedItem as string
                ?? "All Products";

            List<StatisticsRow> visibleRows =
                chosenProduct == "All Products"
                    ? _allStatistics
                    : _allStatistics
                        .Where(row =>
                            row.ProductName == chosenProduct)
                        .ToList();

            if (visibleRows.Count == 0)
            {
                ShowStatus(
                    "The selected product has no recorded " +
                    "sales in that period.");

                ClearResults();
                return;
            }

            ShowStatus(null);

            RecordedWeeksText.Text =
                $"{weeks.Count} recorded week" +
                (weeks.Count == 1 ? "" : "s");

            SummarySubtitle.Text =
                $"Weekly units sold by product · " +
                $"{weeks.Count} recorded week" +
                (weeks.Count == 1 ? "" : "s");

            // Keep the previous focused product when possible.
            // Rice 5kg remains the initial highlighted row.

            StatisticsRow focus =
                visibleRows.FirstOrDefault(
                    row => row.ProductId == _focusedProductId)
                ?? visibleRows.First();

            _settingTable = true;

            SummaryTable.ItemsSource = visibleRows;
            SummaryTable.SelectedItem = focus;

            _settingTable = false;

            UpdateProductInterpretation(focus);
            UpdatePeriodHighlights(_allStatistics);
        }

        // ============================================================
        // DESCRIPTIVE STATISTICS - UNCHANGED
        // ============================================================

        private static StatisticsRow CalculateStatistics(
            PreviewProduct product,
            List<DateTime> periodWeeks)
        {
            // BACKEND:
            // Move these calculations into the statistics service.
            // Use POPULATION standard deviation: divide by N, not N-1.
            //
            // DATABASE:
            // Return actual product-week quantities.
            // Do not invent zeros for missing records.

            List<TrendPoint> points =
                periodWeeks
                    .Where(week =>
                        product.Sales.ContainsKey(week))
                    .Select(week =>
                        new TrendPoint
                        {
                            WeekStart = week,
                            Units = product.Sales[week]
                        })
                    .ToList();

            int[] values =
                points
                    .Select(point => point.Units)
                    .OrderBy(n => n)
                    .ToArray();

            int count = values.Length;

            if (count == 0)
            {
                return new StatisticsRow
                {
                    ProductId = product.Id,
                    ProductName = product.Name
                };
            }

            int total = values.Sum();

            double mean =
                (double)total / count;

            double median =
                count % 2 == 1
                    ? values[count / 2]
                    : (values[count / 2 - 1] +
                       values[count / 2]) / 2.0;

            int range =
                values[count - 1] - values[0];

            double variance =
                values.Sum(value =>
                    Math.Pow(value - mean, 2)) / count;

            return new StatisticsRow
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SellingUnits = product.SellingUnits,
                AvailableStock = product.Stock,
                RecordedWeeks = count,
                Total = total,
                Mean = mean,
                Median = median,
                Range = range,
                StandardDeviation = Math.Sqrt(variance),
                Points = points
            };
        }

        // ============================================================
        // SELECTED PRODUCT INTERPRETATION - UNCHANGED
        // ============================================================

        private void SummaryTable_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_settingTable ||
                SummaryTable.SelectedItem is not StatisticsRow row)
            {
                return;
            }

            UpdateProductInterpretation(row);
        }

        private void UpdateProductInterpretation(
            StatisticsRow row)
        {
            _focusedRow = row;
            _focusedProductId = row.ProductId;

            InterpretationProduct.Text =
                row.ProductName;

            InterpretationWeeks.Text =
                $"Based on {row.RecordedWeeks} recorded week" +
                (row.RecordedWeeks == 1 ? "" : "s");

            AverageSalesText.Text =
                $"{FormatQuantity(row.Mean)} " +
                $"{row.SellingUnits}/week";

            if (row.Range == 0)
            {
                VariabilityText.Text =
                    "Weekly sales remained identical " +
                    "across the recorded weeks " +
                    "(range: 0; standard deviation: 0.00).";
            }
            else
            {
                VariabilityText.Text =
                    "Weekly sales varied across the " +
                    "recorded weeks " +
                    $"(range: {row.Range}; standard deviation: " +
                    $"{row.StandardDeviation:F2}).";
            }

            AvailableStockText.Text =
                $"{row.AvailableStock:N0} {row.SellingUnits}";

            InventoryAverageText.Text =
                $"{FormatQuantity(row.Mean)} {row.SellingUnits}";

            bool belowAverage =
                row.AvailableStock < row.Mean;

            StockStatusBadge.Visibility =
                Visibility.Visible;

            StockStatusText.Text =
                belowAverage ? "Below Average" : "Sufficient";

            StockStatusText.Foreground =
                belowAverage
                    ? NewBrush(227, 70, 25)
                    : NewBrush(0, 128, 99);

            StockStatusBadge.Background =
                belowAverage
                    ? NewBrush(255, 230, 205)
                    : NewBrush(215, 246, 235);

            StockExplanationText.Text =
                belowAverage
                    ? "Available stock is below average " +
                      "recorded weekly sales."
                    : "Available stock meets or exceeds " +
                      "average recorded weekly sales.";

            TrendTitle.Text =
                "Weekly Sales Trend — " + row.ProductName;

            YAxisLabel.Text =
                char.ToUpperInvariant(row.SellingUnits[0]) +
                row.SellingUnits.Substring(1) + " sold";

            DrawTrendChart();
        }

        private void UpdatePeriodHighlights(
            List<StatisticsRow> rows)
        {
            // Based only on recorded quantities in the period.

            StatisticsRow highest =
                rows.OrderByDescending(row => row.Total).First();

            StatisticsRow lowest =
                rows.OrderBy(row => row.Total).First();

            HighestTotalText.Text =
                $"{highest.ProductName} ({highest.Total:N0})";

            LowestTotalText.Text =
                $"{lowest.ProductName} ({lowest.Total:N0})";
        }

        // ============================================================
        // RECORDED WEEKLY TREND CHART - UNCHANGED
        // ============================================================

        private void TrendCanvas_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            DrawTrendChart();
        }

        private void DrawTrendChart()
        {
            // FRONTEND:
            // Native WPF chart of recorded points only.
            // Redraw it when the content width changes.
            //
            // BACKEND:
            // Supply ordered actual recorded week/value pairs.
            // Never add predictive points.

            TrendCanvas.Children.Clear();

            if (_focusedRow == null ||
                _focusedRow.Points.Count == 0)
            {
                return;
            }

            double width = TrendCanvas.ActualWidth;
            double height = TrendCanvas.ActualHeight;

            if (width < 170 || height < 100)
                return;

            List<TrendPoint> points =
                _focusedRow.Points
                    .OrderBy(p => p.WeekStart)
                    .ToList();

            double left = 45;
            double right = width - 18;
            double top = 18;
            double bottom = height - 38;

            double plotWidth = Math.Max(1, right - left);
            double plotHeight = Math.Max(1, bottom - top);

            int minimumValue = points.Min(p => p.Units);
            int maximumValue = points.Max(p => p.Units);

            double axisMin =
                Math.Max(
                    0,
                    Math.Floor((minimumValue - 10) / 5.0) * 5);

            double axisMax =
                Math.Ceiling((maximumValue + 8) / 5.0) * 5;

            if (axisMax <= axisMin)
                axisMax = axisMin + 5;

            double interval =
                NiceInterval((axisMax - axisMin) / 6.0);

            axisMin =
                Math.Max(
                    0,
                    Math.Floor(axisMin / interval) * interval);

            axisMax =
                Math.Ceiling(axisMax / interval) * interval;

            // HORIZONTAL GRID LINES AND LABELS

            for (double tick = axisMin;
                 tick <= axisMax + interval * 0.01;
                 tick += interval)
            {
                double y =
                    bottom -
                    (tick - axisMin) /
                    (axisMax - axisMin) * plotHeight;

                TrendCanvas.Children.Add(
                    new Line
                    {
                        X1 = left,
                        Y1 = y,
                        X2 = right,
                        Y2 = y,
                        Stroke = GridColor,
                        StrokeThickness = 1
                    });

                var tickLabel = new TextBlock
                {
                    Text = tick.ToString(
                        "0.##",
                        CultureInfo.InvariantCulture),
                    FontSize = 12,
                    Foreground = Ink,
                    Width = 36,
                    TextAlignment = TextAlignment.Right
                };

                Canvas.SetLeft(tickLabel, 2);
                Canvas.SetTop(tickLabel, y - 9);

                TrendCanvas.Children.Add(tickLabel);
            }

            // AXES

            TrendCanvas.Children.Add(
                new Line
                {
                    X1 = left,
                    Y1 = top,
                    X2 = left,
                    Y2 = bottom,
                    Stroke = Ink,
                    StrokeThickness = 1
                });

            TrendCanvas.Children.Add(
                new Line
                {
                    X1 = left,
                    Y1 = bottom,
                    X2 = right,
                    Y2 = bottom,
                    Stroke = Ink,
                    StrokeThickness = 1
                });

            // TREND LINE

            var series = new Polyline
            {
                Stroke = Green,
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            };

            for (int i = 0; i < points.Count; i++)
            {
                TrendPoint point = points[i];

                double x =
                    points.Count == 1
                        ? left + plotWidth / 2
                        : left + i * plotWidth /
                          (points.Count - 1);

                double y =
                    bottom -
                    (point.Units - axisMin) /
                    (axisMax - axisMin) * plotHeight;

                series.Points.Add(new Point(x, y));

                var dot = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Fill = Green,
                    Stroke = Green,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(dot, x - 4.5);
                Canvas.SetTop(dot, y - 4.5);

                TrendCanvas.Children.Add(dot);

                var valueText = new TextBlock
                {
                    Text = point.Units.ToString("N0"),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Ink,
                    Width = 48,
                    TextAlignment = TextAlignment.Center
                };

                Canvas.SetLeft(valueText, x - 24);
                Canvas.SetTop(valueText, Math.Max(0, y - 23));

                TrendCanvas.Children.Add(valueText);

                var weekText = new TextBlock
                {
                    Text = point.WeekStart.ToString(
                        "MMM d",
                        CultureInfo.InvariantCulture),
                    FontSize = 11,
                    Foreground = Ink,
                    Width = 48,
                    TextAlignment = TextAlignment.Center
                };

                Canvas.SetLeft(weekText, x - 24);
                Canvas.SetTop(weekText, bottom + 8);

                TrendCanvas.Children.Add(weekText);
            }

            // Line above gridlines, beneath dots and labels.

            TrendCanvas.Children.Insert(
                TrendCanvas.Children.Count -
                points.Count * 3,
                series);
        }

        private static double NiceInterval(double raw)
        {
            if (raw <= 5) return 5;
            if (raw <= 10) return 10;
            if (raw <= 20) return 20;
            if (raw <= 50) return 50;
            if (raw <= 100) return 100;

            double magnitude =
                Math.Pow(
                    10,
                    Math.Floor(Math.Log10(raw)));

            return Math.Ceiling(raw / magnitude) * magnitude;
        }

        // ============================================================
        // RESPONSIVE LAYOUT - UNCHANGED
        // ============================================================

        private void PageRoot_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            bool stack = e.NewSize.Width < 780;

            BodyGrid.ColumnDefinitions[0].Width =
                stack
                    ? new GridLength(1, GridUnitType.Star)
                    : new GridLength(2.2, GridUnitType.Star);

            BodyGrid.ColumnDefinitions[1].Width =
                stack
                    ? new GridLength(0)
                    : new GridLength(1.2, GridUnitType.Star);

            Grid.SetColumn(InterpretationCard, stack ? 0 : 1);
            Grid.SetRow(InterpretationCard, stack ? 1 : 0);

            InterpretationCard.Margin =
                stack
                    ? new Thickness(0, 14, 0, 0)
                    : new Thickness(0);

            LeftColumn.Margin =
                stack
                    ? new Thickness(0)
                    : new Thickness(0, 0, 14, 0);
        }

        private void ShowStatus(string? message)
        {
            StatusText.Text = message ?? "";

            StatusText.Visibility =
                string.IsNullOrEmpty(message)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private void ClearResults()
        {
            _focusedRow = null;
            _allStatistics.Clear();

            SummaryTable.ItemsSource = null;

            RecordedWeeksText.Text = "0 recorded weeks";
            SummarySubtitle.Text =
                "No recorded data in the selected period";

            TrendTitle.Text = "Weekly Sales Trend";
            TrendCanvas.Children.Clear();

            InterpretationProduct.Text = "—";
            InterpretationWeeks.Text = "No recorded weeks";
            AverageSalesText.Text = "—";
            VariabilityText.Text = "—";
            AvailableStockText.Text = "—";
            InventoryAverageText.Text = "—";

            StockStatusBadge.Visibility = Visibility.Collapsed;

            StockExplanationText.Text = "—";
            HighestTotalText.Text = "—";
            LowestTotalText.Text = "—";
        }

        // ============================================================
        // SHARED MONDAY-SUNDAY REPORTING-WEEK FORMAT
        // ============================================================

        private static string FormatWeek(DateTime weekStart)
        {
            DateTime weekEnd = weekStart.AddDays(6);

            if (weekStart.Year == weekEnd.Year &&
                weekStart.Month == weekEnd.Month)
            {
                return
                    $"{weekStart:MMM} {weekStart.Day}–" +
                    $"{weekEnd.Day}, {weekEnd.Year}";
            }

            if (weekStart.Year == weekEnd.Year)
            {
                return
                    $"{weekStart:MMM} {weekStart.Day}–" +
                    $"{weekEnd:MMM} {weekEnd.Day}, {weekEnd.Year}";
            }

            return
                $"{weekStart:MMM} {weekStart.Day}, {weekStart.Year}–" +
                $"{weekEnd:MMM} {weekEnd.Day}, {weekEnd.Year}";
        }

        private static string FormatQuantity(double value)
        {
            return Math.Abs(value - Math.Round(value)) < 0.000001
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static Brush NewBrush(byte r, byte g, byte b)
        {
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }
}