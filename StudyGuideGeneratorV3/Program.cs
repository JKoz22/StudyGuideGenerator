using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronOcr;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.IO.Image;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;


namespace StudyGuideGeneratorV3
{
    class MyOcr
    {
        public OcrResult UseOcr(string FileLocation)
        {
            Console.WriteLine("Processing PDF");
            var Ocr = new AdvancedOcr()
            {
                CleanBackgroundNoise = true,
                EnhanceContrast = true,
                EnhanceResolution = true,
                Language = IronOcr.Languages.English.OcrLanguagePack,
                Strategy = IronOcr.AdvancedOcr.OcrStrategy.Advanced,
                ColorSpace = AdvancedOcr.OcrColorSpace.GrayScale,
                DetectWhiteTextOnDarkBackgrounds = false,
                InputImageType = AdvancedOcr.InputTypes.Document,
                RotateAndStraighten = false,
                ReadBarCodes = false,
                ColorDepth = 4
            };
            var Results = Ocr.ReadPdf(FileLocation, null);
            Console.WriteLine("PDF Processed");
            return Results;
        }

    }
    class AnalyzedPDF
    {
        /*public static List<int> PageDimensions(OcrResult result)
        {
            OcrResult.OcrPage page = result.Pages[0];
            List<int> MyList = new List<int>();
            MyList.Add(page.Width);
            MyList.Add(page.Height);
            return MyList;
        }*/
        public static List<OcrResult.OcrPage> GetPages(OcrResult Result)
        {
            List<OcrResult.OcrPage> MyPages = new List<OcrResult.OcrPage>();
            foreach(var page in Result.Pages)
            {
                MyPages.Add(page);
            }
            return MyPages;
        }
        public static List<OcrResult.OcrWord> CompareWordsToListPerPage(OcrResult.OcrPage MyPage)
        {
            HashSet<String> BannedWords = WordData.FileReaderToCompare();
            List<OcrResult.OcrWord> MyWords = new List<OcrResult.OcrWord>();
            foreach(var paragraph in MyPage.Paragraphs)
            {
                foreach (var line in paragraph.Lines)
                {
                    foreach(var word in line.Words)
                    {
                        if (!(BannedWords.Contains(word.Text.ToLower())))
                        {
                            MyWords.Add(word);
                        }

                    }
                }
            }
            return MyWords;
        }
        public static List<Bitmap> PageBitmaps(List<OcrResult.OcrPage> MyPages)
        {
            List<Bitmap> MyBitmaps = new List<Bitmap>();
            foreach(var page in MyPages)
            {
                System.Drawing.Image ThisPage = page.Image;
                Bitmap bmp = new Bitmap(ThisPage);
                MyBitmaps.Add(bmp);
            }
            return MyBitmaps;
        }
    }
    class PdfEditor
    {
        public static Bitmap ScaleToSize(Bitmap Bmp, int MyWidth, int MyHeight, Boolean Quality)
        {
            Double BmpWidth = Bmp.Width;
            Double BmpHeight = Bmp.Height;
            Double scale = Math.Min((Double)(MyWidth / BmpWidth), (Double)(MyHeight / BmpHeight));
            Console.WriteLine(scale);
            Bitmap resized = new Bitmap(Bmp, new Size((int)Math.Floor(Bmp.Width * scale), (int)Math.Floor(Bmp.Height * scale)));
            return resized;
        }
        public static List<Bitmap> ScaleBitmapPages(List<Bitmap> CoveredPages, int MyWidth, int MyHeight, Boolean Quality)
        {
            List<Bitmap> PdfPages = new List<Bitmap>();
            foreach(Bitmap bmp in CoveredPages)
            {
                Bitmap ScaledBmp = ScaleToSize(bmp, MyWidth, MyHeight, Quality);
                PdfPages.Add(ScaledBmp);
            }
            return PdfPages;
        }
        public static byte[] ImageToByte(System.Drawing.Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
        public static void BmpsToPdf(List<Bitmap> MyPages, string OcrFile,  int MyWidth, int MyHeight)
        {
            {
                /*Document doc = new Document(PageSize.A4);
                string imageFile = @"C:\Users\JustinKozlowski\image.pdf";
                PdfWriter.GetInstance(doc, new FileStream(imageFile, FileMode.Create));
                doc.Open();
                iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(image, System.Drawing.Imaging.ImageFormat.Jpeg);
                doc.Add(pdfImage);
                doc.Close();*/
            }
            //System.Drawing.Image image = System.Drawing.Image.FromFile(@"C:\Users\JustinKozlowski\MyBestPage");
            iText.Kernel.Geom.PageSize bmpSize = new iText.Kernel.Geom.PageSize(MyWidth, MyHeight);
            PdfWriter writer = new PdfWriter(OcrFile);
            PdfDocument OcrPdf = new PdfDocument(writer);
            Document document = new Document(OcrPdf, bmpSize);
            foreach (Bitmap MyBmp in MyPages)
            {
                byte[] imgBytes = ImageToByte(MyBmp);
                iText.IO.Image.ImageData imgData = iText.IO.Image.ImageDataFactory.Create(imgBytes);
                iText.Layout.Element.Image img = new iText.Layout.Element.Image(imgData);
                document.Add(img);
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }
            document.Close();
            writer.Close();
        }
        public static Bitmap CoverBmpWordsOfPage(List<OcrResult.OcrWord> Words, Bitmap bmp)
        {
            //string filename = @"C:\Users\JustinKozlowski\MyPage";
            //string filename1 = @"C:\Users\JustinKozlowski\MyBestPage";
            //System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(filename);
            foreach (OcrResult.OcrWord word in Words)
            {
                int WordX = word.X;
                int WordY = word.Y;
                int WordWidth = word.Width;
                int WordHeight = word.Height;
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(WordX, WordY, WordWidth, WordHeight);
                //Console.WriteLine("{0} {1} {2} {3}", WordX, WordY, WordWidth, WordHeight);
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    using (System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                    {
                        graphics.FillRectangle(myBrush, rect); // whatever
                                                               // and so on...
                    }
                    using (System.Drawing.Pen myPen = new System.Drawing.Pen(System.Drawing.Color.Red))
                    {
                        graphics.DrawRectangle(myPen, rect); // whatever
                                                                                    // and so on...
                    }
                }
            }
            return bmp;
        }

        /*public static void CoverWords(List<OcrResult.OcrWord> Words, List<int> dimensionsXY)
        {
            string oldFile = @"C:\Users\JustinKozlowski\NewMyPage.pdf";
            string newFile = @"C:\Users\JustinKozlowski\OcrTest1.pdf";
            PdfWriter writer = new PdfWriter(newFile);
            PdfReader reader = new PdfReader(oldFile);
            PdfDocument pdf = new PdfDocument(reader, writer);
            Document document = new Document(pdf);
            iText.Kernel.Geom.Rectangle pageSize;
            PdfCanvas canvas;
            try
            {
                //document.Open();
                //cb.SetColorFill(new CMYKColor(0f, 0f, 1f, 0f));
                PdfPage page = pdf.GetPage(1);
                pageSize = page.GetPageSize();
                canvas = new PdfCanvas(page);
                foreach(OcrResult.OcrWord word in Words)
                {
                    int WordX = word.X;
                    int WordY = dimensionsXY[1] - word.Y;
                    int WordWidth = word.Width;
                    int WordHeight = word.Height;
                    Console.WriteLine("{0} {1} {2} {3}", WordX, WordY, WordWidth, WordHeight);
                    iText.Kernel.Colors.Color greenColor = new DeviceCmyk(0f, 0f, 1f, 0f);
                    canvas.SetFillColor(greenColor);
                    canvas.MoveTo(WordX, WordY);
                    canvas.LineTo(WordX + WordWidth, WordY);
                    canvas.LineTo(WordX + WordWidth, WordY - WordHeight);
                    canvas.LineTo(WordX, WordY - WordHeight);
                    canvas.Fill();
                }
                //PdfImportedPage page = writer.GetImportedPage(reader, 1);
                //cb.AddTemplate(page, 0, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Editing PDF");
            }
            finally
            {
                document.Close();
                pdf.Close();
                writer.Close();
                reader.Close();
            }
        }*/
    }
        
            

