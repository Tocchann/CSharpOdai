using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOdai;

internal class Measure
{
	public static void Run()
	{
		// メソッドの初期コール時間を除外する(純粋に処理時間を計測する)
		Odai_DateOnly( "01-01-2024" );
		Odai_DateTime( "01-01-2024" );
		// 実際の処理時間の計測
		Profile( "01-01-2024" );
	}
	static string Odai_DateTime( string input )
	{
		return DateTime.ParseExact( input, "MM-dd-yyyy", System.Globalization.CultureInfo.InvariantCulture ).ToString( "yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture );
	}
	static string Odai_DateOnly( string input )
	{
		return DateOnly.ParseExact( input, "MM-dd-yyyy", System.Globalization.CultureInfo.InvariantCulture ).ToString( "yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture );
	}

	static void Profile( string input )
	{
		Console.WriteLine( "計測中...");
		var sw = new System.Diagnostics.Stopwatch();
		sw.Start();
		for( int i = 0 ; i < 100000000 ; i++ )
		{
			Odai_DateTime( "01-01-2024" );
		}
		sw.Stop();
		Console.WriteLine( $"DateTime: {sw.ElapsedMilliseconds}ms" );

		Console.WriteLine( "計測中..." );
		sw.Restart();
		for( int i = 0 ; i < 100000000 ; i++ )
		{
			Odai_DateOnly( "01-01-2024" );
		}
		sw.Stop();
		Console.WriteLine( $"DateOnly: {sw.ElapsedMilliseconds}ms" );
	}

}
