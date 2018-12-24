namespace NetCoreGenericHost.Settings
{
    public class SiloConfigSettings
    {
        public string ClusterId { get; set; }
        public string ServiceId { get; set; }
        public int SiloPort { get; set; }
        public int GatewayPort { get; set; }
    }
}