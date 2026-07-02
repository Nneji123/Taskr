namespace API.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";
    public string Provider { get; set; } = "local";
    public S3StorageSettings S3 { get; set; } = new();
    public CloudinarySettings Cloudinary { get; set; } = new();
    public LocalStorageSettings Local { get; set; } = new();
}

public class S3StorageSettings
{
    public string BucketName { get; set; } = "";
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string ServiceUrl { get; set; } = "";
}

public class CloudinarySettings
{
    public string CloudName { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    public string Folder { get; set; } = "taskr";
}

public class LocalStorageSettings
{
    public string BasePath { get; set; } = "uploads";
    public string BaseUrl { get; set; } = "/uploads";
}
