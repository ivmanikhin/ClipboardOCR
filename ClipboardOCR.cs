using System;
using System.Collections.Generic;
using System.Linq;
//using System.Threading.Tasks;
using System.Windows.Forms;
using TesseractOCR;
using System.Text.RegularExpressions;
using System.Drawing;
//using System.Windows.Forms;
using System.IO;

namespace ClipboardOCR
{
    public class ClipboardOCR
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]

        static void Main(string[] args)
        {
            string lang = args[0].ToString();
            var bitmap = (Bitmap)Clipboard.GetImage();
            if (bitmap != null)
            {
                Bitmap horBitmap = TiltMessage(new Bitmap(bitmap));
                string result = XpsToText(horBitmap, lang);
                Clipboard.SetText(result);
            }
            else
                MessageBox.Show("В буфере обмена нет картинки");
        }


        static string XpsToText(Bitmap horBitmap, string lang)
        {
            //MessageBox.Show(lang.ToString());
            string messageText = "";
            using (var ocr = new Engine(@".\tessdata\", lang, TesseractOCR.Enums.EngineMode.Default))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    horBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    var image = TesseractOCR.Pix.Image.LoadFromMemory(ms);
                    using (var text = ocr.Process(image))
                    {
                        messageText = Regex.Replace(Regex.Replace(text.Text, @" +", " "), @"(?<!\d)-+[ +]?\n+[ +]?|(?<=[\d\wа-яА-я])\s+(?=\.)|(?<=\.)\s+(?=\d)", "");
                    }
                }

            }
            return Regex.Replace(messageText, @"\s+|\t+|\n+|\r+", " ");

            //File.WriteAllText(@"F:\TEMP\TEST\result_" + i.ToString() + ".txt", Regex.Replace(messageText, @"\s+|\t+|\n+|\r+", " "));
            
        }


        static Bitmap TiltMessage(Bitmap inputPic)
        {
            //Image inputPic = Image.FromFile(@"F:\VisualStudioProjects\repos\BitmapTextTilt\inputPic.png");
            Bitmap croppedPic = new Bitmap(inputPic.Width * 2 / 3, inputPic.Height);
            Bitmap horisontalPic = new Bitmap(inputPic.Width, inputPic.Height);
            int colImageHeight = 640;
            if (inputPic.Height < 640)
                colImageHeight = inputPic.Height;
            Bitmap shrinkPic = new Bitmap(colImageHeight * croppedPic.Width / croppedPic.Height, colImageHeight);
            int brightness = 255 * 3;
            int[] brightnessGrad = new int[colImageHeight];
            string log = "";
            Bitmap columnPic = new Bitmap(1, colImageHeight);
            Rectangle rect = new Rectangle((inputPic.Width - croppedPic.Width) / 2, (inputPic.Height - croppedPic.Height) / 2, (inputPic.Width + croppedPic.Width) / 2, (inputPic.Height + croppedPic.Height) / 2);
            croppedPic = inputPic.Clone(rect, inputPic.PixelFormat);
            using (Graphics gr1 = Graphics.FromImage(shrinkPic))
            {
                gr1.DrawImage(croppedPic, 0, 0, shrinkPic.Width, shrinkPic.Height);
            }
            Dictionary<float, int> angles = new Dictionary<float, int>();
            for (float angle = -10; angle < 10; angle += (float)0.25)
            {
                brightness = 255 * 3;
                Bitmap rotatedPic = new Bitmap(shrinkPic.Width, shrinkPic.Height);
                //Bitmap shrinkedPic = new Bitmap(inputPic.Width / 8, inputPic.Height / 8);
                using (Graphics gr = Graphics.FromImage(rotatedPic))
                {
                    gr.RotateTransform(angle);
                    gr.Clear(Color.White);
                    gr.DrawImage(shrinkPic, (int)(shrinkPic.Height * Math.Sin(angle * 3.1415 / 180) * 0.5), -(int)(shrinkPic.Width * Math.Sin(angle * 3.1415 / 180) * 0.5));
                    //croppedPic = rotatedPic.Clone(new Rectangle(croppedPic.Width / 2, croppedPic.Height / 2, croppedPic.Width, croppedPic.Height), croppedPic.PixelFormat);
                    //rotatedPic.Save(@"F:\VisualStudioProjects\repos\BitmapTextTilt\Cropped" + angle.ToString() + ".bmp");
                }
                using (Graphics graphics = Graphics.FromImage(columnPic))
                {
                    graphics.DrawImage(rotatedPic, 0, 0, 1, colImageHeight);
                }
                log += angle.ToString() + "    ";
                for (int i = 0; i < colImageHeight; i++)
                {
                    brightnessGrad[i] = Math.Abs(brightness - (columnPic.GetPixel(0, i).R + columnPic.GetPixel(0, i).G + columnPic.GetPixel(0, i).B));
                    brightness = columnPic.GetPixel(0, i).R + columnPic.GetPixel(0, i).G + columnPic.GetPixel(0, i).B;
                    //Console.WriteLine(brightnessGrad[i]);
                }
                //var sortedBrightness = brightnessGrad.OrderBy(i => i).ToArray();
                var sortedBrightness = brightnessGrad.OrderByDescending(i => i).ToArray();
                angles.Add(angle, sortedBrightness.Take(20).Sum() / 20);
                log += string.Join(", ", sortedBrightness.Take(20));
                log += "\n" + sortedBrightness.Take(20).Sum() / 20 + "\n\n";
                //Console.WriteLine(angle.ToString() + "    " + sortedBrightness.Reverse().Take(30).Sum() / 30);
            }
            float optimumAngle = angles.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            //File.WriteAllText(@"F:\VisualStudioProjects\repos\BitmapTextTilt\log.txt", log + "\n" + optimumAngle.ToString());
            using (Graphics gr = Graphics.FromImage(horisontalPic))
            {
                gr.RotateTransform(optimumAngle);
                gr.Clear(Color.White);
                gr.DrawImage(inputPic, (int)(inputPic.Height * Math.Sin(optimumAngle * 3.1415 / 180) * 0.5), -(int)(inputPic.Width * Math.Sin(optimumAngle * 3.1415 / 180) * 0.5));
            }
            //horisontalPic.Save(@"F:\VisualStudioProjects\repos\BitmapTextTilt\Output_full" + optimumAngle.ToString() + ".bmp");
            return (Bitmap)horisontalPic;
        }
    }
}
