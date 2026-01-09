namespace P_Bit_Ruisseau
{
    internal static class Program
    {
        internal static Dictionary<string, List<Song>> Catalog = new Dictionary<string, List<Song>>();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}