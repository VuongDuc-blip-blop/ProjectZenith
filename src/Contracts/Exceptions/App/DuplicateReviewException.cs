namespace ProjectZenith.Contracts.Exceptions.App
{
    public class DuplicateReviewException : Exception
    {
        public DuplicateReviewException(string message) : base(message)
        {
        }
    }
}
