namespace Application.Services.Abstractions;

public interface IUpdateSlotsUseCase
{
    Task RemoveExpiredAndCreateNewSlotsAsync(CancellationToken ct = default);
}