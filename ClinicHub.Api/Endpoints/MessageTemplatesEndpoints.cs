using ClinicHub.Core.Dtos;
using ClinicHub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Api.Endpoints;

/// <summary>CRUD шаблонов текста SMS (с плейсхолдерами {doctor}/{cabinet}/{date}/{time}/...).</summary>
public static class MessageTemplatesEndpoints
{
    public static IEndpointRouteBuilder MapMessageTemplatesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/message-templates")
            .WithTags("Шаблоны сообщений")
            .RequireAuthorization();

        group.MapGet("/", GetList);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);

        return app;
    }

    private static async Task<IResult> GetList(Data.ClinicHubDbContext db, bool? activeOnly)
    {
        var query = db.MessageTemplates.AsQueryable();
        if (activeOnly is true)
            query = query.Where(t => t.IsActive);

        var templates = await query.OrderBy(t => t.Name).ToListAsync();
        return Results.Ok(templates.Select(ToDto));
    }

    private static async Task<IResult> Create(
        MessageTemplateInputDto dto, Data.ClinicHubDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Text))
            return Results.BadRequest("Укажите название и текст шаблона.");

        var template = new MessageTemplate
        {
            Name = dto.Name.Trim(),
            Text = dto.Text,
            IsActive = dto.IsActive,
        };

        db.MessageTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/message-templates/{template.Id}", ToDto(template));
    }

    private static async Task<IResult> Update(
        int id, MessageTemplateInputDto dto, Data.ClinicHubDbContext db, CancellationToken ct)
    {
        var template = await db.MessageTemplates.FindAsync(new object?[] { id }, ct);
        if (template is null)
            return Results.NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Text))
            return Results.BadRequest("Укажите название и текст шаблона.");

        template.Name = dto.Name.Trim();
        template.Text = dto.Text;
        template.IsActive = dto.IsActive;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToDto(template));
    }

    private static async Task<IResult> Delete(int id, Data.ClinicHubDbContext db, CancellationToken ct)
    {
        var template = await db.MessageTemplates.FindAsync(new object?[] { id }, ct);
        if (template is null)
            return Results.NotFound();

        db.MessageTemplates.Remove(template);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static MessageTemplateDto ToDto(MessageTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Text = t.Text,
        IsActive = t.IsActive,
    };
}
