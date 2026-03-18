using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.EntityFrameworkCore;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDemoData(ApplicationDbContext context)
        {
            var random = new Random();

            // Seed Products if none exist
            if (!await context.Products.AnyAsync())
            {
                var products = new List<Product>
                {
                    new() { Name = "Classic Cotton T-Shirt", SKU = "TSH-COT-001", Price = 25.00m, StockQuantity = 100, Category = ProductCategory.TShirt, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                    new() { Name = "Formal Evening Dress", SKU = "DRS-EVE-002", Price = 120.00m, StockQuantity = 20, Category = ProductCategory.Dress, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                    new() { Name = "Winter Leather Jacket", SKU = "JKT-LEA-003", Price = 150.00m, StockQuantity = 15, Category = ProductCategory.Jacket, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                    new() { Name = "Woolen Sweater", SKU = "SWT-WOO-004", Price = 65.00m, StockQuantity = 30, Category = ProductCategory.Sweater, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-30) }
                };
                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            // Seed Customers if none exist
            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new() { FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone = "123-456-7890", Address = "123 Main St", CreatedAt = DateTime.UtcNow.AddDays(-60) },
                    new() { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", Phone = "098-765-4321", Address = "456 Oak Ave", CreatedAt = DateTime.UtcNow.AddDays(-60) }
                };
                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            // Seed Orders & OrderDetails for trends
            if (!await context.Orders.AnyAsync())
            {
                var customers = await context.Customers.ToListAsync();
                var products = await context.Products.ToListAsync();

                for (int i = 0; i < 30; i++)
                {
                    var date = DateTime.UtcNow.Date.AddDays(-i);
                    int ordersToday = random.Next(1, 4);

                    for (int j = 0; j < ordersToday; j++)
                    {
                        var order = new Order
                        {
                            OrderNumber = $"ORD-{date:yyyyMMdd}-{j}",
                            CustomerId = customers[random.Next(customers.Count)].Id,
                            OrderDate = date.AddHours(random.Next(8, 20)),
                            Status = OrderStatus.Delivered,
                            TotalAmount = 0,
                            FinalAmount = 0,
                            PaymentStatus = PaymentStatus.Paid,
                            TaxAmount = 0,
                            DiscountAmount = 0,
                            CreatedAt = date
                        };

                        context.Orders.Add(order);
                        await context.SaveChangesAsync();

                        int itemsCount = random.Next(1, 3);
                        decimal totalOrder = 0;
                        for (int k = 0; k < itemsCount; k++)
                        {
                            var prod = products[random.Next(products.Count)];
                            var qty = random.Next(1, 2);
                            var detail = new OrderDetail
                            {
                                OrderId = order.Id,
                                ProductId = prod.Id,
                                Quantity = qty,
                                UnitPrice = prod.Price,
                                TotalPrice = prod.Price * qty
                            };
                            totalOrder += detail.TotalPrice;
                            context.OrderDetails.Add(detail);
                        }
                        order.TotalAmount = totalOrder;
                        order.FinalAmount = totalOrder;
                    }
                }
                await context.SaveChangesAsync();
            }

            // Seed Tickets & Calls
            if (!await context.Tickets.AnyAsync())
            {
                var agents = await context.Users.Where(u => u.Role == UserRole.Agent).ToListAsync();
                var dbCustomers = await context.Customers.ToListAsync();
                if (agents.Count > 0 && dbCustomers.Count > 0)
                {
                    var agent = agents[0];
                    var customer = dbCustomers[0];
                    for (int i = 0; i < 10; i++)
                    {
                        var date = DateTime.UtcNow.AddDays(-random.Next(0, 14));
                        context.Tickets.Add(new Ticket
                        {
                            TicketNumber = $"TKT-{date:yyyyMMdd}-{i}",
                            CustomerId = customer.Id,
                            Title = $"Demo Issue {i}",
                            Description = "Sample support ticket.",
                            Status = (i % 3 == 0) ? TicketStatus.Closed : TicketStatus.Open,
                            Priority = TicketPriority.Medium,
                            AssignedAgentId = agent.Id,
                            CreatedAt = date
                        });
                    }
                    await context.SaveChangesAsync();
                }
            }
        }

        public static async Task PurgeDemoData(ApplicationDbContext context)
        {
            // Remove all excluding specific admin if needed, but here we just clear common tables
            context.OrderDetails.RemoveRange(context.OrderDetails);
            context.Orders.RemoveRange(context.Orders);
            context.Tickets.RemoveRange(context.Tickets);
            context.Calls.RemoveRange(context.Calls);
            context.Products.RemoveRange(context.Products);

            // Keep the SuperAdmin user, but we can clear other customers if they are just demo
            var demoCustomers = await context.Customers.ToListAsync();
            context.Customers.RemoveRange(demoCustomers);

            await context.SaveChangesAsync();
        }
    }
}
