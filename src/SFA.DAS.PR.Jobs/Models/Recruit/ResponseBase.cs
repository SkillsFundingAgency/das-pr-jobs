namespace SFA.DAS.PR.Jobs.Models.Recruit;

public enum ResponseCode
{
    Success,
    InvalidRequest,
    NotFound,
    Created
}

public class DetailedValidationError
{
    public string? Field { get; set; }
    public string? Message { get; set; }
}
