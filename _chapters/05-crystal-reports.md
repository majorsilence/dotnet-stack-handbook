---
layout: chapter
title: "Crystal Reports"
number: 5
part: 2
---

Examples to use crystal reports from c#. Crystal reports for .net currently only supports running on .net framework 4.8 and older. If reports need to be generated on modern .net see [CrystalCmd Server and Client](#crystalcmd-server-and-client) below.

Make sure you have the crystal reports runtime installed. It
can be downloaded from [https://wiki.scn.sap.com/wiki/display/BOBJ/Crystal+Reports%2C+Developer+for+Visual+Studio+Downloads](https://wiki.scn.sap.com/wiki/display/BOBJ/Crystal+Reports%2C+Developer+for+Visual+Studio+Downloads).

All examples below require references for **CrystalDecisions.CrystalReports.Engine** and **CrystalDecisions.Shared** to be added to your project.

Ensure the CrystalReports Version and PublicKey token match the installed version of Crystal Reports.

```xml
<ItemGroup>
    <Reference Include="CrystalDecisions.Windows.Forms, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304, processorArchitecture=MSIL">
        <HintPath>C:\Windows\Microsoft.NET\assembly\GAC_MSIL\CrystalDecisions.Windows.Forms\v4.0_13.0.4000.0__692fbea5521e1304\CrystalDecisions.Windows.Forms.dll</HintPath>
    </Reference>
    <Reference Include="CrystalDecisions.CrystalReports.Engine, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304, processorArchitecture=MSIL">
        <HintPath>C:\Windows\Microsoft.NET\assembly\GAC_MSIL\CrystalDecisions.CrystalReports.Engine\v4.0_13.0.4000.0__692fbea5521e1304\CrystalDecisions.CrystalReports.Engine.dll</HintPath>
    </Reference>
    <Reference Include="CrystalDecisions.Shared, Version=13.0.4000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304, processorArchitecture=MSIL">
        <HintPath>C:\Windows\Microsoft.NET\assembly\GAC_MSIL\CrystalDecisions.Shared\v4.0_13.0.4000.0__692fbea5521e1304\CrystalDecisions.Shared.dll</HintPath>
    </Reference>
</ItemGroup>
```

## Set data using DataTables

Example initializing a report and passing in a DataTable.

```cs
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;

// Pass a DataTable to a crystal report table
public static void SetData(string crystalTemplateFilePath,
    string tableName, DataSet val)
{
    using (var rpt = new ReportDocument())
    {
        rpt.Load(crystalTemplateFilePath);

        rpt.Database.Tables[tableName].SetDataSource(val);
    }
}

// Pass any generic IEnumerable data to a crystal report DataTable
public static void SetData<T>(string crystalTemplateFilePath,
    string tableName, IEnumerable<T> val)
{
    using (var rpt = new ReportDocument())
    {
        rpt.Load(crystalTemplateFilePath);

        var dt = ConvertGenericListToDatatable(val);
        rpt.Database.Tables[tableName].SetDataSource(dt);
    }
}

// Found somewhere on the internet.  I know longer remember where.
public static DataTable ConvertGenericListToDatatable<T>(IEnumerable<T> dataLst)
{
    DataTable dt = new DataTable();

    foreach (var info in dataLst.FirstOrDefault().GetType().GetProperties())
    {
        dt.Columns.Add(info.Name, info.PropertyType);
    }

    foreach (var tp in dataLst)
    {
        DataRow row = dt.NewRow();
        foreach (var info in typeof(T).GetProperties())
        {
            if (info.Name == "Item") continue;
            row[info.Name] = info.GetValue(tp, null) == null ? DBNull.Value : info.GetValue(tp, null);
        }
        dt.Rows.Add(row);
    }
    dt.AcceptChanges();
    return dt;
}
```

## Set report parameters

Set report parameters from code.

```cs
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;

public static void SetParameterValueName(string crystalTemplateFilePath, object val)
{
    using (var rpt = new ReportDocument())
    {
        rpt.Load(crystalTemplateFilePath);

        string name = "ParameterName";
        if (rpt.ParameterFields[name] != null)
        {
            rpt.SetParameterValue(name, val);
        }
    }
}
```

## Move report objects

Example moving an object.

```cs
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;

public static void MoveObject(string crystalTemplateFilePath)
{
    using (var rpt = new ReportDocument())
    {
        rpt.Load(crystalTemplateFilePath);

        rpt.ReportDefinition.ReportObjects["objectName"].Left = 15;
        rpt.ReportDefinition.ReportObjects["objectName"].Top = 15;
    }
}
```

## Export to pdf or other file type

Load a report and export it to pdf. You can pass in data or set other properties before the export.

```cs
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;

public static void ExportPdf(string crystalTemplateFilePath,
    string pdfFilename)
{
    using (var rpt = new ReportDocument())
    {
        rpt.Load(crystalTemplateFilePath);

        var exp = ExportFormatType.PortableDocFormat;
        rpt.ExportToDisk(exp, pdfFilename);
    }
}
```

## CrystalCmd Server and Client {#crystalcmd-server-and-client}

crystalcmd is a:

> Java and c# program to load json files into crystal reports and produce PDFs.

- [https://github.com/majorsilence/CrystalCmd](https://github.com/majorsilence/CrystalCmd)

tldr: use crystal reports with dotnet netstandard2.0, net48, net8.0, net9.0, net10.0 on linux, windows, mac, android, and iOS.

To host the cyrstalcmd .net server browse to [https://github.com/majorsilence/CrystalCmd/tree/main/dotnet](https://github.com/majorsilence/CrystalCmd/tree/main/dotnet) and build the **Dockerfile.wine** and **Dockerfile.crystalcmd**. If a java server is required use the prebuilt image at [https://hub.docker.com/r/majorsilence/crystalcmd](https://hub.docker.com/r/majorsilence/crystalcmd). The c# server is recommended.

With a crystalcmd server running crystal report templates and data can be sent to it to produce pdf files. The docker images can run on any system that supports docker such as mac, windows, and linux.

Add the [package Majorsilence.CrystalCmd.Client](https://www.nuget.org/packages/Majorsilence.CrystalCmd.Client) to your project.

To call a crystalcmd server use the nuget package **Majorsilence.CrystalCmd.Client**.

```bash
dotnet add package Majorsilence.CrystalCmd.Client
```

This example will call the server and return the pdf report as a stream.

```cs
DataTable dt = new DataTable();

// init report data
var reportData = new Majorsilence.CrystalCmd.Common.Data()
{
    DataTables = new Dictionary<string, string>(),
    MoveObjectPosition = new List<Majorsilence.CrystalCmd.Common.MoveObjects>(),
    Parameters = new Dictionary<string, object>(),
    SubReportDataTables = new List<Majorsilence.CrystalCmd.Common.SubReports>()
};

// add as many data tables as needed.  The client library will do the necessary conversions to json/csv.
reportData.AddData("report name goes here", "table name goes here", dt);

// export to pdf
var crystalReport = System.IO.File.ReadAllBytes("The rpt template file path goes here");
using (var instream = new MemoryStream(crystalReport))
using (var outstream = new MemoryStream())
{
    
    //var rpt = new Majorsilence.CrystalCmd.Client.Report(serverUrl, username: "The server username goes here", password: "The server password goes here");
    var rpt = new Majorsilence.CrystalCmd.Client.ReportWithPolling(serverUrl, username: "The server username goes here", password: "The server password goes here");
    using (var stream = await rpt.GenerateAsync(reportData, instream, _httpClient))
    {
        stream.CopyTo(outstream);
        return outstream.ToArray();
    }
}
```
