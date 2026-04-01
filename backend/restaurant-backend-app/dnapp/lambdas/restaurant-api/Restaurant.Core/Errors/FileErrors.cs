namespace Restaurant.Core.Errors;

public static class FileErrors
{
    public static BusinessError FileUploadFail =>
        new("Failed to upload", ErrorType.Infrastructure);

    public static BusinessError FileTooBig => 
        new("File too big", ErrorType.Validation);

    public static BusinessError InvalidFileType =>
       new("Invalid file type", ErrorType.Validation);
}
