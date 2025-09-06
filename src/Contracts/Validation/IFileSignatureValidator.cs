namespace ProjectZenith.Contracts.Validation
{
    public interface IFileSignatureValidator
    {
        Task<bool> IsValidFileSignature(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    }
}
