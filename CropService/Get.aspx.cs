using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CropService
{
    public partial class Get : System.Web.UI.Page
    {
        internal string GetUriFromParameters()
        {
            return this.Request.QueryString.GetValues("img").First();
        }
        internal Double GetLeftFromParameters()
        {
            return Double.Parse(this.Request.QueryString.GetValues("left_x").First());
        }
        internal Double GetTopFromParameters()
        {
            return Double.Parse(this.Request.QueryString.GetValues("top_y").First());
        }
        internal Double GetRightFromParameters()
        {
            return Double.Parse(this.Request.QueryString.GetValues("right_x").First());
        }
        internal Double GetBottomFromParameters()
        {
            return Double.Parse(this.Request.QueryString.GetValues("bottom_y").First());
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            double left_x, top_y, right_x, bottom_y;
            left_x = GetLeftFromParameters();
            top_y = GetTopFromParameters();
            right_x = GetRightFromParameters();
            bottom_y = GetBottomFromParameters();

            String imageUri = GetUriFromParameters();
            HttpResponseMessage content = new HttpResponseMessage();
            if (string.IsNullOrWhiteSpace(imageUri))
            {
                throw new MissingFieldException("Missing reference to an image in the query with tag img");
            }
            if (left_x < 0.0 || left_x > 1.0 || right_x < 0.0 || right_x > 1.0 || top_y < 0.0 || top_y > 1.0 || bottom_y < 0.0 || bottom_y > 1.0 || left_x >= right_x || top_y >= bottom_y)
            {
                throw new FormatException("The scale needs to be between 0.0 and 1.0 for each of the crop factors, and left is less than right and top needs to be less than bottom");
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(imageUri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if ((response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Moved ||
                response.StatusCode == HttpStatusCode.Redirect) &&
                response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
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
                        using (Bitmap workingImage = new Bitmap(loadedImage, width, height))
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
                            this.Response.Clear();
                            this.Response.StatusCode = 200;
                            workingImage.Save(this.Response.OutputStream, ImageFormat.Jpeg);
                            this.Response.ContentType = "image/jpeg";
                            this.Response.Flush();
                            this.Response.End();
                        }
                    }
                }
            }
        }
    }
}