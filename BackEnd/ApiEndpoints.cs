using BackEnd.DatabaseModels;
using BackEnd.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace BackEnd;

public static class ApiEndpoints
{
    
    public static void RegisterApiEndpoints(this WebApplication app)
    {
        app.MapGet("/search", GetEventSearch);
        //.RequireAuthorization("customer,organizer,admin");


        app.MapGet("/search/{id:int}", GetEventData);
        //.RequireAuthorization("customer,organizer,admin");

        app.MapPost("/purchase/{id:int}+{quantity:int}", PurchaseItem);
        //.RequireAuthorization("customer,organizer,admin");

        
        // Test organizer authentication
        app.MapGet("/organizer/", () =>
            {
                return Results.Ok("Welcome Organizer!");
            })
            .RequireAuthorization(policy => policy.RequireRole("organizer"));
        
        // Test admin authentication
        app.MapGet("/admin/", () =>
            {
                return Results.Ok("Welcome Admin!");
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"));
        
        
        app.MapGet("/admin/delete/{id}", () =>
            {
                return Results.Ok("Welcome Admin!");
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"));
    }


    // sends every event in database
    static async Task<Ok<List<Event>>> GetEventSearch(AppDbContext db) =>
        TypedResults.Ok(await db.Events.ToListAsync());
    
    
    
    // sends event data in database. depending on the id
    static async Task<Results<Ok<Event>, NotFound>> GetEventData(int id, AppDbContext db) =>
        await db.Events.FindAsync(id)
            is Event e
            ? TypedResults.Ok(e)
            : TypedResults.NotFound();


    static async Task<IResult> PurchaseItem(
        PurchaseItem request,
        AppDbContext db,
        IConfiguration config)
    {
        var events = await db.Events.FirstOrDefaultAsync(u => u.Id == request.ID);

        if (events == null)
            return Results.BadRequest();

        Decimal price = events.Price;
        Decimal totalTransaction = request.quantity * price;

        if (events.AvailableTickets > 0)
            events.AvailableTickets--;
        else
        {
            return Results.BadRequest();
        }

        db.Events.Update(events);
        int save = await db.SaveChangesAsync();
        
        // todo check if database transaction is valid
        return Results.Ok();
    }

}