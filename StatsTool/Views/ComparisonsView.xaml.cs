using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StatsTool.Views
{
    public partial class ComparisonsView : UserControl
    {
        private readonly Dictionary<DateTime, Dictionary<string, int>> _weeklySales =
            new Dictionary<DateTime, Dictionary<string, int>>();

        private bool _initializing = true;

        private static readonly Brush InkBrush = MakeBrush(11, 23, 56);
        private static readonly Brush PositiveBrush = MakeBrush(4, 153, 111);
        private static readonly Brush NegativeBrush = MakeBrush(191, 70, 62);
        private static readonly Brush NeutralBrush = MakeBrush(11, 23, 56);
        private static readonly Brush CurrentBarBrush = MakeBrush(0, 141, 122);
        private static readonly Brush SelectedBarBrush = MakeBrush(115, 214, 191);
        private static readonly Brush RowBorderBrush = MakeBrush(227, 234, 243);

        public ComparisonsView()
        {
            InitializeComponent();

            LoadSampleWeeks();

            // FRONTEND:
            // Populate Current Week with recorded Monday-Sunday weeks.
            // No individual date selection.
            //
            // BACKEND:
            // Replace these sample weeks with the service's actual
            // available recorded weeks for the authenticated account.
            //
            // DATABASE:
            // Provide account-scoped recorded week-start dates.

            foreach (DateTime weekStart in _weeklySales.Keys
                         .OrderByDescending(date => date))
            {
                CurrentWeekComboBox.Items.Add(new ComboBoxItem
                {
                    Content = FormatWeek(weekStart),
                    Tag = weekStart
                });
            }

            CurrentWeekComboBox.SelectedIndex = 0;

            _initializing = false;

            RefreshComparisonWeekOptions();
            CompareSelectedWeeks();
        }

        // ==============================================================
        // TEAM RESPONSIBILITIES
        // ==============================================================

        // FRONTEND TEAM:
        // - Maintain week dropdowns, tables, charts, and summaries.
        // - Display validation and statistical interpretation.
        // - Maintain responsive layout.
        //
        // BACKEND TEAM:
        // - Provide available recorded reporting weeks.
        // - Validate two selected weeks.
        // - Retrieve quantities through the data layer.
        // - Calculate totals, differences, percentage change,
        //   per-product differences, and largest increase.
        // - Return predefined interpretations.
        // - Handle zero baselines safely.
        // - Move preview calculations into the final service.
        //
        // DATABASE TEAM:
        // - Store stable product IDs, names, categories, units,
        //   account ownership, dates, and nonnegative quantities.
        // - Maintain product-to-sales-entry relationships.
        // - Query actual recorded Monday-Sunday weeks.
        // - Aggregate quantities by product ID and week.
        // - Distinguish missing records from saved zero quantities.
        // - Prevent duplicate results caused by joins.
        //
        // PROJECT SCOPE:
        // Read-only descriptive comparisons of recorded quantities.
        // No forecasts, revenue/profit analysis, or live POS.
        // Counts are selling-unit quantities, not equivalent
        // physical measurements such as liters and kilograms.

        // ==============================================================
        // TEMPORARY SAMPLE DATA
        // ==============================================================

        private void LoadSampleWeeks()
        {
            // FRONTEND: Preview values only.
            // BACKEND: Replace this source with the comparison service.
            // DATABASE: Supply actual saved weekly product totals.

            _weeklySales[new DateTime(2026, 10, 12)] =
                Week(64, 54, 38, 32, 25);

            _weeklySales[new DateTime(2026, 10, 19)] =
                Week(68, 58, 42, 34, 25);

            _weeklySales[new DateTime(2026, 10, 26)] =
                Week(72, 62, 46, 39, 26);

            _weeklySales[new DateTime(2026, 11, 2)] =
                Week(76, 64, 48, 41, 29);

            _weeklySales[new DateTime(2026, 11, 9)] =
                Week(80, 66, 49, 45, 30);

            _weeklySales[new DateTime(2026, 11, 16)] =
                Week(84, 72, 52, 46, 30);
        }

        private static Dictionary<string, int> Week(
            int water, int noodles, int rice, int tuna, int oil)
        {
            return new Dictionary<string, int>
            {
                ["Bottled Water"] = water,
                ["Instant Noodles"] = noodles,
                ["Rice 5kg"] = rice,
                ["Canned Tuna"] = tuna,
                ["Cooking Oil"] = oil
            };
        }

        // ==============================================================
        // CURRENT WEEK DROPDOWN
        // ==============================================================

        private void CurrentWeekComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_initializing)
                return;

            RefreshComparisonWeekOptions();

            // Results change only after clicking Compare Weeks.
        }

        // ==============================================================
        // EARLIER RECORDED WEEKS
        // ==============================================================

        private void RefreshComparisonWeekOptions()
        {
            // BACKEND:
            // Use actual recorded week starts, not generated dates.
            //
            // DATABASE:
            // Return week starts earlier than the current selection.

            DateTime? selectedPreviousWeek =
                (CompareWithComboBox.SelectedItem as ComboBoxItem)?.Tag
                    is DateTime previous
                    ? previous
                    : (DateTime?)null;

            CompareWithComboBox.Items.Clear();

            if (CurrentWeekComboBox.SelectedItem
                    is not ComboBoxItem currentItem ||
                currentItem.Tag is not DateTime currentStart)
            {
                return;
            }

            foreach (DateTime weekStart in _weeklySales.Keys
                         .Where(date => date < currentStart)
                         .OrderByDescending(date => date))
            {
                CompareWithComboBox.Items.Add(new ComboBoxItem
                {
                    Content = FormatWeek(weekStart),
                    Tag = weekStart
                });
            }

            ComboBoxItem? restoredItem =
                CompareWithComboBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(item =>
                        selectedPreviousWeek.HasValue &&
                        item.Tag is DateTime week &&
                        week == selectedPreviousWeek.Value);

            if (restoredItem != null)
            {
                CompareWithComboBox.SelectedItem = restoredItem;
            }
            else if (CompareWithComboBox.Items.Count > 0)
            {
                CompareWithComboBox.SelectedIndex = 0;
            }
        }

        private void CompareWeeksButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CompareSelectedWeeks();
        }

        // ==============================================================
        // COMPARE SELECTED RECORDED WEEKS
        // ==============================================================

        private void CompareSelectedWeeks()
        {
            // BACKEND:
            // Validate selections, retrieve quantities, and
            // calculate the comparison through a service.
            //
            // FRONTEND:
            // Display results and error messages.

            if (CurrentWeekComboBox.SelectedItem
                    is not ComboBoxItem currentItem ||
                currentItem.Tag is not DateTime currentStart)
            {
                ShowStatus("Please choose a current reporting week.");
                ClearResults();
                return;
            }

            if (!_weeklySales.TryGetValue(
                    currentStart,
                    out Dictionary<string, int>? current))
            {
                ShowStatus(
                    "There are no recorded sales for " +
                    FormatWeek(currentStart) + ".");

                ClearResults();
                return;
            }

            if (CompareWithComboBox.SelectedItem
                    is not ComboBoxItem selectedItem ||
                selectedItem.Tag is not DateTime selectedStart)
            {
                ShowStatus("Choose an earlier recorded week to compare.");
                ClearResults();
                return;
            }

            if (selectedStart >= currentStart ||
                !_weeklySales.TryGetValue(
                    selectedStart,
                    out Dictionary<string, int>? selected))
            {
                ShowStatus(
                    "The comparison week must be an earlier week " +
                    "with recorded sales.");

                ClearResults();
                return;
            }

            ShowStatus(null);

            RenderComparison(
                currentStart,
                current,
                selectedStart,
                selected);
        }

        // ==============================================================
        // WEEKLY TABLES, CHART, AND SUMMARY
        // ==============================================================

        private void RenderComparison(
            DateTime currentStart,
            Dictionary<string, int> current,
            DateTime selectedStart,
            Dictionary<string, int> selected)
        {
            // FRONTEND:
            // Preserve all existing presentation behavior.
            //
            // BACKEND:
            // Final service should own calculations and interpretation.
            //
            // DATABASE:
            // Use stable product IDs; distinguish missing from zero.

            List<string> products =
                current.Keys.Union(selected.Keys)
                    .OrderByDescending(name => GetUnits(current, name))
                    .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

            int currentTotal = current.Values.Sum();
            int selectedTotal = selected.Values.Sum();
            int difference = currentTotal - selectedTotal;

            double? percentage =
                selectedTotal == 0
                    ? (double?)null
                    : difference * 100.0 / selectedTotal;

            CurrentWeekDateText.Text = FormatWeek(currentStart);
            SelectedWeekDateText.Text = FormatWeek(selectedStart);

            CurrentTotalText.Text =
                currentTotal.ToString("N0", CultureInfo.InvariantCulture);

            SelectedTotalText.Text =
                selectedTotal.ToString("N0", CultureInfo.InvariantCulture);

            CurrentProductCountText.Text =
                current.Values.Count(n => n > 0).ToString();

            SelectedProductCountText.Text =
                selected.Values.Count(n => n > 0).ToString();

            CurrentProductsList.Children.Clear();
            SelectedProductsList.Children.Clear();
            ProductChartRows.Children.Clear();

            int barMaximum = Math.Max(
                1,
                Math.Max(
                    current.Values.DefaultIfEmpty().Max(),
                    selected.Values.DefaultIfEmpty().Max()));

            foreach (string product in products)
            {
                int currentUnits = GetUnits(current, product);
                int selectedUnits = GetUnits(selected, product);

                CurrentProductsList.Children.Add(
                    CreateProductRow(product, currentUnits));

                SelectedProductsList.Children.Add(
                    CreateProductRow(product, selectedUnits));

                ProductChartRows.Children.Add(
                    CreateProductChartRow(
                        product,
                        currentUnits,
                        selectedUnits,
                        barMaximum));
            }

            SummaryCurrentText.Text =
                currentTotal.ToString("N0") + " units";

            SummarySelectedText.Text =
                selectedTotal.ToString("N0") + " units";

            SummaryDifferenceText.Text =
                SignedNumber(difference) + " units";

            SummaryDifferenceText.Foreground =
                DeltaBrush(difference);

            SummaryPercentageText.Text =
                percentage.HasValue
                    ? SignedPercent(percentage.Value)
                    : "N/A";

            SummaryPercentageText.Foreground =
                percentage.HasValue
                    ? DeltaBrush(difference)
                    : NeutralBrush;

            if (selectedTotal == 0)
            {
                InterpretationText.Text =
                    "The selected week recorded zero units. " +
                    "A percentage change cannot be calculated " +
                    "using a zero baseline.";
            }
            else if (difference > 0)
            {
                InterpretationText.Text =
                    $"The current week recorded {difference:N0} more units sold " +
                    $"than the selected week, an increase of " +
                    $"{Math.Abs(percentage!.Value):F1}%.";
            }
            else if (difference < 0)
            {
                InterpretationText.Text =
                    $"The current week recorded {Math.Abs(difference):N0} fewer units " +
                    $"sold than the selected week, a decrease of " +
                    $"{Math.Abs(percentage!.Value):F1}%.";
            }
            else
            {
                InterpretationText.Text =
                    "Both weeks recorded the same total number of units sold.";
            }

            string? largestIncreaseProduct = null;
            int largestIncrease = 0;

            foreach (string product in products)
            {
                int productDifference =
                    GetUnits(current, product) -
                    GetUnits(selected, product);

                if (productDifference > largestIncrease)
                {
                    largestIncrease = productDifference;
                    largestIncreaseProduct = product;
                }
            }

            LargestIncreaseBorder.Visibility = Visibility.Visible;

            LargestIncreaseText.Text =
                largestIncreaseProduct != null
                    ? $"Largest Increase: {largestIncreaseProduct} " +
                      $"(+{largestIncrease:N0} units)"
                    : "Largest Increase: No product recorded an increase.";
        }

        private static int GetUnits(
            Dictionary<string, int> week,
            string product)
        {
            return week.TryGetValue(product, out int units)
                ? units
                : 0;
        }

        // ==============================================================
        // PRODUCT TABLE ROW
        // ==============================================================

        private static Border CreateProductRow(string product, int units)
        {
            var grid = new Grid();

            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(60)
                });

            var productText = new TextBlock
            {
                Text = product,
                ToolTip = product,
                FontSize = 13,
                Foreground = InkBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };

            var unitsText = new TextBlock
            {
                Text = units.ToString("N0", CultureInfo.InvariantCulture),
                FontSize = 13,
                Foreground = InkBrush,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(unitsText, 1);

            grid.Children.Add(productText);
            grid.Children.Add(unitsText);

            return new Border
            {
                BorderBrush = RowBorderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 6, 10, 6),
                Child = grid
            };
        }

        // ==============================================================
        // PRODUCT COMPARISON CHART
        // ==============================================================

        private static Grid CreateProductChartRow(
            string product,
            int currentUnits,
            int selectedUnits,
            int maximum)
        {
            var row = new Grid
            {
                MinHeight = 56,
                Margin = new Thickness(0, 2, 0, 2)
            };

            row.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(116)
                });

            row.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            row.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(52)
                });

            var name = new TextBlock
            {
                Text = product,
                ToolTip = product,
                FontSize = 12,
                Foreground = InkBrush,
                TextAlignment = TextAlignment.Right,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var bars = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            bars.Children.Add(
                CreateBar(currentUnits, maximum, CurrentBarBrush));

            bars.Children.Add(
                CreateBar(selectedUnits, maximum, SelectedBarBrush));

            Grid.SetColumn(bars, 1);

            int delta = currentUnits - selectedUnits;

            var difference = new TextBlock
            {
                Text = SignedNumber(delta),
                Foreground = DeltaBrush(delta),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(difference, 2);

            row.Children.Add(name);
            row.Children.Add(bars);
            row.Children.Add(difference);

            return row;
        }

        private static Grid CreateBar(int units, int maximum, Brush fill)
        {
            // FRONTEND: WPF bars scale with available width.
            // No forecasting or external graph dependency.

            var bar = new Grid
            {
                Height = 19,
                Margin = new Thickness(0, 0, 32, 0)
            };

            bar.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(units, GridUnitType.Star)
                });

            bar.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(
                        Math.Max(0, maximum - units),
                        GridUnitType.Star)
                });

            var filledPart = new Border
            {
                Background = fill,
                CornerRadius = new CornerRadius(3),
                Height = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            var value = new TextBlock
            {
                Text = units.ToString("N0", CultureInfo.InvariantCulture),
                Foreground = InkBrush,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            Grid.SetColumn(value, 1);

            bar.Children.Add(filledPart);
            bar.Children.Add(value);

            return bar;
        }

        // ==============================================================
        // RESPONSIVE LAYOUT - UNCHANGED
        // ==============================================================

        private void PageRoot_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            bool stackSummary = e.NewSize.Width < 760;

            BodyGrid.ColumnDefinitions[0].Width =
                stackSummary
                    ? new GridLength(1, GridUnitType.Star)
                    : new GridLength(2.2, GridUnitType.Star);

            BodyGrid.ColumnDefinitions[1].Width =
                stackSummary
                    ? new GridLength(0)
                    : new GridLength(1, GridUnitType.Star);

            Grid.SetColumn(SummaryCard, stackSummary ? 0 : 1);
            Grid.SetRow(SummaryCard, stackSummary ? 1 : 0);

            SummaryCard.Margin =
                stackSummary
                    ? new Thickness(0, 14, 0, 0)
                    : new Thickness(0);

            MainComparisonColumn.Margin =
                stackSummary
                    ? new Thickness(0)
                    : new Thickness(0, 0, 14, 0);

            double leftWidth =
                stackSummary
                    ? e.NewSize.Width
                    : (e.NewSize.Width - 14) * 2.2 / 3.2;

            bool stackWeekCards = leftWidth < 570;

            WeekCardsGrid.ColumnDefinitions[0].Width =
                new GridLength(1, GridUnitType.Star);

            WeekCardsGrid.ColumnDefinitions[1].Width =
                stackWeekCards
                    ? new GridLength(0)
                    : new GridLength(1, GridUnitType.Star);

            Grid.SetColumn(SelectedWeekCard, stackWeekCards ? 0 : 1);
            Grid.SetRow(SelectedWeekCard, stackWeekCards ? 1 : 0);

            CurrentWeekCard.Margin =
                stackWeekCards
                    ? new Thickness(0)
                    : new Thickness(0, 0, 7, 0);

            SelectedWeekCard.Margin =
                stackWeekCards
                    ? new Thickness(0, 14, 0, 0)
                    : new Thickness(7, 0, 0, 0);
        }

        private void ShowStatus(string? message)
        {
            ComparisonStatusText.Text = message ?? string.Empty;

            ComparisonStatusText.Visibility =
                string.IsNullOrEmpty(message)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private void ClearResults()
        {
            CurrentWeekDateText.Text = "—";
            SelectedWeekDateText.Text = "—";
            CurrentTotalText.Text = "—";
            SelectedTotalText.Text = "—";
            CurrentProductCountText.Text = "—";
            SelectedProductCountText.Text = "—";

            CurrentProductsList.Children.Clear();
            SelectedProductsList.Children.Clear();
            ProductChartRows.Children.Clear();

            SummaryCurrentText.Text = "—";
            SummarySelectedText.Text = "—";
            SummaryDifferenceText.Text = "—";
            SummaryPercentageText.Text = "—";

            InterpretationText.Text =
                "Choose two recorded reporting weeks and select Compare Weeks.";

            LargestIncreaseBorder.Visibility = Visibility.Collapsed;
        }

        // ==============================================================
        // COMMON MONDAY-SUNDAY WEEK FORMAT
        // ==============================================================

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

        private static string SignedNumber(int number)
        {
            return number > 0
                ? "+" + number.ToString("N0")
                : number.ToString("N0");
        }

        private static string SignedPercent(double value)
        {
            return (value > 0 ? "+" : string.Empty) +
                   value.ToString("F1") + "%";
        }

        private static Brush DeltaBrush(int delta)
        {
            return delta > 0
                ? PositiveBrush
                : delta < 0
                    ? NegativeBrush
                    : NeutralBrush;
        }

        private static Brush MakeBrush(byte red, byte green, byte blue)
        {
            return new SolidColorBrush(
                Color.FromRgb(red, green, blue));
        }
    }
}