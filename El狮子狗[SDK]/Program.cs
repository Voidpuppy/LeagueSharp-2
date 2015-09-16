namespace ElRengar
{
    using System;

    using ElRengar.Config;

    using LeagueSharp.SDK.Core.Events;

    internal class Program : Standards
    {
        #region Methods

        private static void Main(string[] args)
        {
            try
            {
                Load.OnLoad += Rengar.OnLoad;
                Console.WriteLine("Rengar loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion
    }
}
