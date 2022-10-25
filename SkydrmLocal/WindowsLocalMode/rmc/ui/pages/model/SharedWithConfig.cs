using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.pages.model
{
    public class SharedWithConfig
    {    
        private IList<string> shareEmail;
        public IList<string> SharedEmailLists
        {
            get { return shareEmail; }
            set { shareEmail = value; }
        }
        public string Comments { get; set; }
    }
}
