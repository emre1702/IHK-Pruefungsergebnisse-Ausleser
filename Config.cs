using System.Collections.Generic;
using System.Xml.Serialization;

namespace PruefungsErgebnisse
{
    [XmlRoot("config")]
    public class Config
    {
        #region Public Properties

        [XmlElement("identnr")]
        public string IdentNr { get; set; }

        [XmlElement("prueflingsnummer")]
        public string PrueflingsNr { get; set; }

        #endregion Public Properties
    }
}