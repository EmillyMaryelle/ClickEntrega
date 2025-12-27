using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClickEntrega.Controllers;
using ClickEntrega.Data;
using ClickEntrega.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ClickEntrega.Services;

namespace ClickEntrega.Tests
{
    public class FakeMessageBusService : IMessageBusService
    {
        public void PublishOrderNotification(int orderId, int clientId, string message, string? orderStatus = null) { }
        public void PublishOrderStatusChange(int orderId, int clientId, string status, DateTime? estimatedDeliveryTime = null) { }
    }

    [TestClass]
    public class MultiTenancyTests
    {
        private ClickEntregaContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<ClickEntregaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new ClickEntregaContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private T GetActionResultValue<T>(ActionResult<T> result)
        {
            if (result.Value != null) return result.Value;
            var objectResult = result.Result as ObjectResult;
            if (objectResult != null && objectResult.Value is T value) return value;
            return default;
        }

        [TestMethod]
        public async Task GetProducts_ReturnsOnlyCompanyProducts()
        {
            var context = GetDatabaseContext();
            var company1 = 1;
            var company2 = 2;
            var categoryId = 10;
            context.Category.Add(new Category { Id = categoryId, Name = "Test Cat", CompanyId = company1 });
            await context.SaveChangesAsync();

            context.Product.Add(new Product { Id = 1, Name = "P1", CompanyId = company1, Price = 10, CategoryId = categoryId });
            context.Product.Add(new Product { Id = 2, Name = "P2", CompanyId = company2, Price = 20, CategoryId = categoryId });
            context.Product.Add(new Product { Id = 3, Name = "P3", CompanyId = company1, Price = 30, CategoryId = categoryId });
            await context.SaveChangesAsync();

            var controller = new ProductsController(context);

            var result = await controller.GetProduct(companyId: company1);

            var products = GetActionResultValue(result) as IEnumerable<Product>;
            Assert.IsNotNull(products);
            Assert.AreEqual(2, products.Count());
            foreach(var p in products)
            {
                Assert.AreEqual(company1, p.CompanyId);
            }
        }

        [TestMethod]
        public async Task PostOrder_SetsCompanyId_FromProducts()
        {
            var context = GetDatabaseContext();
            var companyId = 99;
            var product = new Product { Id = 10, Name = "Pizza", CompanyId = companyId, Price = 50, StockQuantity = 10 };
            context.Product.Add(product);
            await context.SaveChangesAsync();

            var controller = new OrdersController(context, new FakeMessageBusService());
            var order = new Order
            {
                ClientId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 10, Quantity = 1 }
                },
                Delivery = new Delivery { Address = "Street 1" },
                Payment = new Payment { Method = PaymentMethod.CreditCard }
            };

            var result = await controller.PostOrder(order);

            var actionResult = result.Result as CreatedAtActionResult;
            Assert.IsNotNull(actionResult);
            var createdOrder = actionResult.Value as Order;
            Assert.IsNotNull(createdOrder);
            
            Assert.AreEqual(companyId, createdOrder.CompanyId);
        }

        [TestMethod]
        public async Task GetOrders_ReturnsOnlyCompanyOrders()
        {
            var context = GetDatabaseContext();
            var company1 = 10;
            var company2 = 20;
            var clientId = 1;
            context.Client.Add(new Client { Id = clientId, Name = "Client 1", Email = "c1@test.com", Password = "p1" });
            await context.SaveChangesAsync();

            context.Order.Add(new Order { Id = 1, CompanyId = company1, OrderDate = DateTime.Now, ClientId = clientId });
            context.Order.Add(new Order { Id = 2, CompanyId = company2, OrderDate = DateTime.Now, ClientId = clientId });
            context.Order.Add(new Order { Id = 3, CompanyId = company1, OrderDate = DateTime.Now, ClientId = clientId });
            await context.SaveChangesAsync();

            var controller = new OrdersController(context, new FakeMessageBusService());

            var result = await controller.GetOrder(companyId: company1);

            var orders = GetActionResultValue(result) as IEnumerable<Order>;
            Assert.IsNotNull(orders);
            Assert.AreEqual(2, orders.Count());
            foreach (var o in orders)
            {
                Assert.AreEqual(company1, o.CompanyId);
            }
        }
    }
}

