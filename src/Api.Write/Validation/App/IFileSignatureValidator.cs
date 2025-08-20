namespace ProjectZenith.Api.Write.Validation.App
{
    public interface IFileSignatureValidator
    {
        bool IsValidFileSignature(IFormFile file);
    }
}
