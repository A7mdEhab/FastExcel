﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FastExcel
{
    /// <summary>
    /// Read and update xl/sharedStrings.xml file
    /// </summary>
    public class SharedStrings
    {
        //A dictionary is a lot faster than a list
        private Dictionary<string, int> StringDictionary { get; set; }

        private bool SharedStringsExists { get; set; }
        private ZipArchive ZipArchive { get; set; }

        public SharedStrings(ZipArchive archive)
        {
            this.ZipArchive = archive;
            
            this.SharedStringsExists = true;

            if (!this.ZipArchive.Entries.Where(entry => entry.FullName == "xl/sharedStrings.xml").Any())
            {
                this.StringDictionary = new Dictionary<string, int>();
                this.SharedStringsExists = false;
                return;
            }
            
            using (Stream stream = this.ZipArchive.GetEntry("xl/sharedStrings.xml").Open())
            {
                if (stream == null)
                {
                    this.StringDictionary = new Dictionary<string, int>();
                    this.SharedStringsExists = false;
                    return;
                }

                XDocument document = XDocument.Load(stream);

                if (document == null)
                {
                    this.StringDictionary = new Dictionary<string, int>();
                    this.SharedStringsExists = false;
                    return;
                }

                int i = 0;
                this.StringDictionary = document.Descendants().Where(d => d.Name.LocalName == "t").Select(e => e.Value).ToDictionary(k=> k,v => i++);
            }
        }

        internal int AddString(string stringValue)
        {
            if (StringDictionary.ContainsKey(stringValue))
            {
                return StringDictionary[stringValue];
            }
            else
            {
                StringDictionary.Add(stringValue,StringDictionary.Count);
                return StringDictionary.Count - 1;
            }
        }

        internal void Write()
        {
            StreamWriter streamWriter = null;
            try
            {
                if (this.SharedStringsExists)
                {
                    streamWriter = new StreamWriter(this.ZipArchive.GetEntry("xl/sharedStrings.xml").Open());
                }
                else
                {
                    streamWriter = new StreamWriter(this.ZipArchive.CreateEntry("xl/sharedStrings.xml").Open());
                }

                // TODO instead of saving the headers then writing them back get position where the headers finish then write from there

                /* Note: the count attribute value is wrong, it is the number of times strings are used thoughout the workbook it is different to the unique count 
                 *       but because this library is about speed and Excel does not seem to care I am not going to fix it because I would need to read the whole workbook
                 */

                streamWriter.Write(string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                            "<sst uniqueCount=\"{0}\" count=\"{0}\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">", this.StringDictionary.Count));

                // Add Rows
                foreach (var stringValue in this.StringDictionary)
                {
                    streamWriter.Write(string.Format("<si><t>{0}</t></si>", stringValue.Key));
                }

                //Add Footers
                streamWriter.Write("</sst>");
                streamWriter.Flush();
            }
            finally
            {
                streamWriter.Dispose();
            }
        }
    }
}