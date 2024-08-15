using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace WorkCsv;

// スピード重視型CSVパーサー
public class CsvParser
{
	// "" でくくられていない場合はトリムするかどうか(デフォルトはトリムする)
	public bool IsTrim { get; set; } = true;
	public bool Escape { get; set; } = false;

	static CsvParser()
	{
		//	Shift-JISなどデフォルトで使えるエンコーディング以外も扱えるようにしておく
		Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );
	}
	public List<string> ColumnNames { get; private set; } = new();
	public List<List<string>> RowData { get; private set; } = new();
	public void Parse( string filePath, bool isColumn, Encoding? encoding = null )
	{
		var bytes = File.ReadAllBytes( filePath );
		Parse( bytes, isColumn, encoding );
	}
	public void Parse( ReadOnlySpan<byte> bytes, bool isColumn, Encoding? encoding = null )
	{
		//	エンコードを判定する
		encoding = GetEncoding( bytes, encoding );
		// 文字コード別にパーサー書けばコピーしないで処理できるんだけど、コストが高いのでそこまではやらない
		// もしそういう対応をするなら、Parse<T,E>( ReadOnlySpan<T> data, bool isColumn ) みたいにして処理になるでしょう
		// でもって、int CharNext( this Encoding enc, ReadOnlySpan<T> data, int pos ) を作って処理だろうなぁ。
		var source = encoding.GetString( bytes );
		var csvText = source.AsSpan();
		int pos = 0;
		int startPos = 0;
		// BOMは無視する(あっても使わない)
		var bomBytes = encoding.GetPreamble();
		if( bomBytes.Length > 0 )
		{
			if( csvText[0] == encoding.GetString( bomBytes )[0] )
			{
				startPos = 1;
			}
		}
		bool insideQuote = false;
		RowData.Clear();
		List<string> columns = new();
		ColumnNames.Clear();
		bool firstLine = true;
		while( pos < csvText.Length )
		{
			switch( csvText[pos] )
			{
			case '"':
				//	ダブルクォートのエスケープ
				if( startPos < pos && csvText[pos - 1] == '\\' )
				{
					pos++;
				}
				else if( insideQuote )
				{
					insideQuote = false;
					// 範囲をくくっているので空白も含めて出力が必要
					var value = csvText.Slice( startPos, pos - startPos );
					columns.Add( value.ToString() );
					// デリミタまでスキップ(カット後の空白は無視する)
					while( pos < csvText.Length && csvText[pos] != ',' )
					{
						pos++;
					}
					pos++;
					startPos = pos;
				}
				else
				{
					insideQuote = true;
					pos++;
					startPos = pos;
				}
				break;
			case ',':
				if( !insideQuote )
				{
					var value = GetValue( csvText, startPos, pos );
					//	前後の空白は捨てる本当はここでパラメータで設定できるのがベスト
					columns.Add( value.ToString() ); 
					pos++;
					startPos = pos;
				}
				else
				{
					pos++;
				}
				break;
			case '\r':
			case '\n':
				if( !insideQuote )
				{
					if( pos - startPos > 0 )
					{
						var value = GetValue( csvText, startPos, pos );
						columns.Add( value.ToString() );
					}
					//	改行の場合は行の終わり
					if( firstLine )
					{
						if( isColumn )
						{
							ColumnNames.AddRange( columns );	// ここはコピーしてやらないとダメ(代入だとクリアされてしまう)
						}
						else
						{
							RowData.Add( columns );
						}
						firstLine = false;
					}
					else
					{
						RowData.Add( columns );
					}
					columns.Clear();
					if( csvText[pos] == '\r' && pos + 1 < csvText.Length && csvText[pos + 1] == '\n' )
					{
						pos++;
					}
					pos++;
					startPos = pos;
				}
				else
				{
					pos++;
				}
				break;
			default:
				pos++;
				//	その他の場合はセルの終わりまでスルー(エスケープ処理は基本的に単純パースでは処理不要)
				break;
			}
		}
		if( pos - startPos > 0 )
		{
			var value = GetValue( csvText, startPos, pos );
			columns.Add( value.ToString() );
		}
		if( columns.Count > 0 )
		{
			if( firstLine )
			{
				if( isColumn )
				{
					ColumnNames.AddRange( columns );    // ここはコピーしてやらないとダメ(代入だとクリアされてしまう)
				}
				else
				{
					RowData.Add( columns );
				}
				firstLine = false;
			}
			else
			{
				RowData.Add( columns );
			}
		}
	}

	private ReadOnlySpan<char> GetValue( ReadOnlySpan<char> csvText, int startPos, int pos )
	{
		var value = csvText.Slice( startPos, pos - startPos );
		if( IsTrim )
		{
			value = value.Trim();
		}
		return value;
	}

	public static Encoding GetEncoding( ReadOnlySpan<byte> bytes, Encoding? encoding )
	{
		// UTF-16BE, UTF-8, UTF-16LE, UTF-32BE, UTF-32LE の順にチェックする
		var bomEncList = new Encoding[] { Encoding.Unicode, Encoding.UTF8, Encoding.BigEndianUnicode, Encoding.UTF32, new UTF32Encoding( true, true ) };
		foreach( var enc in bomEncList )
		{
			var bomBytes = enc.GetPreamble();
			if( bytes.Slice( 0, bomBytes.Length ).SequenceEqual( bomBytes ) )
			{
				return enc;
			}
		}
		// コード変換チェックのサンプリング用文字を探し出す
		// 判定対象は UTF8 と Shift-JIS の２種類だけとする
		int pos = 0;
		while( pos < bytes.Length && bytes[pos] < 0x7F )
		{
			pos++;
		}
		int enableLength = bytes.Length-pos;
		// 優先設定されているエンコーディングでチェックする
		if( encoding != null )
		{
			if( IsValidEncoding( bytes, pos, encoding ) )
			{
				return encoding;
			}
		}
		// UTF8でチェック
		if( encoding != Encoding.UTF8 )
		{
			if( IsValidEncoding( bytes, pos, Encoding.UTF8 ) )
			{
				return Encoding.UTF8;
			}
		}
		// Shift-JISでチェック
		var SJIS = Encoding.GetEncoding( 932 );
		if( encoding != SJIS )
		{
			if( IsValidEncoding( bytes, pos, SJIS ) )
			{
				return SJIS;
			}
		}
		// ほかの文字コード体系はチェックしない(現実問題使われていないだろうし)
		return encoding ?? Encoding.UTF8;	//	アスキー文字しかないみたいなのでそれをチェックする
	}

	private static bool IsValidEncoding( ReadOnlySpan<byte> bytes, int startPos, Encoding encoding )
	{
		// ASCIIを超える範囲の文字の最初の一文字を変換してみて行って戻ってが同じになれば、それでOKとする
		var enableLength = bytes.Length - startPos;
		int checkLen = Math.Min( enableLength, encoding.GetMaxByteCount( 1 ) );
		var chkBytes = bytes.Slice( startPos, checkLen );

		var decode = encoding.GetString( chkBytes );
		var encode = encoding.GetBytes( decode.Substring(0,1) ); // 一文字分だけ変換する
		checkLen = Math.Min( chkBytes.Length, encode.Length );
		// 同一バイト数分だけ切り出して再度チェックする(１文字分だけのはず)
		var testBytes = bytes.Slice( startPos, checkLen );
		return testBytes.SequenceEqual( encode );
	}
}
