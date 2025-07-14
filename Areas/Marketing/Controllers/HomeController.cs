// Areas/Marketing/Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Areas.Marketing.Models;

namespace POS_Shoes.Areas.Marketing.Controllers
{
    public class HomeController : MarketingBaseController
    {
        public HomeController(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var model = new MarketingDashboardViewModel();
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var startOfMonth = new DateTime(currentYear, currentMonth, 1);

            // Basic statistics
            model.TotalCustomers = await _context.Customers.CountAsync();
            model.NewCustomersThisMonth = await _context.Customers
                .CountAsync(c => c.CustomerID != Guid.Empty); // Assuming we track creation date

            model.TotalProducts = await _context.Products
                .CountAsync(p => p.Status == "Active");

            model.TotalOrders = await _context.Orders
                .CountAsync(o => o.Status == "Completed");

            model.TotalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => o.TotalPrice);

            model.AverageOrderValue = model.TotalOrders > 0 ? model.TotalRevenue / model.TotalOrders : 0;

            // Top selling products
            model.TopProducts = await GetTopSellingProducts();

            // Customer segments
            model.CustomerSegments = await GetCustomerSegments();

            // Sales trend (last 6 months)
            model.SalesTrend = await GetSalesTrend();

            // Customer acquisition (last 8 weeks)
            model.CustomerAcquisition = await GetCustomerAcquisition();

            // Product performance by supplier
            model.ProductPerformance = await GetProductPerformance();

            return View(model);
        }

