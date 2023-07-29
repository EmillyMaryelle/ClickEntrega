using ApiProductsRest.Migrations;
using ApiProductsRest.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
namespace AMappercoesAPI.Controllers
{
    public class DataController : ApiProductsRest.Migrations.Controller
    {
       
        public IActionResult Edit(Mapper entity)
        {
            MongoDbContext dbContext = new MongoDbContext();
            dbContext.Mapper.ReplaceOne(m => m.Id == entity.Id, entity);
            return View(entity);
        }

        private IActionResult View(Mapper entity)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public IActionResult Add()
        {
            return Views();
        }

        private IActionResult Views()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public IActionResult Delete(Guid id)
        {
            MongoDbContext dbContext = new MongoDbContext();
    
            return RedirectToAction("Index", "Mapper");
        }

        private IActionResult RedirectToAction(string v1, string v2)
        {
            throw new NotImplementedException();
        }
    }
}