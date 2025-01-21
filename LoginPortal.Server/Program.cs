using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;

// token initialization...
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// new modification to CORS package...
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// new modification to CORS package...
builder.Services.AddCors(options => {
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy => {
            policy.WithOrigins("http://www.tcsservices.com:40730",
                                "www.tcsservices.com:40730",
                                "tcsservices.com/40730",
                                "http://www.deliverymanager.tcsservices.com:40730",
                                "www.deliverymanager.tcsservices.com:40730",
                                "deliverymanager.tcsservices.com:40730",
                                "https://localhost:5173")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
        });
});

// Add services to the container.
builder.Services.AddControllers();

// token initialization...
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            //ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// var app = builder.Build();

// Adding Serializers, this is a new attempt...
// JSON Serializer
builder.Services.AddControllers().AddNewtonsoftJson(options =>
options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore).AddNewtonsoftJson(
    options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

builder.Services.AddScoped<TokenService>();

var app = builder.Build();

//builder.WebHost.UseUrls("http://*:80");

// Enable CORS (not suggested during production)
//app.UseCors(c => c.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

//app.UseAuthentication();

// new modification to CORS package...
//app.UseCors(MyAllowSpecificOrigins);

// End of updated attempts...

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

