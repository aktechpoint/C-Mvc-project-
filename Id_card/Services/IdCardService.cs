using Id_card.Models;
using iText.IO.Font.Constants;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;

namespace Id_card.Services
{
    public class IdCardService
    {
        public string GenerateQRCode(string data)
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            var imageBytes = qrCode.GetGraphic(20);
            return "data:image/png;base64," + Convert.ToBase64String(imageBytes);
        }

        public string BuildIdCardHtml(Employees employee, string qrCodeImage)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>ID Card - {employee.Name}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 40px;
            background: #f4f6f8;
        }}

        .id-card {{
            width: 420px;
            height: 280px;
            border-radius: 16px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            box-shadow: 0 10px 25px rgba(0,0,0,0.2);
            color: #fff;
            overflow: hidden;
            margin: 0 auto;
            position: relative;
        }}

        .header {{
            text-align: center;
            padding: 15px;
            background: rgba(255, 255, 255, 0.15);
            border-bottom: 2px solid rgba(255,255,255,0.3);
        }}

        .company-name {{
            font-size: 22px;
            font-weight: 700;
            letter-spacing: 1px;
            text-transform: uppercase;
        }}

        .card-title {{
            font-size: 13px;
            opacity: 0.9;
        }}

        .content {{
            display: flex;
            justify-content: space-between;
            padding: 15px 20px;
        }}

        .left {{
            flex: 1;
            display: flex;
            flex-direction: column;
            justify-content: center;
        }}

        .field {{
            margin-bottom: 7px;
            font-size: 13px;
            line-height: 1.4;
        }}

        .label {{
            font-weight: bold;
            color: #ffde59;
        }}

        .right {{
            flex: 0 0 120px;
            text-align: center;
        }}

        .photo {{
            width: 85px;
            height: 85px;
            border-radius: 50%;
            border: 3px solid #fff;
            object-fit: cover;
            box-shadow: 0 4px 8px rgba(0,0,0,0.3);
        }}

        .qr-section {{
            margin-top: 12px;
        }}

        .qr-code {{
            width: 70px;
            height: 70px;
            border: 2px solid white;
            padding: 3px;
            border-radius: 8px;
            background: #fff;
        }}

        .footer {{
            position: absolute;
            bottom: 10px;
            left: 0;
            width: 100%;
            text-align: center;
            font-size: 11px;
            color: #eee;
            letter-spacing: 0.5px;
            background: rgba(255, 255, 255, 0.1);
            padding: 6px 0;
            border-top: 1px solid rgba(255,255,255,0.2);
        }}
    </style>
</head>
<body>
    <div class='id-card'>
        <div class='header'>
            <div class='company-name'>iCard System</div>
            <div class='card-title'>Employee Identity Card</div>
        </div>

        <div class='content'>
            <div class='left'>
                <div class='field'><span class='label'>ID:</span> {employee.EmployeeId}</div>
                <div class='field'><span class='label'>Name:</span> {employee.Name}</div>
                <div class='field'><span class='label'>Dept:</span> {employee.Department}</div>
                <div class='field'><span class='label'>Designation:</span> {employee.Designation}</div>
                {(string.IsNullOrWhiteSpace(employee.BloodGroup) ? "" : $"<div class='field'><span class='label'>Blood Group:</span> {employee.BloodGroup}</div>")}
                <div class='field'><span class='label'>Mobile:</span> {employee.MobileNo}</div>
                <div class='field'><span class='label'>Email:</span> {employee.Email}</div>
                <div class='field'>
                    <span class='label'>Address:</span>
                    {(employee.Address == null ? "" :
                        ($"{employee.Address.HouseNo} {employee.Address.Street}, {employee.Address.City}, {employee.Address.State}, {employee.Address.Country} {employee.Address.Pincode}")
                        .Replace("  ", " ").Trim())}
                </div>
            </div>

            <div class='right'>
                <img src='{(string.IsNullOrEmpty(employee.Image) ? "/favicon.ico" : employee.Image)}' 
                     class='photo' alt='Employee Photo' />
                <div class='qr-section'>
                    <img src='{qrCodeImage}' class='qr-code' alt='QR Code' />
                </div>
            </div>
        </div>

        <div class='footer'>
            Valid Till: {employee.ValidTill?.ToString("MMM yyyy") ?? "N/A"} | 
            Issued: {employee.IDCardIssueDate?.ToString("MMM yyyy") ?? DateTime.Now.ToString("MMM yyyy")}
        </div>
    </div>
</body>
</html>";
        }

        public byte[] BuildIdCardPdfBytes(Employees employee, string qrCodeImage)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf);

            doc.Add(new Paragraph("iCard System")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetFontSize(16));
            doc.Add(new Paragraph("Employee Identity Card").SetTextAlignment(TextAlignment.CENTER).SetFontSize(12));

            var table = new Table(2).UseAllAvailableWidth();
            var left = new Cell().SetPadding(8);

            string addressLine = employee.Address == null ? "" : $"{employee.Address.HouseNo} {employee.Address.Street}, {employee.Address.City}, {employee.Address.State}, {employee.Address.Country} {employee.Address.Pincode}".Replace("  ", " ").Trim();
            left.Add(new Paragraph($"ID: {employee.EmployeeId}").SetFontSize(10));
            left.Add(new Paragraph($"Name: {employee.Name}").SetFontSize(10));
            left.Add(new Paragraph($"Dept: {employee.Department}").SetFontSize(10));
            left.Add(new Paragraph($"Designation: {employee.Designation}").SetFontSize(10));
            if (!string.IsNullOrWhiteSpace(employee.BloodGroup))
                left.Add(new Paragraph($"Blood Group: {employee.BloodGroup}").SetFontSize(10));
            left.Add(new Paragraph($"Mobile: {employee.MobileNo}").SetFontSize(10));
            left.Add(new Paragraph($"Email: {employee.Email}").SetFontSize(10));
            if (!string.IsNullOrWhiteSpace(addressLine))
                left.Add(new Paragraph($"Address: {addressLine}").SetFontSize(10));

            var right = new Cell().SetPadding(8);
            right.Add(new Paragraph("QR Code").SetFontSize(10));

            table.AddCell(left);
            table.AddCell(right);
            doc.Add(table);

            doc.Add(new Paragraph($"Issued: {(employee.IDCardIssueDate?.ToString("dd MMM yyyy") ?? DateTime.Now.ToString("dd MMM yyyy"))}    Valid Till: {(employee.ValidTill?.ToString("dd MMM yyyy") ?? "N/A")}" )
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(9)
                .SetMarginTop(10));

            doc.Close();
            return ms.ToArray();
        }
    }
}


