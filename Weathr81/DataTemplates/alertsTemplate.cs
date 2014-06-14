using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weathr81.DataTemplates
{
    public class AlertsTemplate
    {
        public AlertList alerts { get; set; }
    }
    public class AlertList
    {
        public ObservableCollection<AlertItem> alertList { get; set; }
    }
    public class AlertItem
    {
        public string Headline { get; set; }
        public string TextUrl { get; set; }
    }
}
