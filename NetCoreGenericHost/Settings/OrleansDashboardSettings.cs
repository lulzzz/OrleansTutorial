using System;
using System.Collections.Generic;
using System.Text;

namespace NetCoreGenericHost.Settings
{
    public class OrleansDashboardSettings
    {
        public bool Enable { get; set; } = false;
        public int Port { get; set; } = 8080;
    }
}
