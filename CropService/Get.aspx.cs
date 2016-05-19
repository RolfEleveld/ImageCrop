using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

/// <summary>
/// Deply this with certificate
/// Use Password 4 Cropservice using basic web together.
/// </summary>
/// <see cref="https://azure.microsoft.com/en-us/documentation/articles/app-service-web-arm-with-msdeploy-provision/"/>
namespace CropService
{
    public partial class Get : System.Web.UI.Page
    {
        internal string GetFromParameters(string parameter)
        {
            var value = Request.QueryString.GetValues(parameter);
            if (value != null)
            {
                return value.First();
            }
            return null;
        }
        internal Double GetFromParameters(string parameter, double defaultvalue)
        {
            var value = Request.QueryString.GetValues(parameter);
            Double valueDouble;
            if (value != null && Double.TryParse(value.First(), out valueDouble))
            {
                return valueDouble;
            }
            return defaultvalue;
        }
        protected async void Page_Load(object sender, EventArgs e)
        {
            double left_x, top_y, right_x, bottom_y;
            left_x = GetFromParameters("left_x", 0);
            top_y = GetFromParameters("top_y", 0);
            right_x = GetFromParameters("right_x", 1);
            bottom_y = GetFromParameters("bottom_y", 1);

            String imageUri = GetFromParameters("img");
            HttpResponseMessage content = new HttpResponseMessage();
            if (string.IsNullOrWhiteSpace(imageUri))
            {
                throw new MissingFieldException("Missing reference to an image in the query with tag img");
            }
            if (left_x < 0.0 || left_x > 1.0 || right_x < 0.0 || right_x > 1.0 || top_y < 0.0 || top_y > 1.0 || bottom_y < 0.0 || bottom_y > 1.0 || left_x >= right_x || top_y >= bottom_y)
            {
                throw new FormatException("The scale needs to be between 0.0 and 1.0 for each of the crop factors, and left is less than right and top needs to be less than bottom");
            }

            WebRequest request = WebRequest.Create(imageUri);
            try
            {
                WebResponse response = await request.GetResponseAsync();
                if (response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                    // if the remote file was found, download it
                    using (Stream inputStream = response.GetResponseStream())
                    {
                        // assume the loaded stream is an image pull the stream into the image.
                        using (Bitmap loadedImage = (Bitmap)System.Drawing.Image.FromStream(inputStream))
                        {
                            // calculating x, y offsets and width and height in pixels
                            int width = (int)Math.Ceiling(loadedImage.Width * (right_x - left_x));
                            int x = (int)Math.Floor(loadedImage.Width * left_x);
                            int height = (int)Math.Ceiling(loadedImage.Height * (bottom_y - top_y));
                            int y = (int)Math.Floor(loadedImage.Height * top_y);
                            // use existing image to maintain the graphics structure.
                            using (Bitmap workingImage = loadedImage.Clone(new Rectangle(x, y, width, height), loadedImage.PixelFormat))
                            {
                                // Manipulating image here.
                                using (Graphics graphic = Graphics.FromImage(workingImage))
                                {
                                    graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    graphic.SmoothingMode = SmoothingMode.HighQuality;
                                    graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                    graphic.CompositingQuality = CompositingQuality.HighQuality;
                                    graphic.CompositingMode = CompositingMode.SourceOver;
                                    graphic.Clear(Color.White);
                                    //Code used to crop
                                    graphic.DrawImage(loadedImage, 0, 0, new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
                                }
                                // returning as a JPG stream and end!
                                Response.Clear();
                                Response.StatusCode = 200;
                                workingImage.Save(Response.OutputStream, ImageFormat.Jpeg);
                                Response.ContentType = "image/jpeg";
                                Response.Flush();
                                Response.End();
                            }
                        }
                    }
                }
            }
            catch
            {
                throw new FieldAccessException("Image could not be processed");
            }

        }
    }
}