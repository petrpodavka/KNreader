using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using KNreader;

namespace KNmapper
{
    class Program
    {
        public static string suffix = ".csv";
        public static string htmlSuffix = ".html";

        static void Main(string[] args)
        {
            string path = "";
            var files = new List<string>();

            if (args.Length != 1)
            {
                Console.WriteLine("Wrong number of arguments. Supply path to direcotry with CSV files or path to one specific CSV file.");
                Console.Write("Press any key to exit");
                Console.ReadKey();
                return;
            }

            path = args[0];
            var filename = Path.GetFileName(path);
            if (filename?.Length > 0)
            {
                path = path.TrimEnd(filename);
                files.Add(filename.TrimEnd(suffix));
            }
            else
            {
                files = Directory.GetFiles(path, $"*{suffix}", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f).TrimEnd(suffix)).ToList();
            }

            foreach (var file in files)
            {
                // process each file in series (every entry in each file will run in parallel)
                Console.WriteLine($"{files.IndexOf(file) + 1}/{files.Count()}, {file}{suffix}");
                GenerateMap(path, file);
            }

            Console.Write("\nPress any key to exit...");
            Console.ReadKey();
        }

        public static void GenerateMap(string path, string file)
        {
            using (var htmlWriter = new StreamWriter(Path.Combine(path, $"{file}{htmlSuffix}")))
            using (var reader = new StreamReader(Path.Combine(path, $"{file}{suffix}")))
            using (var csvReader = new CsvReader(reader))
            {
                htmlWriter.AutoFlush = true;
                csvReader.Configuration.Delimiter = ",";
                var records = csvReader.GetRecords<Pozemek>().ToList();

                Console.Write($" # Generating map for {records.Count()} items\t");

                htmlWriter.Write(htmlStart);

                foreach (var r in records)
                {
                    htmlWriter.Write(GenerateEntry(r));
                }

                htmlWriter.Write(htmlEnd);

                Console.WriteLine($"\n # Map generated");
            }
        }

        public static string GenerateEntry(Pozemek pozemek)
        {
            return String.Format(entryTemplate, pozemek.OpsubNazev, pozemek.Parcela, pozemek.DefinicniBod, pozemek.SouradniceKruznice, Math.Max(Math.Sqrt(pozemek.Vymera/Math.PI)/2,3).ToString(CultureInfo.InvariantCulture));
        }

        private static string htmlStart = @"<!doctype html>
<html>
<head>
    <script src=""https://api.mapy.cz/loader.js""></script>
    <script>Loader.load()</script>
</head>

<body>
    <div id=""m"" style=""position: fixed; width:100%; height:100%;""></div>
    <script type=""text/javascript"">
        var zoom = 8;
        var center = SMap.Coords.fromWGS84(15.3989192, 49.9133978);
        var m = new SMap(JAK.gel(""m""), center, zoom);
        m.addDefaultLayer(SMap.DEF_BASE).enable();
        m.addDefaultControls();

        var options = {
            color: ""#f00""
        };

        var dynamic = new SMap.Layer.Geometry();
        m.addLayer(dynamic).enable();

        //var static = new SMap.Layer.Geometry();
        //m.addLayer(static).disable();

        var markers = new SMap.Layer.Marker();
        m.addLayer(markers).disable();
";

        private static string entryTemplate = @"
        var c = new SMap.Card(500);
        c.getHeader().innerHTML = ""<b>OPSUB:</b> {0}<br><b>Parcela:</b> {1}"";
        
        dynamic.addGeometry(new SMap.Geometry(SMap.GEOMETRY_CIRCLE, null, [SMap.Coords.fromWGS84({2}),{4}], options));
        //static.addGeometry(new SMap.Geometry(SMap.GEOMETRY_CIRCLE, null, [SMap.Coords.fromWGS84({2}),SMap.Coords.fromWGS84({3})], options));
        markers.addMarker(new SMap.Marker(SMap.Coords.fromWGS84({2})).decorate(SMap.Marker.Feature.Card, c));
";

        private static string htmlEnd = @"
        var listener = function(e) {
            var newZoom = m.getZoom();

            if (newZoom > zoom && newZoom == 16) {
                //dynamic.disable();
                //static.enable();
                markers.enable();
            }
            
            if (newZoom < zoom && newZoom == 15) {
                //dynamic.enable();
                //static.disable();
                markers.disable();
            }

            zoom = newZoom;
        }

        var signals = m.getSignals();
        signals.addListener(window, ""map-redraw"", listener);
    </script>
</body>
</html>";
    }
}
