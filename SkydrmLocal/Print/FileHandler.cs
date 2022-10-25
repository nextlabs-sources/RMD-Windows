using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Print
{
    public interface FileHandler
    {
        System.Drawing.Image GetImage(int index);

        int GetPageCount();

        void Watermark(string watermark);

        void Release();

    }
}
