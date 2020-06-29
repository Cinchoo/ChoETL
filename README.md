# Cinchoo ETL

<!--
  Title: Cinchoo ETL
  Description: ETL Framework for .NET (Reader / Writer for CSV, Fixed/Flat, Xml, JSON, Key-Value formatted files)
  Author: Cinchoo
  -->
 <meta name='keywords' content='CSV, Fixed, Flat, Xml, JSON, Key-Value, KVP, Reader, Writer, Parser'>
 
[![Join the chat at https://gitter.im/ChoETL/Lobby](https://badges.gitter.im/ChoETL/Lobby.svg)](https://gitter.im/ChoETL/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Build status](https://ci.appveyor.com/api/projects/status/6ktkagfa67vbn9ys?svg=true)](https://ci.appveyor.com/project/Cinchoo/choetl)


An ETL framework for .NET 

Simple, intutive  Extract, transform and load (ETL) library for .NET. Extremely fast, flexible, and easy to use. 

Cinchoo ETL is a code-based ETL framework for extracting data from multiple sources, transforming, and loading into your very own data warehouse in .NET environment. You can have data in your data warehouse in no time.

## Install

To install Cinchoo ETL (.NET Framework), run the following command in the Package Manager Console

    PM> Install-Package ChoETL

To install Cinchoo ETL (.NET Standard), run the following command in the Package Manager Console

    PM> Install-Package ChoETL.NETStandard
    
Add namespace to the program

``` csharp
using ChoETL;
```

# Phase 1:
Here are the items will be targetted on phase 1. 

  1. CSV Reader / Writer
      + [CSV Reader](https://github.com/Cinchoo/ChoETL/wiki/QuickCSVLoad)
      + [CSV Writer](https://github.com/Cinchoo/ChoETL/wiki/QuickCSVWrite)
  2. Fixed Length Reader / Writer
      + [Fixed Length Reader](https://github.com/Cinchoo/ChoETL/wiki/QuickFixedLengthLoad)
      + [Fixed Length Writer](https://github.com/Cinchoo/ChoETL/wiki/QuickFixedLengthWrite)
  3. Xml Reader / Writer
      + [Xml Reader](https://github.com/Cinchoo/ChoETL/wiki/QuickXmlLoad)
      + [Xml Writer](https://github.com/Cinchoo/ChoETL/wiki/QuickXmlWrite)
  4. JSON Reader / Writer
      + [JSON Reader](https://github.com/Cinchoo/ChoETL/wiki/QuickJSONLoad)
      + [JSON Writer](https://github.com/Cinchoo/ChoETL/wiki/QuickJSONWrite)    
  5. Key-Value Reader / Writer
      + [Key-Value Pair (KVP) Reader](https://github.com/Cinchoo/ChoETL/wiki/QuickKVPLoad)
      + [Key-Value Pair (KVP) Writer](https://github.com/Cinchoo/ChoETL/wiki/QuickKVPWrite)
  6. Parquet Reader / Writer
      + [Parquet Reader](https://github.com/Cinchoo/ChoETL/wiki/QuickParquetLoad)
      + [Parquet Writer](https://github.com/Cinchoo/ChoETL/wiki/QuickParquetWrite)
  7. Yaml Reader / Writer
      + [Yaml Reader](https://github.com/Cinchoo/ChoETL/wiki/QuickYamlLoad)
      + [Yaml Writer](https://github.com/Cinchoo/ChoETL/wiki/QuickYamlWrite)

# Phase 2:
ETL Pipelines and ETL processes are coming...


## Documentation

https://github.com/Cinchoo/ChoETL/wiki

## StackOverflow

[Cinchoo ETL questions in StackOverflow](https://stackoverflow.com/search?tab=newest&q="Cinchoo%20ETL")

[Cinchoo ETL questions in StackOverflow](https://stackoverflow.com/questions/tagged/choetl)

## Download Binary

#### Base Library

+ [Nuget (.NET Framework)](https://www.nuget.org/packages/ChoETL/)
+ [Nuget (.NET Core)](https://www.nuget.org/packages/ChoETL.NETStandard/)

#### JSON Plug-In

+ [Nuget (.NET Framework)](https://www.nuget.org/packages/ChoETL.JSON/)
+ [Nuget (.NET Core)](https://www.nuget.org/packages/ChoETL.JSON.NETStandard/)

#### Parquet Plug-In

+ [Nuget (.NET Framework / .NET Core)](https://www.nuget.org/packages/ChoETL.Parquet/)

If this project help you reduce time to develop, you can give me a cup of coffee :)

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=93KJF659K7NVA)

