using System.Reflection;
using System.Text;
using WorkCsv;

namespace WorkCsvTest
{
	public class Tests
	{
		[SetUp]
		public void Setup()
		{
		}
		[Test]
		public void Test01_GetEncodingSJIS()
		{
			// S-JIS のテキストを渡してチェック
			var binFolder = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
			var filePath = Path.Combine( binFolder!, "JIGYOSYO.CSV" );
			var bytes = File.ReadAllBytes( filePath );
			Assert.That( CsvParser.GetEncoding( bytes, null ), Is.EqualTo( Encoding.GetEncoding( 932 ) ) );
		}
		[Test]
		public void Test02_GetEncodingUTF8()
		{
			// UTF8(BOMなし)のテキストを渡してチェック
			var binFolder = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
			var filePath = Path.Combine( binFolder!, "utf_ken_all.csv" );
			var bytes = File.ReadAllBytes( filePath );
			Assert.That( CsvParser.GetEncoding( bytes, null ), Is.EqualTo( Encoding.UTF8 ) );
		}
		[TestCase( "test.csv" )]
		[TestCase( "JIGYOSYO.CSV" )]
		[TestCase( "utf_ken_all.csv" )]
		public void Test03_ParseTest( string fileName )
		{
			var binFolder = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
			var filePath = Path.Combine( binFolder!, fileName );
			var parser = new CsvParser();
			parser.Parse( filePath, false );
			// test.csvは１行
			var text = File.ReadAllText( filePath );
			foreach( var row in parser.RowData )
			{
				foreach( var column in row	)
				{
					// すべてのテキストデータがそのまま含まれているはず(そういうパースの方法なので)
					Assert.That( text.Contains( column ) );
				}
			}
		}
	}
}