    class WordData
    {
        public static HashSet<String> FileReaderToCompare()
        {
            string[] lines = System.IO.File.ReadAllLines(@"C:\Users\JustinKozlowski\source\repos\StudyGuideGeneratorV3\WordFrequencyLists\50k_sortedNoNums.txt");
            HashSet<String> FrequentWords = new HashSet<String>();
            foreach (string line in lines)
            {
                FrequentWords.Add(line);
            }
            return FrequentWords;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Input file path:");
            string FileLocation = Console.ReadLine();  //@"C:\Users\JustinKozlowski\Figures.pdf"
            Console.WriteLine("Output file path:");
            string EndFileLocation = Console.ReadLine();  //@"C:\Users\JustinKozlowski\Final.pdf"
            int PdfWidth = 620;
            int PdfHeight = 877;
            MyOcr FirstOcr = new MyOcr();
            OcrResult FirstResult = FirstOcr.UseOcr(FileLocation);
            List<OcrResult.OcrPage> OcrPages = AnalyzedPDF.GetPages(FirstResult);
            List<Bitmap> OcrBmps = AnalyzedPDF.PageBitmaps(OcrPages);
            List<Bitmap> CoveredBitmaps = new List<Bitmap>();
            using (var ePages = OcrPages.GetEnumerator())
            using (var eBitmaps = OcrBmps.GetEnumerator())
            {
                int PageNum = 0;
                while (ePages.MoveNext() && eBitmaps.MoveNext())
                {
                    PageNum += 1;
                    var Page = ePages.Current;
                    var Image = eBitmaps.Current;
                    List<OcrResult.OcrWord> MyWords = AnalyzedPDF.CompareWordsToListPerPage(Page);
                    foreach(var word in MyWords)
                    {
                        //Console.WriteLine(word.Text);
                    }
                    Bitmap Bmp = PdfEditor.CoverBmpWordsOfPage(MyWords, Image);
                    CoveredBitmaps.Add(Bmp);
                }
            }
            List<Bitmap> PdfDocument = PdfEditor.ScaleBitmapPages(CoveredBitmaps, PdfWidth, PdfHeight, true);
            PdfEditor.BmpsToPdf(PdfDocument, EndFileLocation, PdfWidth, PdfHeight);
            Console.WriteLine("Press any key to exit.");
            System.Console.ReadKey();
        }
    }
}
