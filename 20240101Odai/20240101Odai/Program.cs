﻿// See https://aka.ms/new-console-template for more information
using CSharpOdai;

Measure.Run();

Console.WriteLine( DateTime.ParseExact( args[0], "MM-dd-yyyy", System.Globalization.CultureInfo.InvariantCulture ).ToString( "yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture ) );
