
using System.Reflection;
using System.Text;

namespace WorkCsv;

internal class Program
{
	static void Main( string[] args )
	{
		(var command, var isColumn) = ValidateParams( args );
		// コマンド設定がない場合で返ってきてれば、使い方を表示しているかエラーを出している
		if( command == Command.None )
		{
			return;
		}
		IWork work = command switch
		{
			Command.Sort => new SortWork(),
			Command.Swap => new SwapWork(),
			Command.Sum => new SumWork(),
			_ => throw new ArgumentException( "実行条件が不正です。" ),
		};
		CsvParser csv = new();
		csv.Parse( args[0], isColumn, Encoding.UTF8 );
		work.Execute( csv, args[3..] );	// 0, 1, 2 は全部チェックしている
	}

	private static (Command, bool) ValidateParams( string[] args )
	{
		if( args.Length < 3 )
		{
			Console.Error.WriteLine( "使い方" );
			Console.Error.WriteLine( $"{Assembly.GetEntryAssembly()!.GetName().Name} <CSVファイルパス> <列名有無> <実行条件>" );
			Console.Error.WriteLine( $"列名有無：先頭行が列名の場合は 1または {bool.TrueString}、そうではない場合は 0 または {bool.FalseString}" );
			Console.Error.WriteLine( "実行条件 以下のいずれか(同時指定は負荷)" );
			Console.Error.WriteLine( "ある列でソートをする：sort 列番号" );
			Console.Error.WriteLine( "列の順番を変更する：swap 列番号 列番号...省略したものは、元の順番で後ろに付加する" );
			Console.Error.WriteLine( "集計行を追加する：sum 列番号。列番号省略時はすべての列を対象にする" );
			Console.Error.WriteLine( "列番号は1から、または列名での指定も可能" );
			Console.Error.WriteLine( "結果のCSVは stdout に出力する" );
			return (Command.None, false);
		}
		// ファイルチェック
		if( !File.Exists( args[0] ) )
		{
			Console.Error.WriteLine( "ファイルが存在しません" );
			Console.Error.WriteLine( args[0] );
			return (Command.None, false);
		}
		// 列名チェック
		if( !bool.TryParse( args[1], out var isColumn ) )
		{
			int value = int.Parse( args[1] );
			isColumn = value switch
			{
				0 => false,
				1 => true,
				_ => throw new ArgumentException( "列名有無が不正です。" ),
			};
		}
		// コマンドチェック
		var command = args[2].ToLower() switch
		{
			"sort" => Command.Sort,
			"swap" => Command.Swap,
			"sum" => Command.Sum,
			_ => throw new ArgumentException( "実行条件が不正です。" ),
		};
		return (command, isColumn);
	}
}
