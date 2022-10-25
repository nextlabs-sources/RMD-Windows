using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.chooseServerWindow.Model
{
    public class UrlDataModel
    {
        public UrlDataModel(int idPar, string urlPar)
        {
            this.ID = idPar;
            this.listUrl = urlPar;
        }
        private int id;
        private string url;

        public int ID
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }
        public string listUrl
        {
            get
            {
                return url;
            }
            set
            {
                url = value;
            }
        }

    }
}
