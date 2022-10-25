using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.pages
{
   public class ClassificationItem 
    {
        private string name;
    
        public ClassificationItem(string name)
        {
            this.name = name;       
        }

        public string Name
        {
            get { return name; }
            set {
                name = value;            
            }
        }
    }
}
