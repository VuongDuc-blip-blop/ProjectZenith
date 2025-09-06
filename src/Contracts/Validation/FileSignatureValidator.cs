namespace ProjectZenith.Contracts.Validation
{

    public class FileSignatureValidator : IFileSignatureValidator
    {
        // Defile magic bytes for each exetension
        private static readonly byte[] ZipSignature1 = { 0x50, 0x4B, 0x03, 0x04 };
        private static readonly byte[] ZipSignature2 = { 0x50, 0x4B, 0x05, 0x06 };
        private static readonly byte[] ZipSignature3 = { 0x50, 0x4B, 0x07, 0x08 };
        private static readonly List<byte[]> ZipSignatures = new() { ZipSignature1, ZipSignature2, ZipSignature3 };

        private static readonly Dictionary<string, List<byte[]>> FileSignatures = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".zip", ZipSignatures },
            { ".apk", ZipSignatures }, // .apk is a variant of .zip
            { ".msix", ZipSignatures }, // .msix is a variant of .zip
            { ".appx", ZipSignatures }  // .appx is a variant of .zip
            // { ".exe", new List<byte[]> { new byte[] { 0x4D, 0x5A } } } 
        };

        public async Task<bool> IsValidFileSignature(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
        {
            if (fileStream == null)
            {
                return false;
            }

            // Get extension (lowercase to avoid case-sensitive)
            var extension = Path.GetExtension(fileName);

            // Fail if the extension is not in the allowed list
            if (string.IsNullOrEmpty(extension) || !FileSignatures.ContainsKey(extension))
            {
                return false;
            }

            // Determine the maximum number of bytes we need to read
            var requiredSignatures = FileSignatures[extension];
            var maxSignatureLength = requiredSignatures.Max(s => s.Length);

            // Read only the required header bytes from the stream
            var headerBytes = new byte[maxSignatureLength];
            int bytesRead = 0, totalRead = 0;

            while (totalRead < maxSignatureLength &&
                   (bytesRead = await fileStream.ReadAsync(headerBytes, totalRead, maxSignatureLength - totalRead, cancellationToken)) > 0)
            {
                totalRead += bytesRead;
            }

            // Handle cases where the stream is shorter than the longest signature
            if (totalRead < maxSignatureLength)
            {
                Array.Resize(ref headerBytes, totalRead);
            }

            // Compare the sequence just read with the valid sequences
            return requiredSignatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }

    }
}
