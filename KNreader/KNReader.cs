using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KNreader
{
    class KNReader
    {
        public static async Task<Pozemek> GetPoints(HttpClient httpClient, Pozemek pozemek)
        {
            var encodedQuery = WebUtility.UrlEncode($"{pozemek.Ku} {pozemek.KmenoveCislo}");
            var responseString = await httpClient.GetStringAsync($"https://regiony.kurzy.cz/katastr/?q={encodedQuery}");

            // sometimes the property is not found in single query but instead list of possible matches is returned
            // this will try to find the correct property from such list if that occures
            Match match = null;
            string pattern = $"<td><a href=\"(.*)\" title=\"\">Parcela.*{pozemek.KmenoveCislo}</a></td>";

            var page = 0;
            while (true)
            {
                match = Regex.Match(responseString, pattern);

                if (match.Success)
                {
                    responseString = await httpClient.GetStringAsync(match.Groups[1].Value);
                    break;
                }
                else
                {
                    // try to go to the next page
                    match = Regex.Match(responseString, $"<a href=\"(.*)\">Další strana.*</a>");

                    // max 5 pages
                    if (match.Success && page++ < 5)
                    {
                        responseString = await httpClient.GetStringAsync($@"https://regiony.kurzy.cz{match.Groups[1].Value}");
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // ust parse the GPS and find one GPS on the circle that will have the same area as the area of property
            pattern = @"GPS pozice<\/th><td>(.*),(.*)<\/td><\/tr>";
            match = Regex.Match(responseString, pattern);

            var latitude = Double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var longtitude = Double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

            var latitude2 = latitude;
            var longtitude2 = GetSecondCoordinate(latitude, longtitude, (long)pozemek.Vymera);

            pozemek.DefinicniBod = $@"{longtitude.ToString(CultureInfo.InvariantCulture)},{latitude.ToString(CultureInfo.InvariantCulture)}";
            pozemek.SouradniceKruznice = $@"{longtitude2.ToString(CultureInfo.InvariantCulture)},{latitude2.ToString(CultureInfo.InvariantCulture)}";

            return pozemek;
        }

        public static double GetLengthOfLongtitudeDegree(double latitude)
        {
            int LengthOfLongtituedDegreeAtEquator = 111320;
            return Math.Cos(latitude) * LengthOfLongtituedDegreeAtEquator;
        }

        public static double GetSecondCoordinate(double latitude, double longtitude, long area)
        {
            var r = Math.Sqrt(area / Math.PI);
            var longDegreeLength = GetLengthOfLongtitudeDegree(latitude);
            var offset = r / longDegreeLength;


            return longtitude + offset;
        }
    }
}
