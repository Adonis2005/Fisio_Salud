using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
namespace FisioSalud_Proyecto.Helpers
{
    public static class ReporteExportador
    {
        public static List<List<string>> LeerCsv(string csv)
        {
            var rows=new List<List<string>>();var row=new List<string>();var field=new StringBuilder();bool quoted=false;
            for(int i=0;i<csv.Length;i++) { char c=csv[i]; if(c=='"') {if(quoted && i+1<csv.Length && csv[i+1]=='"'){field.Append('"');i++;}else quoted=!quoted;}
                else if(!quoted && (c==',' || c=='\n')){row.Add(field.ToString().TrimEnd('\r'));field.Clear();if(c=='\n'){rows.Add(row);row=new List<string>();}}
                else field.Append(c); }
            if(field.Length>0 || row.Count>0){row.Add(field.ToString());rows.Add(row);}return rows;
        }
        public static byte[] Excel(string csv)
        {
            using var stream=new MemoryStream();
            using(var zip=new ZipArchive(stream,ZipArchiveMode.Create,true)){
                void Write(string path,string content){using var writer=new StreamWriter(zip.CreateEntry(path).Open(),new UTF8Encoding(false));writer.Write(content);}
                Write("[Content_Types].xml","<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
                Write("_rels/.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                Write("xl/workbook.xml","<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Reporte\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                Write("xl/_rels/workbook.xml.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";var data=new XElement(ns+"sheetData");
                foreach(var row in LeerCsv(csv)){var element=new XElement(ns+"row");foreach(var cell in row) element.Add(new XElement(ns+"c",new XAttribute("t","inlineStr"),new XElement(ns+"is",new XElement(ns+"t",cell))));data.Add(element);}
                Write("xl/worksheets/sheet1.xml",new XElement(ns+"worksheet",data).ToString());
            }return stream.ToArray();
        }
    }
}
