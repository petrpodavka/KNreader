using CsvHelper.Configuration.Attributes;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KNreader
{
    public class Pozemek
    {
        [Index(0)]
        public string Okres { get; set; }
        [Index(1)]
        public string Obec { get; set; }
        [Index(2)]
        public string Ku { get; set; }
        [Index(3)]
        public string OpsubTyp { get; set; }
        [Index(4)]
        public string OpsubRc { get; set; }
        [Index(5)]
        public string OpsubNazev { get; set; }
        [Index(6)]
        public string OpsubAdresa { get; set; }
        [Index(7)]
        public string IdVlastnictvi { get; set; }
        [Index(8)]
        public string PodilCitatel { get; set; }
        [Index(9)]
        public string PodilJmenovatel { get; set; }
        [Index(10)]
        public string ParcelaVymera { get; set; }
        [Index(11)]
        public string ParcelaVymera2 { get; set; }
        [Index(12)]
        public string DruhPozemku { get; set; }
        [Index(13)]
        public string ZpusobVyuziti { get; set; }
        [Index(14)]
        public string Parcela { get; set; }
        [Index(15)]
        public string CisloLvParcela { get; set; }
        [Index(16)]
        public string StavbaCastObce { get; set; }
        [Index(17)]
        public string StavbaZpusobVyuziti { get; set; }
        [Index(18)]
        public string Stavba { get; set; }
        [Index(19)]
        public string CisloLvBudova { get; set; }
        [Optional]
        public string DefinicniBod { get; set; }
        [Optional]
        public string SouradniceKruznice { get; set; }
        [Ignore]
        public string KmenoveCislo => Regex.Match(Parcela, @"^.*č. (.*)$").Groups[1].Value;
        [Ignore]
        public double Vymera => Double.Parse(ParcelaVymera, CultureInfo.InvariantCulture);
    }
}
