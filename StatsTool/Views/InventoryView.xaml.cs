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
using System.Collections.ObjectModel;
using System.Linq;

namespace StatsTool.Views
{
    public partial class InventoryView : UserControl
    {
        // =====================================================
        // INVENTORY STATE
        // =====================================================

        private readonly List<InventoryProduct> _products = new();

        private readonly ObservableCollection<InventoryTableRow>
            _visibleRows = new();

        private InventoryProduct? _editingProduct;

        // Weekly view opens by default.
        private bool _showAllProducts = false;

        private readonly Brush _tealBrush =
            new SolidColorBrush(Color.FromRgb(0, 159, 145));

        private readonly Brush _navyBrush =
            new SolidColorBrush(Color.FromRgb(11, 21, 48));

        public InventoryView()
        {
            InitializeComponent();

            ProductsGrid.ItemsSource = _visibleRows;

            LoadSampleProducts();

            WeekDatePicker.SelectedDate =
                new DateTime(2026, 11, 22);

            UpdateViewControls();
            RefreshInventory();
        }

        // =====================================================
        // INITIAL DISPLAY
        // =====================================================

        private void InventoryView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            UpdateViewControls();
            RefreshInventory();
        }

        // =====================================================
        // PUBLIC BACKEND INTEGRATION POINT
        // =====================================================

        public void LoadInventory(
            IEnumerable<InventoryProduct> products)
        {
            if (products == null)
                throw new ArgumentNullException(nameof(products));

            // BACKEND TEAM:
            //
            // Call this method after obtaining the product
            // catalog and associated weekly records.
            //
            // Supply a stable ProductId for each product.
            //
            // The backend should perform authoritative
            // validation and database operations.
            //
            // Call this method on the WPF UI thread.
            //
            // DATABASE TEAM:
            //
            // Required product fields:
            // ProductId
            // Name
            // Category
            // SellingUnit
            // CurrentStock
            // DateAdded
            // LastModified
            // IsArchived
            //
            // Required weekly fields:
            // ProductId (relationship)
            // WeekStart
            // UnitsSold
            // StockAtWeekEnd
            //
            // WeekStart represents the Monday of the week.
            //
            // Preserve records for historical comparison.

            _products.Clear();
            _products.AddRange(products);

            RefreshInventory();
        }

        // =====================================================
        // VIEW MODE BUTTONS
        // =====================================================

        private void AllProductsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _showAllProducts = true;

