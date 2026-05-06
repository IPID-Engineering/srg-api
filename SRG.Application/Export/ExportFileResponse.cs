namespace SRG.Application.Export;

public record ExportFileResponse(string FileName, string ContentType, byte[] Content);
