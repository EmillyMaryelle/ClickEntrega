using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ClickEntrega.Data;
using ClickEntrega.Services;
using System.Text.Json.Serialization;

namespace ClickEntrega
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("ClickEntregaContext") 
                ?? throw new InvalidOperationException("Connection string 'ClickEntregaContext' not found.");

            builder.Services.AddDbContext<ClickEntregaContext>(options =>
                options.UseNpgsql(connectionString)
            );

            builder.Services.AddSingleton<IMessageBusService, FakeMessageBusService>();

            builder.Services.AddControllers()
                .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.CustomSchemaIds(type => type.FullName.Replace("+", "."));
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ClickEntregaContext>();
                db.Database.EnsureCreated();
            }

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

            app.MapControllers();

            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}
