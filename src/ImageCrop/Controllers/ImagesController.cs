using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNet.Mvc;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace ImageCrop.Controllers
{
    [Route("api/[controller]")]
    public class ImagesController : Controller
    {
        internal string GetUriFromParameters()
        {
            return this.Request.Query.First(kv => kv.Key.Equals("img", StringComparison.OrdinalIgnoreCase)).Value.First();
        }

        // GET: api/values
        [HttpGet]
        public HttpResponseMessage Get()
        {
            return new HttpResponseMessage()
            {
                Content = new StringContent("call with < left x fraction > / < top y fraction > / < right x fraction > / < bottom y fraction > / ? img =< URL > ")
            };            
        }

        // GET api/values/5
        [HttpGet("{left_x}/{top_y}/{right_x}/{bottom_y}")]
        public HttpResponseMessage Get(float left_x, float top_y, float right_x, float bottom_y)
        {
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
                    using (Bitmap loadedImage = (Bitmap)Image.FromStream(inputStream))
                    {
                        // calculating x, y offsets and width and height
                        int width = (int)Math.Ceiling(loadedImage.Width * (right_x - left_x));
                        int x = (int)Math.Floor(loadedImage.Width * left_x);
                        int height = (int)Math.Ceiling(loadedImage.Height * (bottom_y - top_y));
                        int y = (int)Math.Floor(loadedImage.Height * top_y);

                        using (Bitmap workingImage = new Bitmap(width, height))
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
                            // returning as a JPG stream
                            content.StatusCode = HttpStatusCode.OK;
                            MemoryStream memoryStream = new MemoryStream();
                            workingImage.Save(memoryStream, ImageFormat.Jpeg);
                            content.Content = new ByteArrayContent(memoryStream.ToArray());
                            content.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                        }
                    }
                }
            }            
            return content;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {

        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
