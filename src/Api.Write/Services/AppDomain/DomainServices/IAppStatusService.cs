namespace ProjectZenith.Api.Write.Services.AppDomain.DomainServices
{
    public interface IAppStatusService
    {
        Task UpdateAppStatusAsync(Guid appId, CancellationToken cancellationToken);
    }
}
