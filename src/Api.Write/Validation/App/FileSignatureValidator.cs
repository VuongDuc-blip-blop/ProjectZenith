namespace ProjectZenith.Api.Write.Validation.App
{

    public class FileSignatureValidator : IFileSignatureValidator
    {
        // Defile magic bytes for each exetension
        private static readonly List<byte[]> ZipSignatures = new()
        {
            new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            new byte[] { 0x50, 0x4B, 0x05, 0x06 },
            new byte[] { 0x50, 0x4B, 0x07, 0x08 }
        };

        private static readonly Dictionary<string, List<byte[]>> FileSignatures = new()
        {
            { ".zip", ZipSignatures },
            { ".apk", ZipSignatures }, // .apk is the same as .zip
            { ".exe", new List<byte[]> { new byte[] { 0x4D, 0x5A } } }
        };

        public bool IsValidFileSignature(IFormFile file)
        {
            if (file == null || file.Length < 4)
            {
                return false;
            }

            // Get extension (lowercase to avoid case-sensitive)
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Fail if exetension is not in allowed list
            if (!FileSignatures.ContainsKey(extension))
            {
                return false;
            }

            using var reader = new BinaryReader(file.OpenReadStream());

            // Get the length of magic byte for the exetension
            var maxSignatureLength = FileSignatures[extension].Max(s => s.Length);

            // Read header byte corresponds to length
            var headerBytes = reader.ReadBytes(maxSignatureLength);

            // Reset the stream position
            file.OpenReadStream().Position = 0;

            // Compare sequence just read with the valid sequence 
            return FileSignatures[extension].Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }
    }
}
