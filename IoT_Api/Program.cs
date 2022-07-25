using IoT_Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

DB.Register(Environment.CurrentDirectory + "/App_Data");

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Actors.Account.RegisterActors("Actors", "Admin", "User");
Actors.Account.CreateAccount("admin", "admin", "Admin");

Mqtt.Start("broker.emqx.io", 1883);

//Models.DeviceData.Save(new Document { Data = 190, ObjectId = "1223", Unit = "oC" });

app.Run();
