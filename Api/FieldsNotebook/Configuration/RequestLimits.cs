namespace FieldsNotebook.Configuration;

public static class RequestLimits
{
    // Five originals at 25 MiB plus five thumbnails at 2 MiB total 135 MiB.
    // Allow another 5 MiB for form fields and multipart overhead. One MiB is 1,048,576 bytes.
    public const long DelayPhotoUploadBytes = 140L * 1024 * 1024;
}
