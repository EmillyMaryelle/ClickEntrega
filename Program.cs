using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ClickEntrega.Data;
using ClickEntrega.Services; // Add namespace
using System.Text.Json.Serialization;

namespace ClickEntrega
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("ClickEntregaContext") 
                ?? throw new InvalidOperationException("Connection string 'ClickEntregaContext' not found.");

            builder.Services.AddDbContext<ClickEntregaContext>(options =>
                options.UseNpgsql(connectionString)
            );

            // Add services to the container.
            // builder.Services.AddHostedService<OrderTimeoutService>(); // Register background service
            
            // RabbitMQ Services
            // Use FakeMessageBusService when RabbitMQ is not available
            builder.Services.AddSingleton<IMessageBusService, FakeMessageBusService>();
            // builder.Services.AddSingleton<IMessageBusService, MessageBusService>();
            // builder.Services.AddHostedService<NotificationConsumerService>();

            builder.Services.AddControllers()
                .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.CustomSchemaIds(type => type.FullName.Replace("+", "."));
            });

            var app = builder.Build();

            // Database initialization
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ClickEntregaContext>();
                // db.Database.EnsureDeleted(); // CUIDADO: Isso apaga o banco! Use apenas se quiser recriar do zero
                db.Database.EnsureCreated();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            // app.MapRazorPages();
            app.MapControllers();

            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}

