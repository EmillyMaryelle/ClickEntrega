using System;
using System.Threading.Tasks;
using ClickEntrega.Controllers;
using ClickEntrega.Data;
using ClickEntrega.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClickEntrega.Tests
{
    [TestClass]
    public class CompaniesControllerTests
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
        public async Task PostCompany_ReturnsCreatedCompany()
        {
            var context = GetDatabaseContext();
            var controller = new CompaniesController(context);
            var newCompany = new Company { Name = "Test Pizzeria", Type = "Pizzaria", Password = "123" };

            var result = await controller.PostCompany(newCompany);

            var actionResult = result.Result as CreatedAtActionResult;
            Assert.IsNotNull(actionResult);
            var company = actionResult.Value as Company;
            Assert.IsNotNull(company);
            Assert.AreEqual("Test Pizzeria", company.Name);
        }

        [TestMethod]
        public async Task PostCompany_ReturnsBadRequest_WhenNameExists()
        {
            var context = GetDatabaseContext();
            context.Company.Add(new Company { Name = "Existing Co", Type = "Other", Password = "123" });
            await context.SaveChangesAsync();

            var controller = new CompaniesController(context);
            var duplicateCompany = new Company { Name = "Existing Co", Type = "New", Password = "456" };

            var result = await controller.PostCompany(duplicateCompany);

            var actionResult = result.Result as BadRequestObjectResult;
            Assert.IsNotNull(actionResult);
            Assert.AreEqual("Nome de empresa já cadastrado.", actionResult.Value);
        }

        [TestMethod]
        public async Task Login_ReturnsCompany_WhenCredentialsAreCorrect()
        {
            var context = GetDatabaseContext();
            var company = new Company { Name = "Login Test", Type = "Burger", Password = "securepass" };
            context.Company.Add(company);
            await context.SaveChangesAsync();

            var controller = new CompaniesController(context);
            var loginRequest = new CompaniesController.LoginRequest { Name = "Login Test", Password = "securepass" };

            var result = await controller.Login(loginRequest);

            var actionResult = GetActionResultValue(result);
            Assert.IsNotNull(actionResult);
            Assert.AreEqual(company.Id, actionResult.Id);
        }

        [TestMethod]
        public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
        {
            var context = GetDatabaseContext();
            var controller = new CompaniesController(context);
            var loginRequest = new CompaniesController.LoginRequest { Name = "Unknown", Password = "wrong" };

            var result = await controller.Login(loginRequest);

            var actionResult = result.Result as UnauthorizedObjectResult;
            Assert.IsNotNull(actionResult);
        }
    }
}

