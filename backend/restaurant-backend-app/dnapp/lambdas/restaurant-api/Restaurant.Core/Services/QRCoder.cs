using QRCoder;

namespace Restaurant.Core.Services;

public class QRCoder
{
    public byte[] GenerateQrCode(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(pixelsPerModule: 10);
    }
}