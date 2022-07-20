namespace Main.Databases
{
    public partial class Database
    {
        public bool IsSynchronised { get; set; }
        private bool _isSynchronised;
        
        private void OnSetSynchronised(string id, byte[] value)
        {
            
        }

        private void OnSetSynchronised(string id, byte[] value, ulong modCount)
        {
            
        }
        
        
    }
}