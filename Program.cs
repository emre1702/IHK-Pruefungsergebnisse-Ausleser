using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Serialization;

namespace PruefungsErgebnisse
{
    internal class Program
    {
        #region Private Methods

        private static void Main(string[] args)
        {
            try
            {
                var logic = new Logic();
                if (!logic.LoadConfig())
                {
                    Console.WriteLine("Die config.xml Datei fehlt oder konnte nicht geladen werden!");
                    return;
                }

                if (!logic.AddSessionId())
                {
                    Console.WriteLine("Konnte SessionId nicht laden!");
                    return;
                }
                logic.TryLogin();

                logic.OutputMarks();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        #endregion Private Methods
    }
}