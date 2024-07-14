/*

Copyright (c) 2016-2020 Living Computers: Museum+Labs
Copyright (c) 2016-2024 Josh Dersch

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer 
       in the documentation and/or other materials provided with the distribution.
    3. Neither the name of the copyright holder nor the names of its contributors may be used to endorse or promote products derived from 
       this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED 
TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF 
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS 
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using iTextSharp.text;
using iTextSharp.text.pdf;
using Contralto.Logging;

namespace Contralto.IO.Printing
{
    /// <summary>
    /// PdfPageSink takes output from the ROS and turns it into
    /// PDF documents in the PrintOutputPath folder.
    /// 
    /// This uses the iTextSharp PDF creation libraries to do the hard work.
    /// </summary>
    public class PdfPageSink : IPageSink
    {
        public PdfPageSink(Configuration configuration)
        {
            _pageImages = new List<Image>();
            _configuration = configuration;
        }

        public void StartDoc()
        {
            _pageImages.Clear();

            try
            {
                // Start a new document.
                // All output to a Dover printer is letter-sized.
                _pdf = new Document(PageSize.LETTER);

                string path = Path.Combine(
                    _configuration.PrintOutputPath,
                    String.Format("AltoDocument-{0}.pdf", DateTime.Now.ToString("yyyyMMdd-hhmmss")));

                PdfWriter writer = PdfWriter.GetInstance(
                    _pdf,
                    new FileStream(path, FileMode.Create));

                _pdf.Open();

                // Let the Orbit deal with the margins.
                _pdf.SetMargins(0, 0, 0, 0);
            }
            catch(Exception e)
            {
                //
                // Most likely we couldn't create the output file; log the failure.
                // All output will be relegated to the bit bucket.
                //
                _pdf = null;

                Log.Write(LogType.Error, LogComponent.DoverROS, "Failed to create output PDF.  Error {0}", e.Message);
            }
        }

        public void AddPage(byte[] rasters, int width, int height)
        {
            if (_pdf != null)
            {
                Image pageImage = iTextSharp.text.Image.GetInstance(height, width, 1 /* greyscale */, 1 /* 1bpp */, rasters);
                pageImage.SetDpi(375, 375);
                pageImage.SetAbsolutePosition(_configuration.PageRasterOffsetX, _configuration.PageRasterOffsetY);
                pageImage.RotationDegrees = 90;
                pageImage.ScaleToFit(_pdf.PageSize);

                _pageImages.Add(pageImage);
            }
        }

        public void EndDoc()
        {
            if (_pdf != null)
            {
                try
                {
                    // Grab the configuration here so that if some joker changes the configuration
                    // while we're printing we don't do something weird.
                    bool reversePageOrder = _configuration.ReversePageOrder;

                    // Actually write out the pages now, in the proper order.
                    for (int i = 0; i < _pageImages.Count; i++)
                    {
                        _pdf.Add(_pageImages[reversePageOrder ? (_pageImages.Count - 1) - i : i]);
                        _pdf.NewPage();
                    }

                    _pdf.Close();
                }
                catch (Exception e)
                {
                    // Something bad happened during creation, log an error.
                    Log.Write(LogComponent.DoverROS, "Failed to create output PDF.  Error {0}", e.Message);
                }
            }
        }

        /// <summary>
        /// List of page images in this document.
        /// Since Alto software typically prints the document in reverse-page-order due to the way the 
        /// Dover produces output, we need be able to produce the PDF in reverse order.
        /// This uses extra memory, (about 1.7mb per page printed.)
        /// </summary>
        private List<Image> _pageImages;
        private Document _pdf;
        private Configuration _configuration;
    }
}
