using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Configuration;

namespace PrintingAppQR
{
    public class RestaurantReceipt
    {
        public string RestaurantName { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime Date { get; set; }
        public List<MenuItem> Items { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class MenuItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class ReceiptPrinter
    {
        private Font printFont;
        private StringBuilder receiptContent;

        public ReceiptPrinter()
        {
            printFont = new Font("Courier New", 8);
            receiptContent = new StringBuilder();
        }

        public void PrintReceipt(Products receipt, string printerName)
        {
            PrintDocument printDoc = new PrintDocument();
            //printDoc.PrinterSettings.PrinterName = printerName;
            if (!printDoc.PrinterSettings.IsValid)
                throw new Exception("Can't find the default printer.");
            else
            {
                printDoc.PrintPage += (sender, e) => PrintPageHandler(sender, e, receipt);
                printDoc.Print();
            }
        }

        private void PrintPageHandler(object sender, PrintPageEventArgs e, Products receipt)
        {
            var restaurantName = receipt.Tables[0].Rows[0]["RestaurantName"].ToString();
            var address = receipt.Tables[0].Rows[0]["RestaurantAddress1"].ToString();
            var phoneNumber = receipt.Tables[0].Rows[0]["PhoneNo"].ToString();
            var receiptType = receipt.Tables[0].Rows[0]["ReceiptType"].ToString();
            var orderDate = receipt.Tables[0].Rows[0]["OrderDate"].ToString();
            var orderNumber = receipt.Tables[0].Rows[0]["OrderNo"].ToString();
            var waiterName = receipt.Tables[0].Rows[0]["WaiterName"].ToString();
            var orderType = receipt.Tables[0].Rows[0]["OrderType"].ToString();
            var powBy = "Powered By Strawberry Solutions Pvt Ltd";
            var contact = "0345-6786005";

            float yPos = 0;
            float topMargin = 0;
            float receiptWidth = e.MarginBounds.Width;
            float leftMargin = (e.MarginBounds.Width - receiptWidth) / 2;

            using (var titleFont = new Font("Times New Roman", 10, FontStyle.Bold))
            using (var itemFont = new Font("Times New Roman", 8, FontStyle.Regular))
            {
                string dashedLine = new string('-', 150);
                string doubleLine = new string('_', 150);

                yPos = topMargin;
                e.Graphics.DrawString(restaurantName, titleFont, Brushes.Black, 100 + leftMargin + (receiptWidth - e.Graphics.MeasureString(restaurantName, titleFont).Width) / 2, yPos);
                yPos += e.Graphics.MeasureString(restaurantName, titleFont).Height;

                e.Graphics.DrawString(address, itemFont, Brushes.Black, 100 + leftMargin + (receiptWidth - e.Graphics.MeasureString(address, itemFont).Width) / 2, yPos);
                yPos += e.Graphics.MeasureString(address, itemFont).Height;

                e.Graphics.DrawString(phoneNumber, itemFont, Brushes.Black, 100 + leftMargin + (receiptWidth - e.Graphics.MeasureString(phoneNumber, itemFont).Width) / 2, yPos);
                yPos += e.Graphics.MeasureString(phoneNumber, itemFont).Height;

                e.Graphics.DrawString(receiptType, itemFont, Brushes.Black, 100 + leftMargin + (receiptWidth - e.Graphics.MeasureString(receiptType, itemFont).Width) / 2, yPos);
                //yPos += e.Graphics.MeasureString(receiptType, itemFont).Height;

                yPos += itemFont.GetHeight() * 2;
                e.Graphics.DrawString($"Date: {orderDate}\t\t Time: {receipt.Tables[0].Rows[0]["Sliptime"]}", itemFont, Brushes.Black, leftMargin + 10, yPos);
                yPos += itemFont.GetHeight() * 2;

                e.Graphics.DrawString($"Order No: {orderNumber}\t\t Table: {receipt.Tables[0].Rows[0]["TableNo"]}", itemFont, Brushes.Black, leftMargin + 10, yPos);
                yPos += itemFont.GetHeight() * 2;

                e.Graphics.DrawString($"Waiter: {waiterName}\t\t Payment: {receipt.Tables[0].Rows[0]["PaymentType"]} ", itemFont, Brushes.Black, leftMargin + 10, yPos);
                yPos += itemFont.GetHeight() * 2;

                e.Graphics.DrawString(orderType, itemFont, Brushes.Black, leftMargin + 100, yPos);
                yPos += itemFont.Height * 2;

                e.Graphics.DrawString(dashedLine, itemFont, Brushes.Black, 0, yPos);
                yPos += itemFont.GetHeight();


                // Calculate the maximum width needed for the options column
                float maxOptionsWidth = 0;
                for (int i = 0; i < receipt.Tables[0].Rows.Count; i++)
                {
                    var option1Desc1 = receipt.Tables[0].Rows[i]["Option1Desc1"].ToString();
                    var option1Desc2 = receipt.Tables[0].Rows[i]["Option1Desc2"].ToString();
                    var option1DescA = receipt.Tables[0].Rows[i]["Option1DescA"].ToString();

                    string options = $"{option1Desc1}, {option1Desc2}, {option1DescA}";
                    float optionsWidth = e.Graphics.MeasureString(options, itemFont).Width;
                    maxOptionsWidth = Math.Max(maxOptionsWidth, optionsWidth);
                }

                // Adjust the column width based on the maximum options width
                float qtyColumnWidth = 30;
                float descriptionColumnWidth = 120;
                float optionsColumnWidth = Math.Min(maxOptionsWidth, receiptWidth - qtyColumnWidth - descriptionColumnWidth);
                float totalWidth = qtyColumnWidth + descriptionColumnWidth + optionsColumnWidth;

                // Table headers
                e.Graphics.DrawString("Qty", itemFont, Brushes.Black, leftMargin, yPos);
                e.Graphics.DrawString("Description", itemFont, Brushes.Black, leftMargin + qtyColumnWidth, yPos);
                //e.Graphics.DrawString("Option", itemFont, Brushes.Black, leftMargin + descriptionColumnWidth, yPos);
                e.Graphics.DrawString("Rate", itemFont, Brushes.Black, leftMargin + qtyColumnWidth + descriptionColumnWidth, yPos);
                e.Graphics.DrawString("Amount", itemFont, Brushes.Black, leftMargin + totalWidth + qtyColumnWidth + descriptionColumnWidth, yPos);
                yPos += itemFont.GetHeight();

                // Table separator
                e.Graphics.DrawLine(Pens.Black, leftMargin, yPos, e.PageBounds.Width, yPos);
                yPos += 2;

                for (int i = 0; i < receipt.Tables[0].Rows.Count; i++)
                {
                    var qty = receipt.Tables[0].Rows[i]["Qty"].ToString();
                    var description = receipt.Tables[0].Rows[i]["Description"].ToString();
                    var option1Desc1 = receipt.Tables[0].Rows[i]["Option1Desc1"].ToString();
                    var option1Desc2 = receipt.Tables[0].Rows[i]["Option1Desc2"].ToString();
                    var option1DescA = receipt.Tables[0].Rows[i]["Option1DescA"].ToString();
                    var Rate = receipt.Tables[0].Rows[i]["Rate"].ToString();
                    var Amount = receipt.Tables[0].Rows[i]["Amount"].ToString();

                    // Split the options into multiple lines if it exceeds the available space
                    var optionsLines = WrapText($"{option1Desc1}, {option1Desc2}, {option1DescA}", itemFont, optionsColumnWidth);

                    // Calculate the maximum number of lines for options
                    int maxLines = Math.Max(1, Math.Max(optionsLines.Count, Math.Max(qty.Split('\n').Length, description.Split('\n').Length)));

                    // Draw each line of the item in table format
                    for (int lineIndex = 0; lineIndex < maxLines; lineIndex++)
                    {
                        string qtyLine = GetLineValue(qty, lineIndex);
                        string descriptionLine = GetLineValue(description, lineIndex);
                        string optionsLine = GetLineValue(optionsLines, lineIndex);
                        string rateLine = GetLineValue(Rate, lineIndex);
                        string amountLine = GetLineValue(Amount, lineIndex);

                        // Draw items in table format
                        e.Graphics.DrawString(qtyLine, itemFont, Brushes.Black, leftMargin, yPos);
                        e.Graphics.DrawString(descriptionLine, itemFont, Brushes.Black, leftMargin + qtyColumnWidth, yPos);
                        if (optionsLine != "" || optionsLine != ",")
                        {
                            //e.Graphics.DrawString(optionsLine, itemFont, Brushes.Black, leftMargin + qtyColumnWidth + descriptionColumnWidth, yPos);
                        }
                        e.Graphics.DrawString(rateLine, itemFont, Brushes.Black, leftMargin + qtyColumnWidth + descriptionColumnWidth, yPos);
                        e.Graphics.DrawString(amountLine, itemFont, Brushes.Black, leftMargin + qtyColumnWidth + descriptionColumnWidth + totalWidth, yPos);
                        yPos += itemFont.GetHeight();
                    }
                }

                // Table bottom line
                e.Graphics.DrawLine(Pens.Black, leftMargin, yPos, e.PageBounds.Width, yPos);
                yPos += 2;


                //e.Graphics.DrawString(dashedLine, itemFont, Brushes.Black, 0, yPos);
                yPos += itemFont.GetHeight();

                StringFormat rightAlignFormat = new StringFormat();
                rightAlignFormat.Alignment = StringAlignment.Far;

                //e.Graphics.DrawString($"Subtotal: {receipt.Tables[0].Rows[0]["TotalAmount"]}", itemFont, Brushes.Black, 0, yPos);
                e.Graphics.DrawString($"Subtotal: {receipt.Tables[0].Rows[0]["TotalAmount"]}", itemFont, Brushes.Black, leftMargin + qtyColumnWidth + descriptionColumnWidth + totalWidth, yPos, rightAlignFormat);
                yPos += itemFont.GetHeight();

                var totamount = receipt.Tables[0].Rows[0]["TotalAmount"].ToString();
                var disper = receipt.Tables[0].Rows[0]["DiscountPer"].ToString();
                var discamount = "0";

                int totalAmount;
                int discountPercentage;
                int discountAmount = 0;

                if (int.TryParse(totamount, out totalAmount) && int.TryParse(disper, out discountPercentage))
                {
                    /*  discountAmount = (totalAmount * discountPercentage) / 100;
                      discamount = discountAmount.ToString();*/
                }
                //e.Graphics.DrawString($"Discount: {receipt.Tables[0].Rows[0]["DiscountPer"]} %", itemFont, Brushes.Black, 0, yPos);
                e.Graphics.DrawString($"Discount: ({receipt.Tables[0].Rows[0]["DiscountPer"]}%) {discamount} ", itemFont, Brushes.Black, leftMargin + qtyColumnWidth + descriptionColumnWidth + totalWidth, yPos, rightAlignFormat);
                yPos += itemFont.GetHeight();

                e.Graphics.DrawString($"Tax: ({receipt.Tables[0].Rows[0]["TaxPer"]}%) {receipt.Tables[0].Rows[0]["TaxAmount"]}", itemFont, Brushes.Black, leftMargin + qtyColumnWidth + descriptionColumnWidth + totalWidth, yPos, rightAlignFormat);
                yPos += itemFont.GetHeight();

                e.Graphics.DrawString(dashedLine, itemFont, Brushes.Black, 0, yPos);
                yPos += itemFont.GetHeight();

                e.Graphics.DrawString($"Total: {receipt.Tables[0].Rows[0]["NetBill"]}", itemFont, Brushes.Black, leftMargin + qtyColumnWidth + descriptionColumnWidth + totalWidth, yPos, rightAlignFormat);
                yPos += itemFont.GetHeight();

                e.Graphics.DrawString(powBy, itemFont, Brushes.Black, 150 + leftMargin + (receiptWidth - e.Graphics.MeasureString(powBy, titleFont).Width) / 2, yPos + itemFont.GetHeight() * 2);
                yPos += itemFont.GetHeight();

                e.Graphics.DrawString(contact, itemFont, Brushes.Black, 100 + leftMargin + (receiptWidth - e.Graphics.MeasureString(contact, titleFont).Width) / 2, yPos + itemFont.GetHeight() * 2);
            }
        }

        private List<string> WrapText(string text, Font font, float width)
        {
            List<string> wrappedLines = new List<string>();
            string[] words = text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder currentLine = new StringBuilder();
            float currentWidth = 0;

            foreach (string word in words)
            {
                float wordWidth = word.Length * font.Size; // Estimate width based on the number of characters

                if (currentWidth + wordWidth <= width)
                {
                    currentLine.Append(word + " ");
                    currentWidth += wordWidth;
                }
                else
                {
                    wrappedLines.Add(currentLine.ToString().TrimEnd());
                    currentLine.Clear();
                    currentLine.Append(word + " ");
                    currentWidth = wordWidth;
                }
            }

            if (currentLine.Length > 0)
            {
                wrappedLines.Add(currentLine.ToString().TrimEnd());
            }

            return wrappedLines;
        }

        private string GetLineValue(string text, int lineIndex)
        {
            string[] lines = text.Split('\n');
            if (lineIndex >= 0 && lineIndex < lines.Length)
            {
                return lines[lineIndex];
            }
            return string.Empty;
        }

        private string GetLineValue(List<string> lines, int lineIndex)
        {
            if (lineIndex >= 0 && lineIndex < lines.Count)
            {
                return lines[lineIndex];
            }
            return string.Empty;
        }



        //public void PrintReceipt(Products receipt, string printerName)
        //{
        //    var restaurantName = receipt.Tables[0].Rows[0]["RestaurantName"].ToString();
        //    var address = receipt.Tables[0].Rows[0]["RestaurantAddress1"].ToString();
        //    var phoneNumber = receipt.Tables[0].Rows[0]["PhoneNo"].ToString();
        //    var receiptType = receipt.Tables[0].Rows[0]["ReceiptType"].ToString();
        //    var orderDate = receipt.Tables[0].Rows[0]["OrderDate"].ToString();
        //    var orderNumber = receipt.Tables[0].Rows[0]["OrderNo"].ToString();
        //    var waiterName = receipt.Tables[0].Rows[0]["WaiterName"].ToString();
        //    var orderType = receipt.Tables[0].Rows[0]["OrderType"].ToString();
        //    var powBy = "Powered By Strawberry Solutions Pvt Ltd";
        //    var contact = "0345-6786005";

        //    var receiptContent = new StringBuilder();

        //    receiptContent.AppendLine(restaurantName);
        //    receiptContent.AppendLine(address);
        //    receiptContent.AppendLine(phoneNumber);
        //    receiptContent.AppendLine(receiptType);
        //    receiptContent.AppendLine($"Date: {orderDate}\t\t Time: {receipt.Tables[0].Rows[0]["Sliptime"]}");
        //    receiptContent.AppendLine($"Order No: {orderNumber}\t Table: {receipt.Tables[0].Rows[0]["TableNo"]}");
        //    receiptContent.AppendLine($"Waiter: {waiterName}\t\t Payment: CASH");
        //    receiptContent.AppendLine(orderType);
        //    receiptContent.AppendLine("-----------------------------");
        //    receiptContent.AppendLine("-----------------------------");
        //    receiptContent.AppendLine("Description               Qty");

        //    for (int i = 0; i < receipt.Tables[0].Rows.Count; i++)
        //    {
        //        var qty = receipt.Tables[0].Rows[i]["Qty"];
        //        var description = receipt.Tables[0].Rows[i]["Description"];
        //        var option1Desc1 = receipt.Tables[0].Rows[i]["Option1Desc1"];
        //        var option1Desc2 = receipt.Tables[0].Rows[i]["Option1Desc2"];
        //        var option1DescA = receipt.Tables[0].Rows[i]["Option1DescA"];

        //        receiptContent.AppendLine($"{qty} - {description} - {option1Desc1},{option1Desc2},{option1DescA}");
        //    }

        //    receiptContent.AppendLine("-----------------------------");
        //    receiptContent.AppendLine($"Subtotal: {receipt.Tables[0].Rows[0]["NetBill"]}");
        //    receiptContent.AppendLine($"Tax: {receipt.Tables[0].Rows[0]["TaxAmount"]}");
        //    receiptContent.AppendLine("-----------------------------");
        //    receiptContent.AppendLine($"Total: {receipt.Tables[0].Rows[0]["Amount"]}");
        //    receiptContent.AppendLine(powBy);
        //    receiptContent.AppendLine(contact);

        //    PrintDocument printDoc = new PrintDocument();
        //    printDoc.PrinterSettings.PrinterName = printerName;
        //    printDoc.PrintPage += (sender, e) => PrintPageHandler(sender, e, receiptContent.ToString());
        //    printDoc.Print();
        //}

        //private void PrintPageHandler(object sender, PrintPageEventArgs e, string content)
        //{
        //    float yPos = 0;
        //    float topMargin = 0;
        //    float receiptWidth = e.MarginBounds.Width;
        //    float leftMargin = (e.MarginBounds.Width - receiptWidth) / 2;

        //    using (var titleFont = new Font("Times New Roman", 10, FontStyle.Bold))
        //    using (var itemFont = new Font("Times New Roman", 8, FontStyle.Regular))
        //    {
        //        string dashedLine = new string('-', 150);
        //        string doubleLine = new string('_', 50);

        //        string[] contentParts = content.Split('\n');

        //        yPos = topMargin;
        //        e.Graphics.DrawString(contentParts[0], titleFont, Brushes.Black, 100 + leftMargin + (receiptWidth - e.Graphics.MeasureString(contentParts[0], titleFont).Width) / 2, yPos);
        //        yPos += e.Graphics.MeasureString(contentParts[0], titleFont).Height;

        //        for (int i = 1; i < 4; i++)
        //        {
        //            e.Graphics.DrawString(contentParts[i], itemFont, Brushes.Black, 100 + leftMargin + (receiptWidth - e.Graphics.MeasureString(contentParts[i], itemFont).Width) / 2, yPos);
        //            yPos += e.Graphics.MeasureString(contentParts[i], itemFont).Height;
        //        }

        //        e.Graphics.DrawString(doubleLine, itemFont, Brushes.Black, 0, yPos);
        //        yPos += itemFont.GetHeight() * 2;

        //        e.Graphics.DrawString(contentParts[4], itemFont, Brushes.Black, leftMargin + 50, yPos);
        //        yPos += itemFont.GetHeight() * 2;

        //        e.Graphics.DrawString(contentParts[5], itemFont, Brushes.Black, leftMargin + 50, yPos);
        //        yPos += itemFont.GetHeight() * 2;

        //        e.Graphics.DrawString(contentParts[6], itemFont, Brushes.Black, leftMargin + 50, yPos);
        //        yPos += itemFont.GetHeight() * 2;

        //        e.Graphics.DrawString(contentParts[7], itemFont, Brushes.Black, leftMargin + 100, yPos);
        //        yPos += itemFont.Height * 2;

        //        float maxContentWidth = Math.Max(
        //            TextRenderer.MeasureText("Qty", titleFont).Width,
        //            Math.Max(
        //                TextRenderer.MeasureText("Item", titleFont).Width,
        //                TextRenderer.MeasureText("Option", titleFont).Width
        //            )
        //        );

        //        leftMargin = (receiptWidth - maxContentWidth) / 2;
        //        e.Graphics.DrawString("Qty", titleFont, Brushes.Black, leftMargin, yPos);
        //        e.Graphics.DrawString("Item", titleFont, Brushes.Black, leftMargin + 50, yPos);
        //        e.Graphics.DrawString("Option", titleFont, Brushes.Black, leftMargin + 150, yPos);
        //        yPos += titleFont.GetHeight();

        //        e.Graphics.DrawString(dashedLine, itemFont, Brushes.Black, 0, yPos);
        //        yPos += itemFont.GetHeight();

        //        int startLoopIndex = Array.FindIndex(contentParts, element => element.Contains("Description               Qty")) + 1;
        //        //int endLoopIndex = Array.IndexOf(contentParts, "Subtotal:") - 2;
        //        int endLoopIndex = Array.FindIndex(contentParts, element => element.Contains("Subtotal:")) - 2;

        //        for (int i = startLoopIndex; i <= endLoopIndex; i++)
        //        {
        //            var itemLine = contentParts[i].Split('-');
        //            var itemQty = itemLine[0];
        //            var itemName = itemLine[1];
        //            var option = itemLine[2].Trim();

        //            //e.Graphics.DrawString(itemQty, itemFont, Brushes.Black, leftMargin, yPos);
        //            //e.Graphics.DrawString(itemName, itemFont, Brushes.Black, leftMargin + 50, yPos);
        //            //e.Graphics.DrawString(option, itemFont, Brushes.Black, leftMargin + 150, yPos);
        //            //e.Graphics.DrawString(option, itemFont, Brushes.Black, 150 + leftMargin + (receiptWidth - e.Graphics.MeasureString(option, itemFont).Width) / 2, yPos);

        //            float optionXPos = 150 + leftMargin;
        //            float optionWidth = receiptWidth - optionXPos;
        //            SizeF optionSize = e.Graphics.MeasureString(option, itemFont, (int)optionXPos);

        //            if (optionSize.Width > receiptWidth)
        //            {
        //                var options = WrapText(option, itemFont, optionWidth);
        //                float optionsYPos = yPos;

        //                e.Graphics.DrawString(itemQty, itemFont, Brushes.Black, leftMargin, yPos);
        //                e.Graphics.DrawString(itemName, itemFont, Brushes.Black, leftMargin + 50, yPos);

        //                foreach (var opt in options)
        //                {
        //                    if (opt != "")
        //                    {
        //                        e.Graphics.DrawString(opt, itemFont, Brushes.Black, optionXPos, optionsYPos);
        //                        optionsYPos += itemFont.GetHeight();
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                e.Graphics.DrawString(itemQty, itemFont, Brushes.Black, leftMargin, yPos);
        //                e.Graphics.DrawString(itemName, itemFont, Brushes.Black, leftMargin + 50, yPos);
        //                e.Graphics.DrawString(option, itemFont, Brushes.Black, optionXPos, yPos);
        //            }



        //            yPos += itemFont.GetHeight();
        //        }

        //        e.Graphics.DrawString(dashedLine, itemFont, Brushes.Black, 0, yPos);

        //        string subtotalLabel = contentParts[contentParts.Length - 7];
        //        string taxLabel = contentParts[contentParts.Length - 6];
        //        string totalLabel = contentParts[contentParts.Length - 4];

        //        yPos += itemFont.GetHeight() * 2;

        //        e.Graphics.DrawString(subtotalLabel, itemFont, Brushes.Black, 0, yPos);
        //        e.Graphics.DrawString(taxLabel, itemFont, Brushes.Black, 0, yPos + itemFont.GetHeight());
        //        e.Graphics.DrawString(totalLabel, titleFont, Brushes.Black, 0, yPos + itemFont.GetHeight() * 2);

        //        yPos += itemFont.GetHeight() * 2;

        //        e.Graphics.DrawString(contentParts[contentParts.Length - 3], itemFont, Brushes.Black, 100 + leftMargin + (receiptWidth - e.Graphics.MeasureString(contentParts[contentParts.Length - 3], titleFont).Width) / 2, yPos + itemFont.GetHeight() * 2);

        //        yPos += itemFont.GetHeight();
        //        e.Graphics.DrawString(contentParts[contentParts.Length - 2], itemFont, Brushes.Black, 100 + leftMargin + (receiptWidth - e.Graphics.MeasureString(contentParts[contentParts.Length - 2], titleFont).Width) / 2, yPos + itemFont.GetHeight() * 2);
        //    }
        //}

        //private List<string> WrapText(string text, Font font, float width)
        //{
        //    List<string> wrappedLines = new List<string>();
        //    string[] words = text.Split(',');
        //    StringBuilder currentLine = new StringBuilder();
        //    float currentWidth = 0;

        //    foreach (string word in words)
        //    {
        //        float wordWidth = TextRenderer.MeasureText(word + " ", font).Width;

        //        if (currentWidth + wordWidth <= width)
        //        {
        //            currentLine.Append(word + " ");
        //            currentWidth += wordWidth;
        //        }
        //        else
        //        {
        //            wrappedLines.Add(currentLine.ToString().Trim());
        //            currentLine.Clear();
        //            currentLine.Append(word + " ");
        //            currentWidth = wordWidth;
        //        }
        //    }

        //    if (currentLine.Length > 0)
        //    {
        //        wrappedLines.Add(currentLine.ToString().Trim());
        //    }

        //    return wrappedLines;
        //}



        public string WriteLog(string strLog)
        {
            StreamWriter log;
            FileStream fileStream = null;
            DirectoryInfo logDirInfo = null;
            FileInfo logFileInfo;

            string logFilePath = ConfigurationManager.AppSettings["LogFilePath"];
            logFilePath = logFilePath + "Log-" + System.DateTime.Today.ToString("MM-dd-yyyy") + "." + "txt";
            logFileInfo = new FileInfo(logFilePath);
            logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
            if (!logDirInfo.Exists) logDirInfo.Create();
            if (!logFileInfo.Exists)
            {
                fileStream = logFileInfo.Create();
            }
            else
            {
                fileStream = new FileStream(logFilePath, FileMode.Append);
            }
            log = new StreamWriter(fileStream);
            log.WriteLine(strLog);
            log.Close();
            return "CLOSED";
        }

    }

}
