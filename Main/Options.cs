namespace Main
{
    public static class Options
    {
        public const int MaxMessageSize = int.MaxValue;
        public const int DefaultPort = 11000;
        
        /// <summary>
        /// True if clients shall save data persistently if host of data is persistent
        /// </summary>
        public const bool DefaultClientPersistence = true;
        
        //default timeout in ms
        public const int DefaultTimeout = 3000;
    }
}