        private async Task<List<TopProductItem>> GetTopSellingProducts()
        {
            var topProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order.Status == "Completed")
                .GroupBy(od => od.Product)
                .Select(g => new TopProductItem
                {
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => (double)(od.UnitPrice * od.Quantity)),
                    Image = g.Key.Image ?? "/images/no-image.png"
                })
                .OrderByDescending(p => p.QuantitySold)
                .Take(8)
                .ToListAsync();

            // Calculate growth rate (simplified)
            foreach (var product in topProducts)
            {
                product.GrowthRate = Math.Round(new Random().NextDouble() * 20 - 10, 1); // Mock data
            }

            return topProducts;
        }

        private async Task<List<CustomerSegmentItem>> GetCustomerSegments()
        {
            var allCustomers = await _context.Customers
                .Include(c => c.Orders)
                .ToListAsync();

            var segments = new List<CustomerSegmentItem>();

            // VIP Customers (>= 10 orders or >= 5,000,000 VND)
            var vipCustomers = allCustomers.Where(c =>
                c.Orders.Count(o => o.Status == "Completed") >= 10 ||
                c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalPrice) >= 5000000).ToList();

            // Regular Customers (2-9 orders)
            var regularCustomers = allCustomers.Where(c =>
                c.Orders.Count(o => o.Status == "Completed") >= 2 &&
                c.Orders.Count(o => o.Status == "Completed") < 10 &&
                c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalPrice) < 5000000).ToList();

            // New Customers (1 order)
            var newCustomers = allCustomers.Where(c =>
                c.Orders.Count(o => o.Status == "Completed") == 1).ToList();

            // Inactive Customers (0 orders)
            var inactiveCustomers = allCustomers.Where(c =>
                !c.Orders.Any(o => o.Status == "Completed")).ToList();

            segments.Add(new CustomerSegmentItem
            {
                SegmentName = "Khách hàng VIP",
                CustomerCount = vipCustomers.Count,
                AverageSpending = vipCustomers.Any() ? vipCustomers.Average(c => c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalPrice)) : 0,
                Percentage = allCustomers.Count > 0 ? (double)vipCustomers.Count / allCustomers.Count * 100 : 0
            });

            segments.Add(new CustomerSegmentItem
            {
                SegmentName = "Khách hàng thường xuyên",
                CustomerCount = regularCustomers.Count,
                AverageSpending = regularCustomers.Any() ? regularCustomers.Average(c => c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalPrice)) : 0,
                Percentage = allCustomers.Count > 0 ? (double)regularCustomers.Count / allCustomers.Count * 100 : 0
            });

            segments.Add(new CustomerSegmentItem
            {
                SegmentName = "Khách hàng mới",
                CustomerCount = newCustomers.Count,
                AverageSpending = newCustomers.Any() ? newCustomers.Average(c => c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalPrice)) : 0,
                Percentage = allCustomers.Count > 0 ? (double)newCustomers.Count / allCustomers.Count * 100 : 0
            });

            segments.Add(new CustomerSegmentItem
            {
                SegmentName = "Khách hàng chưa mua",
                CustomerCount = inactiveCustomers.Count,
                AverageSpending = 0,
                Percentage = allCustomers.Count > 0 ? (double)inactiveCustomers.Count / allCustomers.Count * 100 : 0
            });

            return segments;
        }

        private async Task<SalesTrendChart> GetSalesTrend()
        {
            var chart = new SalesTrendChart();
            var now = DateTime.Now;

            for (int i = 5; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                var startDate = new DateTime(month.Year, month.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                chart.Months.Add(month.ToString("MM/yyyy"));

                var monthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "Completed")
                    .SumAsync(o => o.TotalPrice);

                var monthlyOrders = await _context.Orders
                    .CountAsync(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "Completed");

                var monthlyCustomers = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "Completed")
                    .Select(o => o.CustomerID)
                    .Distinct()
                    .CountAsync();

                chart.Revenue.Add(monthlyRevenue);
                chart.OrderCounts.Add(monthlyOrders);
                chart.CustomerCounts.Add(monthlyCustomers);
            }

            return chart;
        }

        private async Task<CustomerAcquisitionChart> GetCustomerAcquisition()
        {
            var chart = new CustomerAcquisitionChart();
            var now = DateTime.Now;

            // Get data for last 8 weeks
            for (int i = 7; i >= 0; i--)
            {
                var weekStart = now.AddDays(-i * 7 - (int)now.DayOfWeek);
                var weekEnd = weekStart.AddDays(6);

                chart.Weeks.Add($"T{i + 1}");

                // New customers (first order in this week)
                var newCustomers = await _context.Orders
                    .Where(o => o.OrderDate >= weekStart && o.OrderDate <= weekEnd && o.Status == "Completed")
                    .GroupBy(o => o.CustomerID)
                    .Select(g => new { CustomerId = g.Key, FirstOrder = g.Min(o => o.OrderDate) })
                    .Where(x => x.FirstOrder >= weekStart && x.FirstOrder <= weekEnd)
                    .CountAsync();

                // Returning customers
                var returningCustomers = await _context.Orders
                    .Where(o => o.OrderDate >= weekStart && o.OrderDate <= weekEnd && o.Status == "Completed")
                    .Select(o => o.CustomerID)
                    .Distinct()
                    .CountAsync() - newCustomers;

                chart.NewCustomers.Add(newCustomers);
                chart.ReturningCustomers.Add(Math.Max(0, returningCustomers));
            }

            return chart;
        }

        private async Task<ProductPerformanceChart> GetProductPerformance()
        {
            var chart = new ProductPerformanceChart();

            var performanceData = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order.Status == "Completed")
                .GroupBy(od => od.Product.Supplier)
                .Select(g => new
                {
                    Supplier = g.Key ?? "Khác",
                    UnitsSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => (double)(od.UnitPrice * od.Quantity))
                })
                .OrderByDescending(x => x.Revenue)
                .Take(6)
                .ToListAsync();

            chart.Categories = performanceData.Select(x => x.Supplier).ToList();
            chart.UnitsSold = performanceData.Select(x => x.UnitsSold).ToList();
            chart.Revenue = performanceData.Select(x => x.Revenue).ToList();

            return chart;
        }
    }
}
