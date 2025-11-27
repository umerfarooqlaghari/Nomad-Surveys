namespace Nomad.Api.Configuration;

public class EmailSettings
{
    public string Provider { get; set; } = "AWSSES";
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public AwsSettings AWS { get; set; } = new();
}

public class AwsSettings
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
}

