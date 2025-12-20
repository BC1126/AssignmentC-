using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AssignmentC_.Controllers;

public class ReceiptDocument : IDocument
{
    private readonly PaymentVM _payment;

    public ReceiptDocument(PaymentVM payment)
    {
        _payment = payment;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var subtotal = 0;

        container.Page(page =>
        {
            page.Margin(20);
            page.Size(PageSizes.A4);

            page.Header().Text("Payment Receipt").SemiBold().FontSize(20).AlignCenter();

            page.Content().Column(col =>
            {
                // Movie info
                col.Item().Text($"Movie: {_payment.Booking.ShowTime.Movie.Title}");
                col.Item().Text($"Date: {_payment.Booking.BookingDate:yyyy-MM-dd}");
                col.Item().Text($"Time: {_payment.Booking.ShowTime.StartTime:HH:mm}");
                col.Item().Text($"Hall: {_payment.Booking.ShowTime.Hall.Name}");
                col.Item().Text($"Seats: ");

                col.Item().LineHorizontal(1);

                // Order details
                col.Item().Text("Order Details").Bold();
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Item");
                        header.Cell().Text("Qty");
                        header.Cell().Text("Price (RM)");
                    });

                    table.Cell().Text("Ticket");
                    table.Cell().Text(_payment.Booking.TicketQuantity.ToString());
                    table.Cell().Text(_payment.Booking.TotalPrice.ToString("F2"));

                    table.Cell().Text("Add On");
                    table.Cell().Text("-");
                    table.Cell().Text(_payment.Amount.ToString("F2"));

                    table.Cell().Text("Discount");
                    table.Cell().Text("-");
                    table.Cell().Text(_payment.Amount.ToString("F2"));
                });

                col.Item().Text($"Total: RM {_payment.Amount.ToString("F2")}")
                    .Bold().AlignRight();
            });

            page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm}");
        });
    }
}