            UpdateViewControls();
            RefreshInventory();
        }

        private void WeeklyViewButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _showAllProducts = false;

            UpdateViewControls();
            RefreshInventory();
        }

        private void UpdateViewControls()
        {
            if (AllProductsButton == null)
                return;

            // Selected mode
            AllProductsButton.Background =
                _showAllProducts ? _tealBrush : Brushes.White;

            AllProductsButton.Foreground =
                _showAllProducts ? Brushes.White : _tealBrush;

            WeeklyViewButton.Background =
                !_showAllProducts ? _tealBrush : Brushes.White;

            WeeklyViewButton.Foreground =
                !_showAllProducts ? Brushes.White : _tealBrush;

            // Calendar is relevant only in weekly mode.
            bool weeklyMode = !_showAllProducts;

            WeekDatePicker.IsEnabled = weeklyMode;
            PreviousWeekButton.IsEnabled = weeklyMode;
            NextWeekButton.IsEnabled = weeklyMode;
        }

        // =====================================================
        // CALENDAR / WEEK SELECTION
        // =====================================================

        private void PreviousWeekButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DateTime current =
                WeekDatePicker.SelectedDate ?? DateTime.Today;

            WeekDatePicker.SelectedDate =
                current.AddDays(-7);
        }

        private void NextWeekButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DateTime current =
                WeekDatePicker.SelectedDate ?? DateTime.Today;

            WeekDatePicker.SelectedDate =
                current.AddDays(7);
        }

        private void WeekDatePicker_SelectedDateChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            RefreshInventory();
        }

        // Convert any selected day into the Monday of its week.
        private static DateTime GetWeekStart(DateTime date)
        {
            int difference =
                ((int)date.DayOfWeek + 6) % 7;

            return date.Date.AddDays(-difference);
        }

        private static string FormatWeek(DateTime start)
        {
            DateTime end = start.AddDays(6);

            if (start.Month == end.Month &&
                start.Year == end.Year)
            {
                return $"{start:MMM d}–{end:d}, {end:yyyy}";
            }

            return $"{start:MMM d}–{end:MMM d, yyyy}";
        }

        // =====================================================
        // SEARCH AND STATUS FILTERS
        // =====================================================

        private void SearchTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            RefreshInventory();
        }

        private void StatusFilter_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            RefreshInventory();
        }

        private string GetSelectedStatus()
        {
            if (StatusFilter.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? "All";
            }

            return "All";
        }

        // =====================================================
        // REFRESH TABLE
        // =====================================================

        private void RefreshInventory()
        {
            if (ProductsGrid == null)
                return;

            _visibleRows.Clear();

            DateTime selectedWeek = GetWeekStart(
                WeekDatePicker.SelectedDate ??
                new DateTime(2026, 11, 22));

            string search =
                SearchTextBox.Text.Trim();

            string selectedStatus = GetSelectedStatus();

            foreach (InventoryProduct product in
                     _products
                         .Where(p => !p.IsArchived)
                         .OrderBy(p => p.Name))
            {
                // Weekly record for the selected week.
                InventoryWeekRecord? selectedRecord =
                    product.WeekRecords.FirstOrDefault(
                        r => r.WeekStart == selectedWeek);

                // Weekly view only shows products recorded
                // during that reporting week.
                if (!_showAllProducts && selectedRecord == null)
                    continue;

                // In All Products mode, use all recorded weeks.
                // In weekly mode, use records up to that week.
                //
                // BACKEND TEAM:
                // Perform the authoritative historical-average
                // calculation using the paper's approved rules.

                List<InventoryWeekRecord> history =
                    product.WeekRecords
                        .Where(r =>
                            _showAllProducts ||
                            r.WeekStart <= selectedWeek)
                        .OrderBy(r => r.WeekStart)
                        .ToList();

                double average = history.Count > 0
                    ? history.Average(r => r.UnitsSold)
                    : 0;

                InventoryWeekRecord? latestRecord =
                    product.WeekRecords
                        .OrderByDescending(r => r.WeekStart)
                        .FirstOrDefault();

                // All Products: current stock.
                // By Week: stock recorded at the end of that week.
                int displayedStock = _showAllProducts
                    ? product.CurrentStock
                    : selectedRecord!.StockAtWeekEnd;

                int? displayedUnits = _showAllProducts
                    ? latestRecord?.UnitsSold
                    : selectedRecord!.UnitsSold;

                string status;

                if (history.Count == 0)
                {
                    status = "No history";
                }
                else if (displayedStock < average)
                {
                    status = "Below average";
                }
                else
                {
                    status = "Sufficient";
                }

                // Search product name, category, or unit.
                bool matchesSearch =
                    string.IsNullOrWhiteSpace(search) ||
                    product.Name.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    product.Category.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    product.SellingUnit.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase);

                if (!matchesSearch)
                    continue;

                // Status: All means no status restriction.
                if (selectedStatus != "All" &&
                    selectedStatus != status)
                {
                    continue;
                }

                Brush badgeBackground;
                Brush badgeForeground;

                if (status == "Below average")
                {
                    badgeBackground =
                        new SolidColorBrush(
                            Color.FromRgb(255, 233, 212));

                    badgeForeground =
                        new SolidColorBrush(
                            Color.FromRgb(233, 74, 22));
                }
                else if (status == "Sufficient")
                {
                    badgeBackground =
                        new SolidColorBrush(
                            Color.FromRgb(218, 247, 236));

                    badgeForeground =
                        new SolidColorBrush(
                            Color.FromRgb(0, 131, 100));
                }
                else
                {
                    badgeBackground =
                        new SolidColorBrush(
                            Color.FromRgb(235, 239, 244));

                    badgeForeground =
                        new SolidColorBrush(
                            Color.FromRgb(93, 107, 127));
                }

                _visibleRows.Add(new InventoryTableRow
                {
                    SourceProduct = product,

                    Product = product.Name,
                    Category = product.Category,

                    UnitsSoldText = displayedUnits?.ToString()
                        ?? "—",

                    Stock = displayedStock,

                    WeeklyAverageText = history.Count == 0
                        ? "—"
                        : average.ToString("0.#"),

                    Status = status,

                    StatusBackground = badgeBackground,
                    StatusForeground = badgeForeground,

                    DateAddedText =
                        product.DateAdded.ToString("MMM d, yyyy"),

                    LastModifiedText =
                        product.LastModified.ToString("MMM d, yyyy")
                });
            }

            // Update table headings.
            if (_showAllProducts)
            {
                CurrentViewLabel.Text = "All Products";

                WeekRangeLabel.Text =
                    "Complete active product catalog";

                UnitsSoldColumn.Header = "Latest Week";

                StockColumn.Header = "Current Stock";
            }
            else
            {
                CurrentViewLabel.Text = "Weekly Inventory";

                WeekRangeLabel.Text =
                    FormatWeek(selectedWeek);

                UnitsSoldColumn.Header = "Units Sold";

                StockColumn.Header = "Week-End Stock";
            }

            int count = _visibleRows.Count;

            ProductCountLabel.Text =
                $"{count} product{(count == 1 ? "" : "s")}";
        }

        // =====================================================
        // ADD PRODUCT
        // =====================================================

        private void AddProductButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _editingProduct = null;

            EditorTitle.Text = "Add Product";

            ProductNameInput.Text = "";
            CategoryInput.Text = "";
            SellingUnitInput.Text = "";

            EditorErrorText.Visibility = Visibility.Collapsed;
            EditorOverlay.Visibility = Visibility.Visible;

            ProductNameInput.Focus();
        }

        // =====================================================
        // EDIT PRODUCT
        // =====================================================

        private void EditProductButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not InventoryTableRow row)
            {
                return;
            }

            _editingProduct = row.SourceProduct;

            EditorTitle.Text = "Edit Product";

            ProductNameInput.Text = _editingProduct.Name;
            CategoryInput.Text = _editingProduct.Category;
            SellingUnitInput.Text = _editingProduct.SellingUnit;

            EditorErrorText.Visibility = Visibility.Collapsed;
            EditorOverlay.Visibility = Visibility.Visible;

            ProductNameInput.Focus();
        }

        // =====================================================
        // SAVE PRODUCT
        // =====================================================

        private void SaveProductButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string name = ProductNameInput.Text.Trim();
            string category = CategoryInput.Text.Trim();
            string sellingUnit = SellingUnitInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(category) ||
                string.IsNullOrWhiteSpace(sellingUnit))
            {
                EditorErrorText.Text =
                    "Please complete all product fields.";

                EditorErrorText.Visibility = Visibility.Visible;
                return;
            }

            // Prevent duplicate active product names in the
            // frontend preview.
            //
            // BACKEND TEAM:
            // Validate product identity and duplicates again
            // on the server.

            bool duplicateExists = _products.Any(p =>
                !p.IsArchived &&
                p.ProductId != _editingProduct?.ProductId &&
                string.Equals(
                    p.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase));

            if (duplicateExists)
            {
                EditorErrorText.Text =
                    "A product with this name already exists.";

                EditorErrorText.Visibility = Visibility.Visible;
                return;
            }

            if (_editingProduct == null)
            {
                // FRONTEND PREVIEW:
                // New products begin with zero stock.
                // Actual stock should be entered via Daily Entry.

                InventoryProduct newProduct = new InventoryProduct
                {
                    ProductId = Guid.NewGuid(),
                    Name = name,
                    Category = category,
                    SellingUnit = sellingUnit,
                    CurrentStock = 0,
                    DateAdded = DateTime.Now,
                    LastModified = DateTime.Now
                };

                _products.Add(newProduct);

                // Show the new product in the catalog.
                _showAllProducts = true;
                UpdateViewControls();
            }
            else
            {
                // Edit product details only.
                // Do not change historical weekly quantities.

                _editingProduct.Name = name;
                _editingProduct.Category = category;
                _editingProduct.SellingUnit = sellingUnit;
                _editingProduct.LastModified = DateTime.Now;
            }

            // =================================================
            // BACKEND TEAM - SAVE PRODUCT
            // =================================================
            //
            // Connect this action to CreateProduct or UpdateProduct.
            // Validate all fields before saving.
            // Return the database-assigned product identity.
            // Refresh the product list after successful save.
            //
            // DATABASE TEAM:
            // Maintain product fields and creation/update dates.
            // Do not overwrite historical weekly sales records.
            //
            // Current behavior changes only in-memory sample data.
            // It does NOT persist to the database.

            EditorOverlay.Visibility = Visibility.Collapsed;
            _editingProduct = null;

            RefreshInventory();
        }

        // =====================================================
        // CANCEL PRODUCT EDITOR
        // =====================================================

        private void CancelEditorButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            EditorOverlay.Visibility = Visibility.Collapsed;
            _editingProduct = null;
        }

        // =====================================================
        // DELETE PRODUCT
        // =====================================================

        private void DeleteProductButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not InventoryTableRow row)
            {
                return;
            }

            InventoryProduct product = row.SourceProduct;

            MessageBoxResult confirmation = MessageBox.Show(
                $"Remove '{product.Name}' from the active product list?\n\n" +
                "Historical sales records should be preserved.",
                "Delete Product",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
                return;

            // FRONTEND PREVIEW:
            // Hide the product without destroying its history.

            product.IsArchived = true;
            product.LastModified = DateTime.Now;

            // =================================================
            // BACKEND TEAM - PRODUCT DELETION
            // =================================================
            //
            // Implement the approved delete/archive behavior.
            // Prefer preserving products referenced by
            // historical sales and inventory records.
            //
            // DATABASE TEAM:
            // Maintain referential integrity.
            // Do not automatically delete historical sales
            // when removing a product from the active catalog.
            //
            // Current behavior is in-memory only.

            RefreshInventory();
        }

        // =====================================================
        // DAILY ENTRY NAVIGATION
        // =====================================================

        private void DailyEntryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is StatsTool.MainWindow mainWindow)
            {
                mainWindow.NavigateTo("Daily Entry");
            }
        }

        // =====================================================
        // SAMPLE INVENTORY DATA
        // =====================================================

        private void LoadSampleProducts()
        {
            _products.Clear();

            // Weekly sample values are consistent with the
            // Dashboard's six-week trend.
            //
            // These are not database records.

            _products.Add(CreateSampleProduct(
                "Bottled Water",
                "Beverages",
                "Bottle (500 mL)",
                100,
                new DateTime(2026, 10, 1),
                new[] { 65, 68, 74, 78, 82, 84 },
                new[] { 120, 115, 110, 107, 104, 100 }));

            _products.Add(CreateSampleProduct(
                "Instant Noodles",
                "Food",
                "Pack",
                85,
                new DateTime(2026, 10, 2),
                new[] { 60, 64, 69, 73, 76, 72 },
                new[] { 105, 100, 95, 91, 88, 85 }));

            _products.Add(CreateSampleProduct(
                "Rice 5kg",
                "Food",
                "Bag (5 kg)",
                18,
                new DateTime(2026, 10, 3),
                new[] { 40, 42, 45, 47, 50, 52 },
                new[] { 55, 46, 39, 31, 25, 18 }));

            _products.Add(CreateSampleProduct(
                "Canned Tuna",
                "Food",
                "Can",
                12,
                new DateTime(2026, 10, 4),
                new[] { 25, 28, 30, 31, 32, 46 },
                new[] { 42, 36, 29, 24, 18, 12 }));

            _products.Add(CreateSampleProduct(
                "Cooking Oil",
                "Grocery",
                "Bottle",
                36,
                new DateTime(2026, 10, 5),
                new[] { 23, 25, 27, 29, 30, 30 },
                new[] { 62, 55, 50, 44, 40, 36 }));

            // A product without weekly sales records.
            // This demonstrates why All Products is useful.

            _products.Add(new InventoryProduct
            {
                ProductId = Guid.NewGuid(),
                Name = "Potato Chips",
                Category = "Snacks",
                SellingUnit = "Pack",
                CurrentStock = 20,
                DateAdded = new DateTime(2026, 11, 20),
                LastModified = new DateTime(2026, 11, 20)
            });
        }

        private static InventoryProduct CreateSampleProduct(
            string name,
            string category,
            string unit,
            int currentStock,
            DateTime dateAdded,
            int[] weeklySales,
            int[] weeklyStock)
        {
            InventoryProduct product = new InventoryProduct
            {
                ProductId = Guid.NewGuid(),
                Name = name,
                Category = category,
                SellingUnit = unit,
                CurrentStock = currentStock,
                DateAdded = dateAdded,
                LastModified = new DateTime(2026, 11, 22)
            };

            DateTime firstWeek =
                new DateTime(2026, 10, 12);

            for (int i = 0; i < weeklySales.Length; i++)
            {
                product.WeekRecords.Add(
                    new InventoryWeekRecord
                    {
                        WeekStart = firstWeek.AddDays(i * 7),
                        UnitsSold = weeklySales[i],
                        StockAtWeekEnd = weeklyStock[i]
                    });
            }

            return product;
        }
    }

    // =========================================================
    // PRODUCT MODEL - BACKEND / DATABASE HANDOFF
    // =========================================================

    public sealed class InventoryProduct
    {
        public Guid ProductId { get; set; }

        public string Name { get; set; } = "";

        public string Category { get; set; } = "";

        public string SellingUnit { get; set; } = "";

        public int CurrentStock { get; set; }

        public DateTime DateAdded { get; set; }

        public DateTime LastModified { get; set; }

        public bool IsArchived { get; set; }

        public List<InventoryWeekRecord> WeekRecords { get; set; }
            = new List<InventoryWeekRecord>();
    }

    // =========================================================
    // WEEKLY RECORD MODEL
    // =========================================================
    //
    // DATABASE TEAM:
    // Associate each record with its ProductId.
    // This frontend model nests records for convenience.
    //
    // UnitsSold:
    // Total units sold during the reporting week.
    //
    // StockAtWeekEnd:
    // Available stock recorded for that reporting week.
    //
    // Do not assume stock can be calculated solely by
    // subtracting sales; the paper relies on manual inputs.
    // =========================================================

    public sealed class InventoryWeekRecord
    {
        public DateTime WeekStart { get; set; }

        public int UnitsSold { get; set; }

        public int StockAtWeekEnd { get; set; }
    }

    // =========================================================
    // DISPLAY ROW - FRONTEND ONLY
    // =========================================================

    public sealed class InventoryTableRow
    {
        public InventoryProduct SourceProduct { get; set; }
            = new InventoryProduct();

        public string Product { get; set; } = "";

        public string Category { get; set; } = "";

        public string UnitsSoldText { get; set; } = "";

        public int Stock { get; set; }

        public string WeeklyAverageText { get; set; } = "";

        public string Status { get; set; } = "";

        public Brush StatusBackground { get; set; }
            = Brushes.Transparent;

        public Brush StatusForeground { get; set; }
            = Brushes.Black;

        public string DateAddedText { get; set; } = "";

        public string LastModifiedText { get; set; } = "";
    }
}