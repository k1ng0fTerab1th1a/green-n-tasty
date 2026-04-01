namespace Restaurant.Core.DTOs;

public record FileUploadDto(Stream Content, string ContentType, long Size);